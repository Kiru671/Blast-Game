using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    [SerializeField, Range(2,10)] private int rowWidth = 10;
    [SerializeField, Range(2,10)] private int rowHeight = 10;
    [SerializeField] private float blockSpacing = 1f;
    [SerializeField] private BlockPool blockPool;
    [SerializeField] private float fallSpeed = 10f;
    
    public float BlockSpacing => blockSpacing;
    public int RowWidth => rowWidth;
    public int RowHeight => rowHeight;

    private Block[,] tileMap;
    public List<Block> GridBlocks;
    private HashSet<Vector2Int> visitedPositions;
    private bool isInitialized = false;
    private bool isProcessing = false;

    void Awake()
    {  
        tileMap = new Block[rowWidth, rowHeight];
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

    public void HandleBlockClick(Block clickedBlock)
    {
        if (isProcessing) return; // Prevent multiple clicks while processing

        StartCoroutine(ProcessBlockClick(clickedBlock));
    }

    private IEnumerator ProcessBlockClick(Block clickedBlock)
    {
        isProcessing = true;

        // Get only the connected blocks from the clicked one
        List<Block> connectedBlocks = GetConnectedBlocks(clickedBlock);

        if (connectedBlocks.Count >= 2) // Only remove groups of 2+
        {
            Debug.Log($"Processing click on block at {clickedBlock.GridPosition} with {connectedBlocks.Count} connected blocks");

            // Remove only the clicked group
            foreach (Block block in connectedBlocks)
            {
                Vector2Int pos = block.GridPosition;
                tileMap[pos.x, pos.y] = null;
                GridBlocks.Remove(block);
                blockPool.Release(block);
            }

            // Short delay for visual feedback
            yield return new WaitForSeconds(0.2f);

            // Apply gravity and wait until all blocks settle
            yield return StartCoroutine(ApplyGravity());

            // Once all blocks have settled, scan and update groups
            yield return StartCoroutine(ScanAndAssignGroups());
        }

        isProcessing = false;
    }




    private List<Block> GetConnectedBlocks(Block startBlock)
    {
        List<Block> connectedBlocks = new List<Block>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Block> toCheck = new Queue<Block>();

        toCheck.Enqueue(startBlock);
        visited.Add(startBlock.GridPosition);
        connectedBlocks.Add(startBlock);

        while (toCheck.Count > 0)
        {
            Block current = toCheck.Dequeue();
            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = current.GridPosition + dir;
                if (IsValidPosition(neighborPos) && !visited.Contains(neighborPos))
                {
                    Block neighbor = tileMap[neighborPos.x, neighborPos.y];
                    if (neighbor != null && neighbor.Color == startBlock.Color)
                    {
                        visited.Add(neighborPos);
                        connectedBlocks.Add(neighbor);
                        toCheck.Enqueue(neighbor);
                    }
                }
            }
        }

        return connectedBlocks;
    }

    private IEnumerator ApplyGravity()
    {
        bool moved;
        do
        {
            moved = false;
            for (int x = 0; x < rowWidth; x++)
            {
                for (int y = 1; y < rowHeight; y++)
                {
                    if (tileMap[x, y] != null && tileMap[x, y - 1] == null)
                    {
                        Block block = tileMap[x, y];
                        Vector2Int newPos = new Vector2Int(x, y - 1);
                        Vector3 targetPosition = new Vector3(
                            newPos.x * blockSpacing,
                            newPos.y * blockSpacing,
                            0
                        );

                        // Update grid references
                        tileMap[x, y] = null;
                        tileMap[x, y - 1] = block;
                        block.SetGridPosition(newPos);
                        block.MoveToPosition(targetPosition);
                        moved = true;
                    }
                }
            }

            // Wait a short moment if movement happened
            if (moved)
            {
                yield return new WaitForSeconds(0.1f); // Adjust if needed
            }
        }
        while (moved); // Keep looping until no blocks move

        // Gravity is now finished, update groups
        StartCoroutine(ScanAndAssignGroups());
    }



    private void FillEmptySpaces()
    {
        for (int x = 0; x < rowWidth; x++)
        {
            List<Block> availableBlocks = GetBlocksAboveGrid(x);
            int emptyCount = CountEmptySpacesInColumn(x);

            while (emptyCount > 0)
            {
                if (availableBlocks.Count > 0)
                {
                    // Move an existing block into the empty spot
                    Block block = availableBlocks[0];
                    availableBlocks.RemoveAt(0);

                    Vector2Int newPos = FindLowestEmptySpace(x);
                    MoveBlockToGrid(block, newPos);
                }
                else
                {
                    // If no blocks are available above, pull from the pool
                    SpawnBlockAboveGrid(x);
                }
            
                emptyCount--;
            }
        }
    }

    private void MoveBlockToGrid(Block block, Vector2Int newPos)
    {
        tileMap[newPos.x, newPos.y] = block;
        block.SetGridPosition(newPos);

        Vector3 targetPos = new Vector3(
            newPos.x * blockSpacing,
            newPos.y * blockSpacing,
            0
        );
        block.MoveToPosition(targetPos);
    }

    private List<Block> GetBlocksAboveGrid(int column)
    {
        List<Block> aboveBlocks = new List<Block>();

        foreach (Block block in GridBlocks)
        {
            if (block.GridPosition.x == column && block.GridPosition.y >= rowHeight)
            {
                aboveBlocks.Add(block);
            }
        }

        return aboveBlocks;
    }

    private Vector2Int FindLowestEmptySpace(int column)
    {
        for (int y = 0; y < rowHeight; y++)
        {
            if (tileMap[column, y] == null)
            {
                return new Vector2Int(column, y);
            }
        }
        return new Vector2Int(column, rowHeight - 1); // Fallback (should not happen)
    }

    private IEnumerator ScanAndAssignGroups()
    {
        foreach (Block block in GridBlocks)
        {
            if (block != null)
            {
                block.UpdateGroupSize(); // Only updates, no removal
            }
        }
        yield return null;
    }


    private void PopulateGrid()
    {
        for (int x = 0; x < rowWidth; x++)
        {
            for (int y = 0; y < rowHeight; y++) // Normal grid
            {
                CreateBlockAt(new Vector2Int(x, y));
            }
        }
    }


    private void CreateBlockAt(Vector2Int position)
    {
        if (!IsValidPosition(position) && position.y < rowHeight) return; // Allow positions above the grid

        Block block = blockPool.Get();
        if (block == null)
        {
            Debug.LogError("❌ Failed to get block from pool");
            return;
        }

        Block.BlockColor randomColor = (Block.BlockColor)Random.Range(0, System.Enum.GetValues(typeof(Block.BlockColor)).Length);

        block.transform.position = new Vector3(
            position.x * blockSpacing,
            position.y * blockSpacing,
            0
        );

        tileMap[position.x, position.y] = block;
        GridBlocks.Add(block);

        block.Initialize(randomColor, position, this);
    }


    public int GetGroupSize(Block block)
    {
        int groupSize = CountConnectedBlocks(block.GridPosition, block.Color);
        return groupSize;
    }
    
    private int CountConnectedBlocks(Vector2Int startPosition, Block.BlockColor color)
    {
        Block startBlock = tileMap[startPosition.x, startPosition.y];

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
    private int CountEmptySpacesInColumn(int column)
    {
        int count = 0;
        for (int y = 0; y < rowHeight; y++)
        {
            if (tileMap[column, y] == null)
                count++;
        }
        return count;
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
        Vector2Int abovePosition = new Vector2Int(column, rowHeight);
        Vector3 spawnPosition = new Vector3(
            column * blockSpacing,
            rowHeight * blockSpacing + blockSpacing, // Above the grid
            0
        );

        block.transform.position = spawnPosition;
        block.Initialize(randomColor, abovePosition, this);
        GridBlocks.Add(block);
    }
}