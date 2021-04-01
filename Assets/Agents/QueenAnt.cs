using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;
using Antymology.Helpers;
using System;
using System.Linq;

public class QueenAnt : MonoBehaviour
{
    public int health;
    public int nestCost;
    public float command;
    private int numStone;
    private int numGrass;
    private int aboveAcid;
    private BlocksDisplay display;
    private int[] QueenLayerIndexes;
    public int ticksSurvived;
    public int[] takenActions;
    public int nestBlocksPlaced;
    private int statesReached;
    public int uselessActions;
    private Queue<int> movementLoop;
    public int loops;
    public int lastAction;

    public void Init(int dx, int dz)
    {
        gameObject.SetActive(true);
        command = 0.0f;
        numStone = 0;
        numGrass = 0;
        ticksSurvived = 0;
        nestBlocksPlaced = 0;
        statesReached = 0;
        uselessActions = 0;
        lastAction = 0;
        loops = 0;
        movementLoop = new Queue<int>();
        movementLoop.Enqueue(0);
        movementLoop.Enqueue(0);
        nestCost = AntNetConfigs.Instance.Max_Health / 3;
        takenActions = new int[AntNetConfigs.Instance.Ant_Net_Outputs];

        int xQueen = WorldManager.Instance.Topography.GetLength(0) / 2 + dx;
        int zQueen = WorldManager.Instance.Topography.GetLength(1) / 2 + dz;

        while (WorldManager.Instance.GetBlock(xQueen, WorldManager.Instance.Topography[xQueen, zQueen], zQueen) as ContainerBlock != null && !AgentManager.Instance.QueenInit)
        {
            //avoids queen being stranded on pillar
            xQueen++;
            zQueen++;
        }

        float yQueen = WorldManager.Instance.Topography[xQueen, zQueen] + 0.7f;
        transform.position = new Vector3(xQueen, yQueen, zQueen);
        health = AntNetConfigs.Instance.Start_Health;

        GameObject textObj = GameObject.FindGameObjectWithTag("TextDisplay");
        display = textObj.GetComponent<BlocksDisplay>();
        display.ResetNestBlocks();

        int ycoord = CustomMath.fastfloor(transform.position.y);

        AbstractBlock target = WorldManager.Instance.GetBlock(xQueen, ycoord, zQueen);
        if (target as AcidicBlock != null) aboveAcid = 2;
        else aboveAcid = 1;
    }

    public void InitLayers()
    {
        QueenLayerIndexes = new int[6];

        QueenLayerIndexes[0] = // Layer1 Weights
            AntNetConfigs.Instance.Queen_Net_HL1_Nodes *
            (AntNetConfigs.Instance.Queen_Net_Extra_Inputs +
            AntNetConfigs.Instance.Queen_Net_Env_Inputs *
            AntNetConfigs.Instance.Queen_Net_Env_Features);

        QueenLayerIndexes[1] = // Layer1 Biases
            QueenLayerIndexes[0] +
            AntNetConfigs.Instance.Queen_Net_HL1_Nodes;

        QueenLayerIndexes[2] = // Layer2 Weights
            QueenLayerIndexes[1] +
            (AntNetConfigs.Instance.Queen_Net_HL2_Nodes *
            AntNetConfigs.Instance.Queen_Net_HL1_Nodes);

        QueenLayerIndexes[3] = // Layer2 Biases
            QueenLayerIndexes[2] +
            AntNetConfigs.Instance.Queen_Net_HL2_Nodes;

        QueenLayerIndexes[4] = // Layer3 Weights
            QueenLayerIndexes[3] +
            (AntNetConfigs.Instance.Queen_Net_HL3_Nodes *
            AntNetConfigs.Instance.Queen_Net_HL2_Nodes);

        QueenLayerIndexes[5] = // Layer3 Biases
            QueenLayerIndexes[4] +
            AntNetConfigs.Instance.Queen_Net_HL3_Nodes;

        genetics = new float[
            QueenLayerIndexes[5] +
            (AntNetConfigs.Instance.Queen_Net_Outputs *
            AntNetConfigs.Instance.Queen_Net_HL3_Nodes)];
    }

