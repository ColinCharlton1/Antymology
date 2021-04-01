using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antymology.Helpers;
using Antymology.Terrain;
using System;
using UnityEngine.UI;
using System.Linq;

public class AgentManager : Singleton<AgentManager>
{

    #region Fields

    /// <summary>
    /// The prefab containing the ant.
    /// </summary>
    public GameObject antPrefab;

    /// <summary>
    /// The prefab containing the Queen ant.
    /// </summary>
    public GameObject queenAntPrefab;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public QueenAnt theQueen;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public List<QueenAnt> initQueens;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    private float QueenCommand;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public Queue<float[]> WaitingAntGenes;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public Queue<float[]> WaitingQueenGenes;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public List<Ant> Ants;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public float GlobalFeeling;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public int[,] AntDist;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public List<float[]> AntGeneHistory;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public List<float> AntRewardHistory;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public List<float[]> QueenGeneHistory;

    /// <summary>
    /// A list of all the ants in this generation.
    /// </summary>
    public List<float> QueenRewardHistory;

    /// <summary>
    /// Just holds sight diameter of ants.
    /// </summary>
    private int SightRadius;

    /// <summary>
    /// Just holds sight diameter of ants.
    /// </summary>
    private int QueenSightRadius;

    /// <summary>
    /// Random number generator.
    /// </summary>
    private System.Random RNG;

    /// <summary>
    /// Random number generator.
    /// </summary>
    public bool QueenInit;

    /// <summary>
    /// Random number generator.
    /// </summary>
    public bool AntInit;
    
    /// <summary>
    /// Values Used by Ants for interpreting genes as neural net.
    /// </summary>
    public int[] NetworkLayerIndexes;

    /// <summary>
    /// The text for displaying ticks.
    /// </summary>
    private Text GenText;
    public int gen;
    public int subset;

    #endregion

    #region Initialization

    public void Init(System.Random rng)
    {
        gen = 1;
        subset = 0;
        QueenCommand = 0.5f;
        GlobalFeeling = 0.5f;
        QueenInit = true;
        AntInit = true;
        RNG = rng;
        Ants = new List<Ant>();
        initQueens = new List<QueenAnt>();
        WaitingAntGenes = new Queue<float[]>();
        WaitingQueenGenes = new Queue<float[]>();
        QueenGeneHistory = new List<float[]>();
        AntGeneHistory = new List<float[]>();
        QueenRewardHistory = new List<float>();
        AntRewardHistory = new List<float>();

        AntDist = new int[
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter];

        NetworkLayerIndexes = new int[6];
            
        NetworkLayerIndexes[0] = // Layer1 Biases
            AntNetConfigs.Instance.Ant_Net_HL1_Nodes *
            (AntNetConfigs.Instance.Ant_Net_Extra_Inputs +
            AntNetConfigs.Instance.Ant_Net_Env_Inputs *
            AntNetConfigs.Instance.Ant_Net_Env_Features);

        NetworkLayerIndexes[1] = // Layer1 Weights
            NetworkLayerIndexes[0] + 
            AntNetConfigs.Instance.Ant_Net_HL1_Nodes;

        NetworkLayerIndexes[2] = // Layer2 Weights
            NetworkLayerIndexes[1] +
            (AntNetConfigs.Instance.Ant_Net_HL2_Nodes *
            AntNetConfigs.Instance.Ant_Net_HL1_Nodes);

        NetworkLayerIndexes[3] = // Layer2 Weights
            NetworkLayerIndexes[2] +
            AntNetConfigs.Instance.Ant_Net_HL2_Nodes;

        NetworkLayerIndexes[4] = // Layer3 Weights
            NetworkLayerIndexes[3] +
            (AntNetConfigs.Instance.Ant_Net_HL3_Nodes *
            AntNetConfigs.Instance.Ant_Net_HL2_Nodes);

        NetworkLayerIndexes[5] = // Layer3 Biases
            NetworkLayerIndexes[4] +
            AntNetConfigs.Instance.Ant_Net_HL3_Nodes;

        GameObject queen = Instantiate(queenAntPrefab);
        theQueen = queen.GetComponent<QueenAnt>();
        theQueen.InitLayers();
        theQueen.Init(0, 0);

        for (int i = 0; i < AntNetConfigs.Instance.Num_Ants * 5; i++)
        {
            GameObject antObj = Instantiate(antPrefab);
            Ant ant = antObj.GetComponent<Ant>();
            ant.Init(RNG);
            Ants.Add(ant);
        }

        SightRadius = (int)(Mathf.Sqrt(AntNetConfigs.Instance.Ant_Net_Env_Inputs) - 1) / 2;
        QueenSightRadius = (int)(Mathf.Sqrt(AntNetConfigs.Instance.Queen_Net_Env_Inputs) - 1) / 2;

        GameObject genTextObject = GameObject.FindGameObjectWithTag("GenText");
        GenText = genTextObject.GetComponent<Text>();
        CreateInitQueens();
    }

