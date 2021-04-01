using Antymology.Terrain;
using Antymology.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Ant : MonoBehaviour
{

    public int health;
    public float feelings;
    private int numStone;
    private int numGrass;
    private int aboveAcid;
    private int statesReached;
    public int uselessActions;
    public int ticksSurvived;
    public int[] takenActions;
    private Queue<int> movementLoop;
    public int loops;
    private int giftsGiven;
    public int lastAction;
    public int ticksStuck;

    public void Init(System.Random RNG)
    {
        gameObject.SetActive(true);
        int xCoord = RNG.Next(WorldManager.Instance.Topography.GetLength(0) / 4, WorldManager.Instance.Topography.GetLength(0) * 3 / 4);
        int zCoord = RNG.Next(WorldManager.Instance.Topography.GetLength(1) / 4, WorldManager.Instance.Topography.GetLength(1) * 3 / 4);
        while (WorldManager.Instance.Topography[xCoord, zCoord] >= (ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter) - 2)
        {
            // protects ants from being abandoned on tall pillars
            xCoord = RNG.Next(WorldManager.Instance.Topography.GetLength(0) / 4, WorldManager.Instance.Topography.GetLength(0) * 3 / 4);
            zCoord = RNG.Next(WorldManager.Instance.Topography.GetLength(1) / 4, WorldManager.Instance.Topography.GetLength(1) * 3 / 4);
        }
        float yCoord = WorldManager.Instance.Topography[xCoord, zCoord] + 0.7f;
        transform.position = new Vector3(xCoord, yCoord, zCoord);
        AgentManager.Instance.AntDist[xCoord, zCoord] += 1;
        health = AntNetConfigs.Instance.Start_Health;
        AbstractBlock target = WorldManager.Instance.GetBlock(xCoord, CustomMath.fastfloor(yCoord), zCoord);
        if (target as AcidicBlock != null) aboveAcid = 2;
        else aboveAcid = 1;
        numStone = 0;
        numGrass = 0;
        lastAction = 0;
        statesReached = 0;
        ticksSurvived = 0;
        uselessActions = 0;
        giftsGiven = 0;
        ticksStuck = 0;
        loops = 0;
        movementLoop = new Queue<int>();
        movementLoop.Enqueue(0);
        movementLoop.Enqueue(0);
        takenActions = new int[AntNetConfigs.Instance.Ant_Net_Outputs];

        genetics = new float[
            AgentManager.Instance.NetworkLayerIndexes[5] + 1 +
            (AntNetConfigs.Instance.Ant_Net_Outputs *
            AntNetConfigs.Instance.Ant_Net_HL3_Nodes)];
    }

    #region Actions

    public void TakeTurn(float[] inputs)
    {
        if (!gameObject.activeSelf) return;
        ticksSurvived++;
        //AgentManager.Instance.PrintList(myid, inputs, "inputs");
        float[] output = ForwardPass(inputs);
        feelings = output[10 - 1];
        //AgentManager.Instance.PrintList(myid, output, "output");
        int moveAction = 0;
        float cMax = output[0];
        for (int i = 1; i < 5; i++)
        {
            if (output[i] > cMax)
            {
                cMax = output[i];
                moveAction = i;
            }
        }
        int action = 0;
        cMax = output[5];
        for (int i = 6; i < AntNetConfigs.Instance.Ant_Net_Outputs - 1; i++)
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
        else uselessActions++;
        takenActions[moveAction]++;
    }

    public void DoAction(int action)
    {
        // if action is 0, does nothing
        //Debug.Log("Queen is taking action: " + action);
        if (action == 5)
        {
            DestroyBlock();
            if (lastAction > 5) loops++;
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
            ShareHealth();
        }
        else uselessActions++;
        takenActions[action]++;
        lastAction = action;
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

    public void Move(int dx, int dz)
    {
        int oldx = (int)transform.position.x;
        int oldz = (int)transform.position.z;
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
        if (dy == -3) {
            ticksStuck++;
            uselessActions++;
            return;
        }
        
        transform.position = new Vector3(xcoord, transform.position.y + dy, zcoord);
        statesReached++;

        if (target as AcidicBlock != null) aboveAcid = 2;
        else aboveAcid = 1;

        AgentManager.Instance.AntDist[oldx, oldz]--;
        AgentManager.Instance.AntDist[xcoord, zcoord]++;
    }

    public void ShareHealth()
    {
        if (AgentManager.Instance.theQueen.transform.position == transform.position) {
            int gift = Mathf.Min(health - 1, AntNetConfigs.Instance.Max_Health_Gift);
            health -= gift;
            AgentManager.Instance.theQueen.getHealed(gift);
            statesReached++;
            giftsGiven += 10;
            return;
        }
        foreach (Ant ant in AgentManager.Instance.Ants) {
            if (ant != this && ant.GetPosition() == transform.position)
            {
                int gift = Mathf.Min(health - 1, AntNetConfigs.Instance.Max_Health_Gift);
                health -= gift;
                ant.getHealed(gift);
                statesReached++;
                giftsGiven++;
                return;
            }
        }
        uselessActions++;
    }

    #endregion

    #region Helpers

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void getHealed(int gift)
    {
        health += gift;
    }

    private bool SharesSpace()
    {
        if (AgentManager.Instance.theQueen.transform.position == transform.position) return true;
        int xcoord = (int)transform.position.x;
        int zcoord = (int)transform.position.z;
        if (AgentManager.Instance.AntDist[xcoord, zcoord] > 1) return true;
        return false;
        
    }

    #endregion

    #region Network
    public float[] genetics;

    public void InitRandomNetwork()
    {
        int factor = UnityEngine.Random.Range(1, 10);
        for (int i = 0; i < AgentManager.Instance.NetworkLayerIndexes[0]; i++) genetics[i] = factor * UnityEngine.Random.Range(-0.1f, 0.1f);
        for (int i = AgentManager.Instance.NetworkLayerIndexes[0]; i < AgentManager.Instance.NetworkLayerIndexes[1]; i++) genetics[i] = UnityEngine.Random.Range(-0.05f, 0.05f);
        for (int i = AgentManager.Instance.NetworkLayerIndexes[1]; i < AgentManager.Instance.NetworkLayerIndexes[2]; i++) genetics[i] = factor * UnityEngine.Random.Range(-0.1f, 0.1f);
        for (int i = AgentManager.Instance.NetworkLayerIndexes[2]; i < AgentManager.Instance.NetworkLayerIndexes[3]; i++) genetics[i] = UnityEngine.Random.Range(-0.05f, 0.05f);
        for (int i = AgentManager.Instance.NetworkLayerIndexes[3]; i < AgentManager.Instance.NetworkLayerIndexes[4]; i++) genetics[i] = factor * UnityEngine.Random.Range(-0.1f, 0.1f);
        for (int i = AgentManager.Instance.NetworkLayerIndexes[4]; i < AgentManager.Instance.NetworkLayerIndexes[5]; i++) genetics[i] = UnityEngine.Random.Range(-0.05f, 0.05f);
        for (int i = AgentManager.Instance.NetworkLayerIndexes[5]; i < genetics.Length; i++) genetics[i] = factor * UnityEngine.Random.Range(-0.1f, 0.1f);
    }

    public float[] ForwardPass(float[] inputs)
    {    
        float[] h1vals = new float[AntNetConfigs.Instance.Ant_Net_HL1_Nodes];
        float[] h2vals = new float[AntNetConfigs.Instance.Ant_Net_HL2_Nodes];
        float[] h3vals = new float[AntNetConfigs.Instance.Ant_Net_HL3_Nodes];
        float[] outputs = new float[AntNetConfigs.Instance.Ant_Net_Outputs];
        for (int i = 0; i < AntNetConfigs.Instance.Ant_Net_HL1_Nodes; i++)
        {
            for (int j = 0; j < inputs.Length; j++)
            {
                h1vals[i] += inputs[j] * genetics[i * inputs.Length + j];
            }
            h1vals[i] = Math.Max((h1vals[i] + genetics[i + AgentManager.Instance.NetworkLayerIndexes[0]]) * 0.01f, h1vals[i] + genetics[i + AgentManager.Instance.NetworkLayerIndexes[0]]); // Leaky ReLU
        }
        for (int i = 0; i < AntNetConfigs.Instance.Ant_Net_HL2_Nodes; i++)
        {
            for (int j = 0; j < AntNetConfigs.Instance.Ant_Net_HL1_Nodes; j++)
            {
                h2vals[i] += h1vals[j] * genetics[AgentManager.Instance.NetworkLayerIndexes[1] + i * AntNetConfigs.Instance.Ant_Net_HL1_Nodes + j];
            }
            h2vals[i] = Math.Max((h2vals[i] + genetics[i + AgentManager.Instance.NetworkLayerIndexes[2]]) * 0.01f, h2vals[i] + genetics[i + AgentManager.Instance.NetworkLayerIndexes[2]]); // Leaky ReLU
        }
        for (int i = 0; i < AntNetConfigs.Instance.Ant_Net_HL3_Nodes; i++)
        {
            for (int j = 0; j < AntNetConfigs.Instance.Ant_Net_HL2_Nodes; j++)
            {
                h3vals[i] += h2vals[j] * genetics[AgentManager.Instance.NetworkLayerIndexes[3] + i * AntNetConfigs.Instance.Ant_Net_HL2_Nodes + j];
            }
            h3vals[i] = Math.Max((h3vals[i] + genetics[i + AgentManager.Instance.NetworkLayerIndexes[4]]) * 0.01f, h3vals[i] + genetics[i + AgentManager.Instance.NetworkLayerIndexes[4]]); // Leaky ReLU
        }
        for (int i = 0; i < AntNetConfigs.Instance.Ant_Net_Outputs; i++)
        {
            for (int j = 0; j < AntNetConfigs.Instance.Ant_Net_HL3_Nodes; j++)
            {
                outputs[i] += h3vals[j] * genetics[AgentManager.Instance.NetworkLayerIndexes[5] + i * AntNetConfigs.Instance.Ant_Net_HL3_Nodes + j];
            }
        }
        return outputs;
    }

    public float GetScore()
    {
        int score = 0;
        score += statesReached / 10;
        score -= takenActions.Max() / 10;
        score -= uselessActions;
        score -= ticksStuck * 2;
        score -= loops;
        score += takenActions.Min() * 10;
        score += ticksSurvived / 10;
        foreach (int a in takenActions) if (a > 0) score += 5;
        score += AgentManager.Instance.theQueen.nestBlocksPlaced * 5;
        return score;
    }

    #endregion

    /// <summary>
    /// Late update happens after all default updates have been called.
    /// </summary>
    public void LateUpdate()
    {
        // Ant dies if it has no health left
        if (health == 0)
        {
            gameObject.SetActive(false);
            AgentManager.Instance.AntDist[(int)transform.position.x, (int)transform.position.z]--;
        }
    }
}
