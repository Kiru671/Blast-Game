using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    [SerializeField, Range(2,10)] private int rowWidth = 10;
    [SerializeField, Range(2,10)] private int rowHeight = 10;
    [SerializeField] private float blockSpacing = 1f;
    [SerializeField] private BlockPool blockPool;
    
    public float BlockSpacing => blockSpacing;
    public int RowWidth => rowWidth;
    public int RowHeight => rowHeight;

    private Block[,] tileMap;
    public List<Block> GridBlocks;
    private HashSet<Vector2Int> visitedPositions;
    private bool isInitialized = false;

    void Awake()
    {  
        tileMap = new Block[rowWidth, rowHeight];

        if (tileMap == null)
        {
            Debug.LogError("❌ tileMap is NULL after initialization!");
            return;
        }

        if (blockPool == null)
        {
            Debug.LogError("❌ BlockPool reference is missing!");
            return;
        }

        GridBlocks = new List<Block>();
        visitedPositions = new HashSet<Vector2Int>();
        isInitialized = true;
    }

    void Start()
    {
        if (!isInitialized)
        {
            Debug.LogError("❌ BlockManager not properly initialized!");
            return;
        }
        
        PopulateGrid();
    }

    private void PopulateGrid()
    {
        for (int x = 0; x < rowWidth; x++)
        {
            for (int y = 0; y < rowHeight; y++)
            {
                CreateBlockAt(new Vector2Int(x, y));
            }
        }
    }

    private void CreateBlockAt(Vector2Int position)
    {
        // First, ensure the position is valid
        if (!IsValidPosition(position))
        {
            Debug.LogError($"❌ Attempted to create block at invalid position {position}");
            return;
        }

        // Get a block from the pool
        Block block = blockPool.Get();
        if (block == null)
        {
            Debug.LogError("❌ Failed to get block from pool");
            return;
        }

        // Generate random color
        Block.BlockColor randomColor = (Block.BlockColor)Random.Range(0, System.Enum.GetValues(typeof(Block.BlockColor)).Length);
    
        // Set position
        block.transform.position = new Vector3(
            position.x * blockSpacing,
            position.y * blockSpacing,
            0
        );

        // Store the block in the tileMap BEFORE initializing it
        // This ensures the block is in the tileMap when Initialize triggers any callbacks
        tileMap[position.x, position.y] = block;
        GridBlocks.Add(block);

        // Initialize the block
        block.Initialize(randomColor, position, this);
        
        Debug.Log($"✅ Created block at {position} with color {randomColor}");
    }

    public int GetGroupSize(Block block)
    {
        if (block == null)
        {
            Debug.LogError("❌ GetGroupSize: Block is NULL");
            return 0;
        }

        if (!IsValidPosition(block.GridPosition))
        {
            Debug.LogError($"❌ GetGroupSize: Block position {block.GridPosition} is INVALID");
            return 0;
        }

        Block storedBlock = tileMap[block.GridPosition.x, block.GridPosition.y];
        if (storedBlock == null)
        {
            Debug.LogError($"❌ GetGroupSize: No block found at {block.GridPosition} in tileMap");
            return 0;
        }

        if (storedBlock != block)
        {
            Debug.LogError($"❌ GetGroupSize: Block mismatch at {block.GridPosition}");
            return 0;
        }

        Debug.Log($"✅ Block FOUND at {block.GridPosition} - Color: {storedBlock.Color}");
    
        int groupSize = CountConnectedBlocks(block.GridPosition, block.Color);
        Debug.Log($"✅ GetGroupSize: Block at {block.GridPosition} (Color: {block.Color}) has GroupSize = {groupSize}");
    
        return groupSize;
    }
    
    private int CountConnectedBlocks(Vector2Int startPosition, Block.BlockColor color)
    {
        if (!IsValidPosition(startPosition))
        {
            Debug.LogError($"❌ Invalid Position {startPosition}");
            return 0;
        }
 
        Block startBlock = tileMap[startPosition.x, startPosition.y];
        if (startBlock == null)
        {
            Debug.LogError($"❌ No block at start position {startPosition}");
            return 0;
        }

        visitedPositions.Clear(); // Clear the visited set before starting new search
        Queue<Vector2Int> toVisit = new Queue<Vector2Int>();
        List<Block> connectedBlocks = new List<Block>(); // Keep track of all connected blocks
    
        toVisit.Enqueue(startPosition);
        visitedPositions.Add(startPosition);
        connectedBlocks.Add(startBlock); // Add the first block
    
        while (toVisit.Count > 0)
        {
            Vector2Int currentPos = toVisit.Dequeue();
            Debug.Log($"🔍 Checking Block at {currentPos}");

            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = currentPos + dir;

                if (!IsValidPosition(neighborPos))
                {
                    Debug.Log($"⏭️ Skipping invalid position {neighborPos}");
                    continue;
                }

                if (visitedPositions.Contains(neighborPos))
                {
                    Debug.Log($"⏭️ Already visited {neighborPos}");
                    continue;
                }

                Block neighbor = tileMap[neighborPos.x, neighborPos.y];

                if (neighbor != null && neighbor.Color == color)
                {
                    Debug.Log($"✅ Adding neighbor at {neighborPos}");
                    toVisit.Enqueue(neighborPos);
                    visitedPositions.Add(neighborPos);
                    connectedBlocks.Add(neighbor); // Add the connected block to our list
                }
            }
        }

        int groupSize = connectedBlocks.Count;
        Debug.Log($"✅ Total connected blocks for color {color}: {groupSize}");

        // Update all blocks in the connected group with their group size
        foreach (Block block in connectedBlocks)
        {
            block.GroupSize = groupSize;
            block.UpdateSprite(); // This will trigger the sprite update based on the new group size
        }

        return groupSize;
    }

    private bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < rowWidth &&
               position.y >= 0 && position.y < rowHeight;
    }

    public void SpawnBlockAboveGrid(int column)
    {
        if (column < 0 || column >= rowWidth) return;

        Block block = blockPool.Get();
        Block.BlockColor randomColor = (Block.BlockColor)Random.Range(0, System.Enum.GetValues(typeof(Block.BlockColor)).Length);
        
        // Position above the grid
        Vector3 spawnPosition = new Vector3(
            column * blockSpacing,
            rowHeight * blockSpacing + blockSpacing,
            0
        );
        
        block.transform.position = spawnPosition;
        block.Initialize(randomColor, new Vector2Int(column, rowHeight), this);
    }
}