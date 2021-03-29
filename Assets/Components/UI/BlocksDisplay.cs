using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlocksDisplay : MonoBehaviour
{
    private int nestBlocks = 0;
    public Text nestBlockText;
    // Update is called once per frame
    void Update()
    {
        nestBlockText.text = "Nest Blocks : " + nestBlocks;
    }
    public void AddNestBlock()
    {
        nestBlocks++;
    }
}