    /// <summary>
    /// Runs as the update function until enough viable queens have been stored in the history
    /// Used because only having 1 queen evaluated each time was not nearly enough
    /// </summary>
    private void InitializeQueens()
    {
        if (QueenRewardHistory.Count >= 10 * AntNetConfigs.Instance.Num_Subsets)
        {
            QueenInit = false;
            foreach (QueenAnt queen in initQueens) Destroy(queen.gameObject);
            initQueens.Clear();
            StartAntInit();
            return;
        }
        RunInitQueenTurns();
        if (WorldManager.Instance.Ticks == 50)
        {
            subset++;
            WorldManager.Instance.CreateNewWorld();
            foreach (Ant ant in Ants) ant.Init(RNG); ;
            foreach (QueenAnt queen in initQueens)
            {
                int actions = 0;
                foreach (int a in queen.takenActions) if (a > 0) actions++;
                Debug.Log("Queen Actions greater than 2: " + (actions > 2) + " less than 40 useless actions: " + (queen.uselessActions < 40) + " less than 10 loops: " + (queen.loops < 10));
                Debug.Log("Queen Actions greater than 3: " + (actions > 3) + " less than 45 useless actions: " + (queen.uselessActions < 45) + " less than 15 loops: " + (queen.loops < 15));
                Debug.Log("Queen Actions greater than 4: " + (actions > 4) + " less than 50 useless actions: " + (queen.uselessActions < 50) + " less than 20 loops: " + (queen.loops < 20));
                if (
                    (actions > 2 && queen.uselessActions < 50 && queen.loops < 10) ||
                    (actions > 3 && queen.uselessActions < 65 && queen.loops < 15) ||
                    (actions > 4 && queen.uselessActions < 70 && queen.loops < 20) ||
                    queen.nestBlocksPlaced > 3)
                {
                    Debug.Log("Good Queen");
                    QueenGeneHistory.Add(queen.genetics);
                    QueenRewardHistory.Add(queen.GetScore() + (AntNetConfigs.Instance.Start_Health - 50) / 10);

                }
                if (QueenRewardHistory.Count < 10 * AntNetConfigs.Instance.Num_Subsets)
                {
                    queen.Init(RNG.Next(-40, 40), RNG.Next(-40, 40));
                    queen.InitRandomNetwork();
                }

            }
        }
    }

    private void StartAntInit()
    {
        AntDist = new int[
        ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
        ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter];

        WorldManager.Instance.CreateNewWorld();
        theQueen.Init(0, 0);
        foreach (Ant ant in Ants)
        {
            ant.Init(RNG);
            ant.InitRandomNetwork();
        }
        subset = 0;
        QueenCommand = 0.5f;
        GlobalFeeling = 0.5f;
    }