    public void TakeTurn(float[] inputs)
    {
        if(gameObject.activeSelf) ticksSurvived++;
        float[] output = ForwardPass(inputs);
        command = output[AntNetConfigs.Instance.Queen_Net_Outputs - 1];
        int moveAction = 0;
        float cMax = output[0];
        for (int i = 1; i < 5 ; i++)
        {
            if (output[i] > cMax)
            {
                cMax = output[i];
                moveAction = i;
            }
        }
        int action = 0;
        cMax = output[5];
        for (int i = 6; i < AntNetConfigs.Instance.Queen_Net_Outputs - 1; i++)
        {
            if (output[i] > cMax)
            {
                cMax = output[i];
                action = i;
            }
        }
        DoMove(moveAction);
        DoAction(action);
        health -= aboveAcid;
    }

    private void DoMove(int moveAction)
    {
        // if moveAction is 0, does nothing
        //Debug.Log("Queen is taking move: " + moveAction);
        if (moveAction == 1)
        {
            Move(1, 0);
            if (moveAction == movementLoop.Dequeue()) loops++;
            movementLoop.Enqueue(moveAction);
        }
        else if (moveAction == 2)
        {
            Move(0, 1);
            if (moveAction == movementLoop.Dequeue()) loops++;
            movementLoop.Enqueue(moveAction);
        }
        else if (moveAction == 3)
        {
            Move(-1, 0);
            if (moveAction == movementLoop.Dequeue()) loops++;
            movementLoop.Enqueue(moveAction);
        }
        else if (moveAction == 4)
        {
            Move(0, -1);
            if (moveAction == movementLoop.Dequeue()) loops++;
            movementLoop.Enqueue(moveAction);
        }
        takenActions[moveAction]++;
    }

    public void DoAction(int action)
    {
        // if action is 0, does nothing
        //Debug.Log("Queen is taking action: " + action);
        if (action == 5)
        {
            DestroyBlock();
            if (lastAction == 6 || lastAction == 7) loops++;
        }
        else if (action == 6)
        {
            PlaceGrass();
            if (lastAction == 5) loops++;
        }
        else if (action == 7)
        {
            PlaceStone();
            if (lastAction == 5) loops++;
        }
        else if (action == 8)
        {
            PlaceNest();
        }
        else uselessActions++;
        takenActions[action]++;
        lastAction = action;
    }

    private void PlaceNest()
    {
        if (health > nestCost)
        {
            int xcoord = (int)transform.position.x;
            int zcoord = (int)transform.position.z;
            int ycoord = CustomMath.fastfloor(transform.position.y) + 1;
            if (ycoord >= (ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter) - 2) return;
            transform.position += new Vector3(0f, 1f, 0f);
            WorldManager.Instance.SetBlock(xcoord, ycoord, zcoord, new NestBlock());
            display.AddNestBlock();
            nestBlocksPlaced++;
            health -= nestCost;
        }
        else uselessActions++;
    }

    public void DestroyBlock()
    {
        if (SharesSpace()) return;
        int xcoord = (int)transform.position.x;
        int zcoord = (int)transform.position.z;
        int ycoord = CustomMath.fastfloor(transform.position.y);
        AbstractBlock target = WorldManager.Instance.GetBlock(xcoord, ycoord, zcoord);
        if (target as MulchBlock != null)
        {
            WorldManager.Instance.SetBlock(xcoord, ycoord, zcoord, new AirBlock());
            health = Mathf.Min(AntNetConfigs.Instance.Max_Health, health + AntNetConfigs.Instance.Mulch_Health_Gain);
            transform.position += new Vector3(0f, -1f, 0f);
            statesReached++;
        }
        else if (target as StoneBlock != null)
        {
            WorldManager.Instance.SetBlock(xcoord, ycoord, zcoord, new AirBlock());
            if (numStone < AntNetConfigs.Instance.Max_Stone) numStone += 1;
            transform.position += new Vector3(0f, -1f, 0f);
            statesReached++;
        }
        else if (target as GrassBlock != null)
        {
            WorldManager.Instance.SetBlock(xcoord, ycoord, zcoord, new AirBlock());
            if (numGrass < AntNetConfigs.Instance.Max_Grass) numGrass += 1;
            transform.position += new Vector3(0f, -1f, 0f);
            statesReached++;
        }
        else if (target as AcidicBlock != null)
        {
            WorldManager.Instance.SetBlock(xcoord, ycoord, zcoord, new AirBlock());
            transform.position += new Vector3(0f, -1f, 0f);
            statesReached++;
        }
        else uselessActions++;
    }

