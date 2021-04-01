using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntNetConfigs : Singleton<AntNetConfigs>
{

    /// <summary>
    /// number of ants to spawn.
    /// </summary>
    public int Num_Ants = 100;

    /// <summary>
    /// number of ant subsets in each generation
    /// </summary>
    public int Num_Subsets = 10;

    /// <summary>
    /// max health of ants and queen.
    /// </summary>
    public int Max_Health = 500;

    /// <summary>
    /// start health of ants.
    /// </summary>
    public int Start_Health = 250;

    /// <summary>
    /// start health of ants.
    /// </summary>
    public int Mulch_Health_Gain = 100;

    /// <summary>
    /// start health of ants.
    /// </summary>
    public int Max_Health_Gift = 100;

    /// <summary>
    /// start health of ants.
    /// </summary>
    public int Max_Stone = 10;

    /// <summary>
    /// start health of ants.
    /// </summary>
    public int Max_Grass = 10;

    /// <summary>
    /// The number of extra inputs for ant neural networks
    /// </summary>
    public int Ant_Net_Extra_Inputs = 10;

    /// <summary>
    /// The number of environment inputs for ant neural networks, must be a perfect square with uneven root
    /// </summary>
    public int Ant_Net_Env_Inputs = 25;

    /// <summary>
    /// The number of environment features for ant neural networks
    /// </summary>
    public int Ant_Net_Env_Features = 3;

    /// <summary>
    /// The number of outputs for ant neural networks
    /// </summary>
    public int Ant_Net_Outputs = 11;

    /// <summary>
    /// The number of hidden layer 1 nodes for ant neural networks
    /// </summary>
    public int Ant_Net_HL1_Nodes = 32;

    /// <summary>
    /// The number of hidden layer 2 nodes for ant neural networks
    /// </summary>
    public int Ant_Net_HL2_Nodes = 24;

    /// <summary>
    /// The number of hidden layer 3 nodes for ant neural networks
    /// </summary>
    public int Ant_Net_HL3_Nodes = 24;

    /// <summary>
    /// The number of extra inputs for Queen neural networks
    /// </summary>
    public int Queen_Net_Extra_Inputs = 6;

    /// <summary>
    /// The number of environment inputs for ant neural networks, must be a perfect square with uneven root
    /// </summary>
    public int Queen_Net_Env_Inputs = 121;

    /// <summary>
    /// The number of environment features for ant neural networks
    /// </summary>
    public int Queen_Net_Env_Features = 3;

    /// <summary>
    /// The number of outputs for ant neural networks
    /// </summary>
    public int Queen_Net_Outputs = 11;

    /// <summary>
    /// The number of hidden layer 1 nodes for ant neural networks
    /// </summary>
    public int Queen_Net_HL1_Nodes = 98;

    /// <summary>
    /// The number of hidden layer 2 nodes for ant neural networks
    /// </summary>
    public int Queen_Net_HL2_Nodes = 64;

    /// <summary>
    /// The number of hidden layer 2 nodes for ant neural networks
    /// </summary>
    public int Queen_Net_HL3_Nodes = 64;
}