    private void InitializeAnts()
    {
        if (AntRewardHistory.Count >= AntNetConfigs.Instance.Num_Ants * AntNetConfigs.Instance.Num_Subsets / 2)
        {
            AntInit = false;
            for (int i = AntNetConfigs.Instance.Num_Ants; i < Ants.Count; i++) Destroy(Ants[i].gameObject);
            Ants.RemoveRange(AntNetConfigs.Instance.Num_Ants, Ants.Count - AntNetConfigs.Instance.Num_Ants);
            Debug.Log("ants after range removed: " + Ants.Count);
            subset = 0;
            NewColony();
            return;
        }
        RunAntTurns();
        if (WorldManager.Instance.Ticks == 50)
        {
            subset++;
            WorldManager.Instance.CreateNewWorld();

            AntDist = new int[
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter];

            theQueen.Init(0, 0);
            foreach (Ant ant in Ants)
            {
                int actions = 0;
                foreach (int a in ant.takenActions) if (a > 0) actions++;
                Debug.Log("Ant Actions greater than 2: " + (actions > 2) + " less than 40 useless actions: " + (ant.uselessActions < 40) + " less than 5 loops: " + (ant.loops < 10));
                Debug.Log("Ant Actions greater than 3: " + (actions > 3) + " less than 50 useless actions: " + (ant.uselessActions < 50) + " less than 10 loops: " + (ant.loops < 15));
                Debug.Log("Ant Actions greater than 4: " + (actions > 4) + " less than 60 useless actions: " + (ant.uselessActions < 60) + " less than 15 loops: " + (ant.loops < 20));
                if (
                    (actions > 2 && ant.uselessActions < 40 && ant.loops < 10 && ant.ticksStuck < 10) ||
                    (actions > 3 && ant.uselessActions < 50 && ant.loops < 15 && ant.ticksStuck < 15) ||
                    (actions > 4 && ant.uselessActions < 60 && ant.loops < 20 && ant.ticksStuck < 20))
                {
                    Debug.Log("Good Ant");
                    AntGeneHistory.Add(ant.genetics);
                    AntRewardHistory.Add(ant.GetScore() * + (AntNetConfigs.Instance.Start_Health - 50) / 10);
                }
                if (AntRewardHistory.Count < AntNetConfigs.Instance.Num_Ants * AntNetConfigs.Instance.Num_Subsets / 2)
                {
                    ant.Init(RNG);
                    ant.InitRandomNetwork();
                }
            }
        }
    }

    private void RunInitQueenTurns()
    {
        foreach (QueenAnt queen in initQueens)
        {
            int xQcoord = (int)queen.transform.position.x;
            int zQcoord = (int)queen.transform.position.z;
            int yQcoord = CustomMath.fastfloor(theQueen.transform.position.y) + 1;
            float[] inputQ = new float[AntNetConfigs.Instance.Queen_Net_Extra_Inputs + AntNetConfigs.Instance.Queen_Net_Env_Inputs * AntNetConfigs.Instance.Queen_Net_Env_Features];
            int indexQ = 0;
            inputQ[indexQ++] = GlobalFeeling;
            inputQ[indexQ++] = queen.transform.position.x / AntDist.GetLength(0);
            inputQ[indexQ++] = queen.transform.position.z / AntDist.GetLength(1);
            inputQ[indexQ++] = queen.transform.position.y / (ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter);
            inputQ[indexQ++] = (float)Ants.Count / (AntNetConfigs.Instance.Num_Ants);
            inputQ[indexQ++] = (float)theQueen.health / AntNetConfigs.Instance.Max_Health;

            for (int x = xQcoord - QueenSightRadius; x <= xQcoord + QueenSightRadius; x++)
            {
                for (int z = zQcoord - QueenSightRadius; z <= zQcoord + QueenSightRadius; z++)
                {
                    if (x < 0 || x >= AntDist.GetLength(0) || z < 0 || z >= AntDist.GetLength(1))
                    {
                        indexQ += 3;
                    }
                    else
                    {
                        inputQ[indexQ++] = BlockToFloat(WorldManager.Instance.GetBlock(x, WorldManager.Instance.Topography[x, z], z));
                        inputQ[indexQ++] = (WorldManager.Instance.Topography[x, z] - yQcoord) * 0.1f;
                        inputQ[indexQ++] = (AntDist[x, z]) * 0.5f;
                    }
                }
            }
            queen.TakeTurn(inputQ);
        }
    }

