using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;
using Antymology.Helpers;
using System;

public class QueenAnt : MonoBehaviour
{
    public int health = 10000;
    public int nestCost;
    public int maxStone;
    public int maxGrass;
    public WorldManager worldManager;
    private int numStone = 0;
    private int numGrass = 0;
    private int aboveAcid;
    private BlocksDisplay display;

    public void Init(WorldManager wm)
    {
        worldManager = wm;
        GameObject textObj = GameObject.FindGameObjectWithTag("TextDisplay");
        display = textObj.GetComponent<BlocksDisplay>();
        int xcoord = (int)transform.position.x;
        int zcoord = (int)transform.position.z;
        int ycoord = CustomMath.fastfloor(transform.position.y);
        AbstractBlock target = worldManager.GetBlock(xcoord, ycoord, zcoord);
        if (target as AcidicBlock != null)
        {
            aboveAcid = 2;
        }
        else
        {
            aboveAcid = 1;
        }
        nestCost = health / 3;
    }

    // if action is 0, does nothing
    public void DoAction(int action)
    {
        if (action == 1)
        {
            Move(1, 0);
        }
        else if (action == 2)
        {
            Move(0, 1);
        }
        else if (action == 3)
        {
            Move(-1, 0);
        }
        else if (action == 4)
        {
            Move(0, -1);
        }
        else if (action == 5)
        {
            DestroyBlock();
        }
        else if (action == 6)
        {
            PlaceGrass();
        }
        else if (action == 7)
        {
            PlaceStone();
        }
        else if (action == 8)
        {
            PlaceNest();
        }
        health -= aboveAcid;
        if (health <= 0)
        {
            Debug.Log("The Queen is Dead :(");
            Destroy(gameObject);
        }
    }

    private void PlaceNest()
    {
        if (health > nestCost)
        {
            transform.position += new Vector3(0f, 1f, 0f);
            int xcoord = (int)transform.position.x;
            int zcoord = (int)transform.position.z;
            int ycoord = CustomMath.fastfloor(transform.position.y);
            worldManager.SetBlock(xcoord, ycoord, zcoord, new NestBlock());
            display.AddNestBlock();
            health -= nestCost;
            Debug.Log("health after nest block placement: " + health);
        }
    }

    public void DestroyBlock()
    {
        if (SharesSpace()) return;
        int xcoord = (int)transform.position.x;
        int zcoord = (int)transform.position.z;
        int ycoord = CustomMath.fastfloor(transform.position.y);
        AbstractBlock target = worldManager.GetBlock(xcoord, ycoord, zcoord);
        if (target as MulchBlock != null)
        {
            worldManager.SetBlock(xcoord, ycoord, zcoord, new AirBlock());
            health += 1000;
            Debug.Log("health after mulch eat: " + health);
            transform.position += new Vector3(0f, -1f, 0f);
        }
        else if (target as StoneBlock != null)
        {
            worldManager.SetBlock(xcoord, ycoord, zcoord, new AirBlock());
            if (numStone < maxStone) numStone += 1;
            transform.position += new Vector3(0f, -1f, 0f);
        }
        else if (target as GrassBlock != null)
        {
            worldManager.SetBlock(xcoord, ycoord, zcoord, new AirBlock());
            if (numGrass < maxGrass) numGrass += 1;
            transform.position += new Vector3(0f, -1f, 0f);
        }
        else if (target as AcidicBlock != null)
        {
            worldManager.SetBlock(xcoord, ycoord, zcoord, new AirBlock());
            transform.position += new Vector3(0f, -1f, 0f);
        }
    }

    public void PlaceStone()
    {
        if (numStone > 0)
        {
            transform.position += new Vector3(0f, 1f, 0f);
            int xcoord = (int)transform.position.x;
            int zcoord = (int)transform.position.z;
            int ycoord = CustomMath.fastfloor(transform.position.y);
            worldManager.SetBlock(xcoord, ycoord, zcoord, new StoneBlock());
            numStone -= 1;
        }
    }

    public void PlaceGrass()
    {
        if (numGrass > 0)
        {
            transform.position += new Vector3(0f, 1f, 0f);
            int xcoord = (int)transform.position.x;
            int zcoord = (int)transform.position.z;
            int ycoord = CustomMath.fastfloor(transform.position.y);
            worldManager.SetBlock(xcoord, ycoord, zcoord, new GrassBlock());
            numGrass -= 1;
        }
    }

    private bool SharesSpace()
    {
        foreach (Ant ant in worldManager.Ants)
        {
            if (ant != this && ant.GetPosition() == transform.position)
            {
                return true;
            }
        }
        return false;
    }

    public void Move(int dx, int dz)
    {
        int xcoord = (int)transform.position.x + dx;
        int zcoord = (int)transform.position.z + dz;
        int ycoord = CustomMath.fastfloor(transform.position.y) + 1;
        int dy = -3;
        AbstractBlock target = worldManager.GetBlock(xcoord, ycoord + 2, zcoord);
        if (target as AirBlock == null) return;
        for (int j = 2; j >= -2; j--)
        {
            target = worldManager.GetBlock(xcoord, ycoord + j - 1, zcoord);
            if (target as AirBlock == null)
            {
                dy = j;
                break;
            }
        }
        if (dy == -3) return;
        transform.position = new Vector3(xcoord, transform.position.y + dy, zcoord);
        if (target as AcidicBlock != null)
        {
            aboveAcid = 2;
        }
        else
        {
            aboveAcid = 1;
        }

    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void getHealed(int gift)
    {
        health += gift;
        Debug.Log("health after getting healed: " + health);
    }

}