    public void PlaceStone()
    {
        if (numStone > 0 &&
            CustomMath.fastfloor(transform.position.y) < (ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter))
        {
            transform.position += new Vector3(0f, 1f, 0f);
            int xcoord = (int)transform.position.x;
            int zcoord = (int)transform.position.z;
            int ycoord = CustomMath.fastfloor(transform.position.y);
            WorldManager.Instance.SetBlock(xcoord, ycoord, zcoord, new StoneBlock());
            statesReached++;
            numStone -= 1;
        }
        else uselessActions++;
    }

    public void PlaceGrass()
    {
        if (numGrass > 0 &&
            CustomMath.fastfloor(transform.position.y) < (ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter))
        {
            transform.position += new Vector3(0f, 1f, 0f);
            int xcoord = (int)transform.position.x;
            int zcoord = (int)transform.position.z;
            int ycoord = CustomMath.fastfloor(transform.position.y);
            WorldManager.Instance.SetBlock(xcoord, ycoord, zcoord, new GrassBlock());
            statesReached++;
            numGrass -= 1;
        }
        else uselessActions++;
    }

    private bool SharesSpace()
    {
        int xcoord = (int)transform.position.x;
        int zcoord = (int)transform.position.z;
        if (AgentManager.Instance.AntDist[xcoord, zcoord] > 0) return true;
        return false;
    }

    public void Move(int dx, int dz)
    {
        int xcoord = (int)transform.position.x + dx;
        int zcoord = (int)transform.position.z + dz;
        int ycoord = CustomMath.fastfloor(transform.position.y) + 1;
        int dy = -3;
        AbstractBlock target = WorldManager.Instance.GetBlock(xcoord, ycoord + 2, zcoord);
        if (target as AirBlock == null) return;
        for (int j = 2; j >= -2; j--)
        {
            target = WorldManager.Instance.GetBlock(xcoord, ycoord + j - 1, zcoord);
            if (target as AirBlock == null)
            {
                dy = j;
                break;
            }
        }
        if (dy == -3)
        {
            uselessActions++;
            return;
        }
        transform.position = new Vector3(xcoord, transform.position.y + dy, zcoord);
        statesReached++;
        if (target as AcidicBlock != null) aboveAcid = 2;
        else aboveAcid = 1;
    }

    public void getHealed(int gift)
    {
        health += gift;
    }

    #region Network
    public float[] genetics;

    public void InitRandomNetwork()
    {
        int factor = UnityEngine.Random.Range(1, 10);
        for (int i = 0; i < QueenLayerIndexes[0]; i++) genetics[i] = factor * UnityEngine.Random.Range(-0.1f, 0.1f);
        for (int i = QueenLayerIndexes[0]; i < QueenLayerIndexes[1]; i++) genetics[i] = UnityEngine.Random.Range(-0.05f, 0.05f);
        for (int i = QueenLayerIndexes[1]; i < QueenLayerIndexes[2]; i++) genetics[i] = factor * UnityEngine.Random.Range(-0.1f, 0.1f);
        for (int i = QueenLayerIndexes[2]; i < QueenLayerIndexes[3]; i++) genetics[i] = UnityEngine.Random.Range(-0.05f, 0.05f);
        for (int i = QueenLayerIndexes[3]; i < QueenLayerIndexes[4]; i++) genetics[i] = factor * UnityEngine.Random.Range(-0.1f, 0.1f);
        for (int i = QueenLayerIndexes[4]; i < QueenLayerIndexes[5]; i++) genetics[i] = UnityEngine.Random.Range(-0.05f, 0.05f);
        for (int i = QueenLayerIndexes[5]; i < genetics.Length; i++) genetics[i] = factor * UnityEngine.Random.Range(-0.1f, 0.1f);
    }