    private void CreateInitQueens()
    {
        for (int i = 0; i < AntNetConfigs.Instance.Num_Ants * 2; i++)
        {
            GameObject queenObj = Instantiate(queenAntPrefab);
            QueenAnt queen = queenObj.GetComponent<QueenAnt>();
            queen.InitLayers();
            queen.Init(RNG.Next(-20, 20), RNG.Next(-20, 20));
            queen.InitRandomNetwork();
            initQueens.Add(queen);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Runs the loops for init and evaluation
    /// </summary>
    void Update()
    {
        if (QueenInit) InitializeQueens();
        else if (AntInit) InitializeAnts();
        else
        {
            RunAntTurns();
            RunQueenTurn();
        }
        GenText.text = "Gen : " + gen + "\nSubset : " + subset;
    }

    /// <summary>
    /// Checks if max ticks has been reached and ends colony if it has
    /// </summary>
    private void LateUpdate()
    {
        if (WorldManager.Instance.Ticks >= 2000 && theQueen.health != 0 && !QueenInit && !AntInit) ColonyDead();
    }

    /// <summary>
    /// Tells the Queen to take their turn.
    /// also gives the Queen their input
    /// </summary>
    private void RunQueenTurn()
    {
        int xQcoord = (int)theQueen.transform.position.x;
        int zQcoord = (int)theQueen.transform.position.z;
        int yQcoord = CustomMath.fastfloor(theQueen.transform.position.y) + 1;
        float[] inputQ = new float[AntNetConfigs.Instance.Queen_Net_Extra_Inputs + AntNetConfigs.Instance.Queen_Net_Env_Inputs * AntNetConfigs.Instance.Queen_Net_Env_Features];
        int indexQ = 0;
        inputQ[indexQ++] = GlobalFeeling;
        inputQ[indexQ++] = theQueen.transform.position.x / AntDist.GetLength(0);
        inputQ[indexQ++] = theQueen.transform.position.z / AntDist.GetLength(1);
        inputQ[indexQ++] = theQueen.transform.position.y / (ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter);
        inputQ[indexQ++] = (float)Ants.Count / (AntNetConfigs.Instance.Num_Ants);
        inputQ[indexQ++] = (float)theQueen.health / AntNetConfigs.Instance.Max_Health;

        for (int x = xQcoord - QueenSightRadius; x <= xQcoord + QueenSightRadius; x++)
        {
            for (int z = zQcoord - QueenSightRadius; z <= zQcoord + QueenSightRadius; z++)
            {
                if (x < 0 || x >= AntDist.GetLength(0) || z < 0 || z >= AntDist.GetLength(1))
                {
                    indexQ += 3;
                }
                else
                {
                    inputQ[indexQ++] = BlockToFloat(WorldManager.Instance.GetBlock(x, WorldManager.Instance.Topography[x, z], z));
                    inputQ[indexQ++] = (WorldManager.Instance.Topography[x, z] - yQcoord) * 0.1f;
                    inputQ[indexQ++] = (AntDist[x, z]) * 0.5f;
                }
            }
        }
        theQueen.TakeTurn(inputQ);
        QueenCommand = (float)Math.Sin(theQueen.command);
    }

    /// <summary>
    /// Tells all ants to take their turn.
    /// also gives all ants their input
    /// </summary>
    private void RunAntTurns()
    {
        float feelsum = 0.0f;
        foreach (Ant ant in Ants)
        {
            if (!ant.gameObject.activeSelf) continue;
            int xcoord = (int)ant.transform.position.x;
            int zcoord = (int)ant.transform.position.z;
            int ycoord = CustomMath.fastfloor(ant.transform.position.y) + 1;
            float[] input = new float[AntNetConfigs.Instance.Ant_Net_Extra_Inputs + AntNetConfigs.Instance.Ant_Net_Env_Inputs * AntNetConfigs.Instance.Ant_Net_Env_Features];
            int index = 0;
            input[index++] = theQueen.transform.position.x / AntDist.GetLength(0);
            input[index++] = theQueen.transform.position.z / AntDist.GetLength(1);
            input[index++] = theQueen.transform.position.y / (ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter);
            input[index++] = QueenCommand;
            input[index++] = theQueen.health / AntNetConfigs.Instance.Max_Health;
            input[index++] = GlobalFeeling;
            input[index++] = ant.transform.position.x / AntDist.GetLength(0);
            input[index++] = ant.transform.position.z / AntDist.GetLength(1);
            input[index++] = ant.transform.position.y / (ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter);
            input[index++] = ant.health / AntNetConfigs.Instance.Max_Health;

            for (int x = xcoord - SightRadius; x <= xcoord + SightRadius; x++)
            {
                for (int z = zcoord - SightRadius; z <= zcoord + SightRadius; z++)
                {
                    if (x < 0 || x >= AntDist.GetLength(0) || z < 0 || z >= AntDist.GetLength(1))
                    {
                        index += 3;
                    }
                    else
                    {
                        input[index++] = BlockToFloat(WorldManager.Instance.GetBlock(x, WorldManager.Instance.Topography[x, z], z));
                        input[index++] = ((WorldManager.Instance.Topography[x, z]) - ycoord) * 0.1f;
                        input[index++] = (AntDist[x, z]) * 0.5f;
                    }

                }
            }
            ant.TakeTurn(input);
            feelsum += ant.feelings;
        }
        GlobalFeeling = (float)Math.Sin(feelsum / Ants.Count);
    }

    /// <summary>
    /// Wraps up Colony and calls NewColony
    /// </summary>
    public void ColonyDead()
    {
        if (theQueen.takenActions.Sum() > 1)
        {
            QueenGeneHistory.Add(theQueen.genetics);
            QueenRewardHistory.Add(theQueen.GetScore());
        }
        foreach (Ant ant in Ants) {
            if (ant.takenActions.Sum() > 1)
            {
                AntGeneHistory.Add(ant.genetics);
                AntRewardHistory.Add(ant.GetScore() + theQueen.nestBlocksPlaced);
            }
        }
        NewColony();
    }

    /// <summary>
    /// Starts a new colony from waiting ants, calls for new ants to be created if needed
    /// </summary>
    private void NewColony()
    {
        if (WaitingQueenGenes.Count == 0)
        {
            gen++;
            subset = 0;
            GenerateNewQueens();
            GenerateNewAnts();
        }

        AntDist = new int[
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter,
            ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter];

        WorldManager.Instance.CreateNewWorld();
        theQueen.Init(0, 0);
        theQueen.genetics = WaitingQueenGenes.Dequeue();
        CreateAnts();
        QueenCommand = 0.5f;
        GlobalFeeling = 0.5f;
        subset++;
    }

    /// <summary>
    /// Re-Initializes all ants with genes from the waiting queue
    /// </summary>
    private void CreateAnts()
    {
        foreach (Ant ant in Ants)
        {
            ant.Init(RNG);
            ant.genetics = WaitingAntGenes.Dequeue();
        }
    }

    #endregion

    #region Helpers

    private float BlockToFloat(AbstractBlock block)
    {
        float result = 0f;
        if (block as AcidicBlock != null)
        {
            result = 0.5f;
        }
        else if (block as StoneBlock != null)
        {
            result = 1.0f;
        }
        else if (block as GrassBlock != null)
        {
            result = 1.5f;
        }
        else if (block as MulchBlock != null)
        {
            result = 2.0f;
        }
        else if (block as NestBlock != null)
        {
            result = 2.5f;
        }
        return result;
    }

    public void PrintList(int antid, float[] antList, string message)
    {
        string listprint = "[ ";
        for (int ind = 0; ind < antList.Length - 1; ind++)
        {
            listprint += antList[ind] + ", ";
        }
        listprint += antList[antList.Length - 1] + " ]";
        Debug.Log("ant: " + antid + " " + message + ": " + listprint);
    }

    #endregion

    #region Genetics

    private void GenerateNewQueens()
    {
        int rand;
        for (int i = 0; i < AntNetConfigs.Instance.Num_Subsets; i++)
        {
            rand = RNG.Next(7);
            if (rand < 2)
            {
                int parent1 = RNG.Next(QueenRewardHistory.Count);
                int parent2 = RNG.Next(QueenRewardHistory.Count);
                for (int j = 0; j < 10; j++)
                {
                    int contender1 = RNG.Next(0, QueenRewardHistory.Count);
                    int contender2 = RNG.Next(0, QueenRewardHistory.Count);
                    if (QueenRewardHistory[parent1] < QueenRewardHistory[contender1]) parent1 = contender1;
                    if (QueenRewardHistory[parent2] < QueenRewardHistory[contender2]) parent2 = contender2;
                }
                CreateQueen(parent1, parent2);
            }
            else if (rand < 4)
            {
                int parent1 = RNG.Next(QueenRewardHistory.Count);
                int parent2 = RNG.Next(QueenRewardHistory.Count);
                for (int j = 0; j < 10; j++)
                {
                    int contender1 = RNG.Next(0, QueenRewardHistory.Count);
                    int contender2 = RNG.Next(0, QueenRewardHistory.Count);
                    if (QueenRewardHistory[parent1] < QueenRewardHistory[contender1]) parent1 = contender1;
                    if (QueenRewardHistory[parent2] < QueenRewardHistory[contender2]) parent2 = contender2;
                }
                CreateStableQueen(parent1, parent2);
            }
            else if (rand < 6)
            {
                int parent1 = RNG.Next(0, QueenRewardHistory.Count);
                for (int j = 0; j < 10; j++)
                {
                    int contender1 = RNG.Next(QueenRewardHistory.Count);
                    if (QueenRewardHistory[parent1] < QueenRewardHistory[contender1]) parent1 = contender1;
                }
                MutateQueen(parent1);
            }
            else
            {
                int parent1 = RNG.Next(0, QueenRewardHistory.Count);
                for (int j = 0; j < 50; j++)
                {
                    int contender1 = RNG.Next(QueenRewardHistory.Count);
                    if (QueenRewardHistory[parent1] < QueenRewardHistory[contender1]) parent1 = contender1;
                }
                PreserveQueen(parent1);
            }
        }
        if (QueenGeneHistory.Count >= 50 * AntNetConfigs.Instance.Num_Subsets)
        {
            QueenGeneHistory.RemoveRange(0, 20 * AntNetConfigs.Instance.Num_Subsets);
            QueenRewardHistory.RemoveRange(0, 20 * AntNetConfigs.Instance.Num_Subsets);
        }
    }

    private void GenerateNewAnts()
    {
        int rand;
        for (int i = 0; i < AntNetConfigs.Instance.Num_Ants * AntNetConfigs.Instance.Num_Subsets; i++)
        {
            rand = RNG.Next(7);
            if (rand < 2)
            {
                int parent1 = RNG.Next(AntRewardHistory.Count);
                int parent2 = RNG.Next(AntRewardHistory.Count);
                for (int j = 0; j < 20; j++)
                {
                    int contender1 = RNG.Next(0, AntRewardHistory.Count);
                    int contender2 = RNG.Next(0, AntRewardHistory.Count);
                    if (AntRewardHistory[parent1] < AntRewardHistory[contender1]) parent1 = contender1;
                    if (AntRewardHistory[parent2] < AntRewardHistory[contender2]) parent2 = contender2;
                }
                CreateChild(parent1, parent2);
            }
            else if (rand < 4)
            {
                int parent1 = RNG.Next(AntRewardHistory.Count);
                int parent2 = RNG.Next(AntRewardHistory.Count);
                for (int j = 0; j < 20; j++)
                {
                    int contender1 = RNG.Next(0, AntRewardHistory.Count);
                    int contender2 = RNG.Next(0, AntRewardHistory.Count);
                    if (AntRewardHistory[parent1] < AntRewardHistory[contender1]) parent1 = contender1;
                    if (AntRewardHistory[parent2] < AntRewardHistory[contender2]) parent2 = contender2;
                }
                CreateStableChild(parent1, parent2);
            }
            else if(rand < 6)
            {
                int parent1 = RNG.Next(0, AntRewardHistory.Count);
                for (int j = 0; j < 20; j++)
                {
                    int contender1 = RNG.Next(AntRewardHistory.Count);
                    if (AntRewardHistory[parent1] < AntRewardHistory[contender1]) parent1 = contender1;
                }
                MutateAnt(parent1);
            }
            else
            {
                int parent1 = RNG.Next(0, AntRewardHistory.Count);
                for (int j = 0; j < 50; j++)
                {
                    int contender1 = RNG.Next(AntRewardHistory.Count);
                    if (AntRewardHistory[parent1] < AntRewardHistory[contender1]) parent1 = contender1;
                }
                PreserveAnt(parent1);
            }
        }
        if (AntRewardHistory.Count >= AntNetConfigs.Instance.Num_Ants * AntNetConfigs.Instance.Num_Subsets * 5)
        {
            AntGeneHistory.RemoveRange(0, AntNetConfigs.Instance.Num_Ants * AntNetConfigs.Instance.Num_Subsets * 2);
            AntRewardHistory.RemoveRange(0, AntNetConfigs.Instance.Num_Ants * AntNetConfigs.Instance.Num_Subsets * 2);
        }
    }

    private void PreserveQueen(int p1)
    {
        float[] childGenes = new float[QueenGeneHistory[p1].Length];
        QueenGeneHistory[p1].CopyTo(childGenes, 0);
        for (int i = 0; i < QueenGeneHistory[p1].Length; i++)
        {
            int rand = RNG.Next(100);
            if (rand == 0) childGenes[i] += UnityEngine.Random.Range(-0.05f, 0.05f);
        }
        WaitingQueenGenes.Enqueue(childGenes);
    }

    private void MutateQueen(int p1)
    {
        float[] childGenes = new float[QueenGeneHistory[p1].Length];
        QueenGeneHistory[p1].CopyTo(childGenes, 0);
        for (int i = 0; i < QueenGeneHistory[p1].Length; i++)
        {
            int rand = RNG.Next(20);
            if (rand < 6) childGenes[i] += UnityEngine.Random.Range(-0.02f * rand, 0.02f * rand);
        }
        WaitingQueenGenes.Enqueue(childGenes);
    }

    private void MutateQueenGenes(float[] childGenes)
    {
        for (int i = 0; i < childGenes.Length; i++)
        {
            int rand = RNG.Next(10);
            if (rand == 0) childGenes[i] += UnityEngine.Random.Range(-0.1f, 0.1f);
        }
        WaitingQueenGenes.Enqueue(childGenes);
    }

    private void CreateQueen(int p1, int p2)
    {
        float[] childGenes = new float[QueenGeneHistory[p1].Length];
        for (int i = 0; i < QueenGeneHistory[p1].Length; i++)
        {
            int rand = RNG.Next(3);
            if (rand == 0) childGenes[i] = (QueenGeneHistory[p2][i] + QueenGeneHistory[p1][i]) / 2.0f;
            else if (rand == 1) childGenes[i] = QueenGeneHistory[p1][i];
            else childGenes[i] = QueenGeneHistory[p2][i];
        }
        MutateQueenGenes(childGenes);
    }

    private void CreateStableQueen(int p1, int p2)
    {
        float[] childGenes = new float[QueenGeneHistory[p1].Length];
        int rand = RNG.Next(2); ;
        for (int i = 0; i < QueenGeneHistory[p1].Length; i++)
        {
            if (i % (QueenGeneHistory[p1].Length / 10) == 0) rand = RNG.Next(2);
            if (rand == 0) childGenes[i] = QueenGeneHistory[p1][i];
            else childGenes[i] = QueenGeneHistory[p2][i];
        }
        MutateQueenGenes(childGenes);
    }

    private void PreserveAnt(int p1)
    {
        float[] childGenes = new float[AntGeneHistory[p1].Length];
        AntGeneHistory[p1].CopyTo(childGenes, 0);
        for (int i = 0; i < AntGeneHistory[p1].Length; i++)
        {
            int rand = RNG.Next(100);
            if (rand == 0) childGenes[i] += UnityEngine.Random.Range(-0.05f, 0.05f);
        }
        WaitingAntGenes.Enqueue(childGenes);
    }

    private void MutateAnt(int p1)
    {
        float[] childGenes = new float[AntGeneHistory[p1].Length];
        AntGeneHistory[p1].CopyTo(childGenes, 0);
        for (int i = 0; i < AntGeneHistory[p1].Length; i++)
        {
            int rand = RNG.Next(5);
            if (rand == 0) childGenes[i] += UnityEngine.Random.Range(-0.05f, 0.05f);
        }
        WaitingAntGenes.Enqueue(childGenes);
    }

    private void MutateAntGenes(float[] childGenes)
    {
        for (int i = 0; i < childGenes.Length; i++)
        {
            int rand = RNG.Next(10);
            if (rand == 0) childGenes[i] += UnityEngine.Random.Range(-0.1f, 0.1f);
        }
        WaitingAntGenes.Enqueue(childGenes);
    }

    private void CreateChild(int p1, int p2)
    {
        float[] childGenes = new float[AntGeneHistory[p1].Length];
        for (int i = 0; i < AntGeneHistory[p1].Length; i++)
        {
            int rand = RNG.Next(3);
            if (rand == 0) childGenes[i] = (childGenes[i] + AntGeneHistory[p1][i]) / 2.0f;
            else if (rand == 1) childGenes[i] = AntGeneHistory[p1][i];
            else childGenes[i] = AntGeneHistory[p2][i];
        }
        MutateAntGenes(childGenes);
    }

    private void CreateStableChild(int p1, int p2)
    {
        float[] childGenes = new float[AntGeneHistory[p1].Length];
        int rand = RNG.Next(2); ;
        for (int i = 0; i < AntGeneHistory[p1].Length; i++)
        {
            if (i % (AntGeneHistory[p1].Length / 10) == 0) rand = RNG.Next(2);
            if (rand == 0) childGenes[i] = AntGeneHistory[p1][i];
            else childGenes[i] = AntGeneHistory[p2][i];
        }
        MutateAntGenes(childGenes);
    }

    #endregion
}
