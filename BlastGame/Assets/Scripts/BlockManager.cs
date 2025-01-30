using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    [SerializeField, Range(2,20)] private int rowWidth = 10;
    [SerializeField, Range(2,20)] private int rowHeight = 10;
    private int[,] tileMap;
    
    private List<Block> GridBlocks { get; set; }

    void Awake()
    {
        tileMap = new int[rowWidth, rowHeight];
    }

    private void PopulateGrid()
    {
         
    }
    
}