    public float[] ForwardPass(float[] inputs)
    {
        float[] h1vals = new float[AntNetConfigs.Instance.Queen_Net_HL1_Nodes];
        float[] h2vals = new float[AntNetConfigs.Instance.Queen_Net_HL2_Nodes];
        float[] h3vals = new float[AntNetConfigs.Instance.Queen_Net_HL3_Nodes];
        float[] outputs = new float[AntNetConfigs.Instance.Queen_Net_Outputs];
        //AgentManager.Instance.PrintList(0, inputs, "inputs");
        for (int i = 0; i < AntNetConfigs.Instance.Queen_Net_HL1_Nodes; i++)
        {
            for (int j = 0; j < inputs.Length; j++)
            {
                h1vals[i] += inputs[j] * genetics[i * inputs.Length + j];
            }
            h1vals[i] = Math.Max((h1vals[i] + genetics[i + QueenLayerIndexes[0]]) * 0.01f, h1vals[i] + genetics[i + QueenLayerIndexes[0]]); // Leaky ReLU
        }

        for (int i = 0; i < AntNetConfigs.Instance.Queen_Net_HL2_Nodes; i++)
        {
            for (int j = 0; j < AntNetConfigs.Instance.Queen_Net_HL1_Nodes; j++)
            {
                h2vals[i] += h1vals[j] * genetics[QueenLayerIndexes[1] + i * AntNetConfigs.Instance.Queen_Net_HL1_Nodes + j];
            }
            h2vals[i] = Math.Max((h2vals[i] + genetics[i + QueenLayerIndexes[2]]) * 0.01f, h2vals[i] + genetics[i + QueenLayerIndexes[2]]); // Leaky ReLU
        }
        for (int i = 0; i < AntNetConfigs.Instance.Queen_Net_HL3_Nodes; i++)
        {
            for (int j = 0; j < AntNetConfigs.Instance.Queen_Net_HL2_Nodes; j++)
            {
                h3vals[i] += h2vals[j] * genetics[QueenLayerIndexes[3] + i * AntNetConfigs.Instance.Queen_Net_HL2_Nodes + j];
            }
            h3vals[i] = Math.Max((h3vals[i] + genetics[i + QueenLayerIndexes[4]]) * 0.01f, h3vals[i] + genetics[i + QueenLayerIndexes[4]]); // Leaky ReLU
        }
        for (int i = 0; i < AntNetConfigs.Instance.Queen_Net_Outputs; i++)
        {
            for (int j = 0; j < AntNetConfigs.Instance.Queen_Net_HL3_Nodes; j++)
            {
                outputs[i] += h3vals[j] * genetics[QueenLayerIndexes[5] + i * AntNetConfigs.Instance.Queen_Net_HL3_Nodes + j];
            }
        }
        //AgentManager.Instance.PrintList(0, outputs, "outputs from position " + transform.position.x + ", " + transform.position.z);
        return outputs;
    }

    public int GetScore()
    {
        int score = 0;
        score += statesReached / 10;
        score -= takenActions.Max() / 10;
        score -= uselessActions;
        score -= loops;
        score += takenActions.Min() * 10;
        score += ticksSurvived / 10;
        foreach (int a in takenActions) if (a > 0) score += 5;
        score += takenActions.Sum();
        score +=nestBlocksPlaced * 10;
        return score;
    }

    #endregion

    /// <summary>
    /// Late update happens after all default updates have been called.
    /// </summary>
    public void LateUpdate()
    {
        // Ant dies if it has no health left
        if (health == 0 && !AgentManager.Instance.QueenInit && !AgentManager.Instance.AntInit)
        {
            AgentManager.Instance.ColonyDead();
        }
    }

}
