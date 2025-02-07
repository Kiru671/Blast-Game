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
    [SerializeField, Range(2,6)] private int colorCount = 6;
    
    public float BlockSpacing => blockSpacing;
    public int RowWidth => rowWidth;
    public int RowHeight => rowHeight;
    public int ColorCount => colorCount;

    private Block[,] tileMap;
    public List<Block> GridBlocks;
    private HashSet<Vector2Int> visitedPositions;
    private bool isInitialized = false;
    private bool isProcessing = false;
    [SerializeField] private BlockCurator curator;

    void Awake()
    {  
        tileMap = new Block[rowWidth, rowHeight];
        
        if (blockPool == null)
        {
            Debug.LogError("No BlockPool reference!");
            return;
        }

        if (curator == null)
        {
            Debug.LogError("No BlockCurator component!");
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
            Debug.LogError("BlockManager not initialized!");
            return;
        }
        
        PopulateGrid();
    }

    private void PopulateGrid()
    {
        Dictionary<Vector2Int, Block.BlockColor> placedBlocks = new Dictionary<Vector2Int, Block.BlockColor>();
        curator.ResetInitialGroupCount();

        for (int x = 0; x < rowWidth; x++)
        {
            for (int y = 0; y < rowHeight; y++)
            {
                Vector2Int position = new Vector2Int(x, y);
                Block.BlockColor color = curator.GetColorForPosition(position, placedBlocks);
                CreateBlockAt(position, color);
                placedBlocks[position] = color;
            }
        }
    }

    public void HandleBlockClick(Block clickedBlock)
    {
        StartCoroutine(ProcessBlockClick(clickedBlock));
    }

    private IEnumerator ProcessBlockClick(Block clickedBlock)
    {
        isProcessing = true;

        List<Block> connectedBlocks = GetConnectedBlocks(clickedBlock);

        if (connectedBlocks.Count >= 2)
        {
            foreach (Block block in connectedBlocks)
            {
                Vector2Int pos = block.GridPosition;
                tileMap[pos.x, pos.y] = null;
                GridBlocks.Remove(block);
                blockPool.Release(block);
            }

            yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(ApplyGravity());

            yield return StartCoroutine(ScanAndAssignGroups());
        }
        FillEmptySpaces();
        yield return StartCoroutine(ApplyGravity());
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

                        tileMap[x, y] = null;
                        tileMap[x, y - 1] = block;
                        block.SetGridPosition(newPos);
                        block.MoveToPosition(targetPosition);
                        moved = true;
                    }
                }
            }

            if (moved)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        while (moved);

        StartCoroutine(ScanAndAssignGroups());
    }

    private void FillEmptySpaces()
    {
        Dictionary<int, int> emptySpacesPerColumn = new Dictionary<int, int>();

        for (int x = 0; x < rowWidth; x++)
        {
            int emptyCount = CountEmptySpacesInColumn(x);
            if (emptyCount > 0)
            {
                emptySpacesPerColumn[x] = emptyCount;
            }
        }

        foreach (var entry in emptySpacesPerColumn)
        {
            int column = entry.Key;
            int missingBlocks = entry.Value;
        
            for (int i = 0; i < missingBlocks; i++)
            {
                SpawnBlockAboveGrid(column, i);
            }
        }

        StartCoroutine(ApplyGravity());
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
        return new Vector2Int(column, rowHeight - 1);
    }

    private IEnumerator ScanAndAssignGroups()
    {
        foreach (Block block in GridBlocks)
        {
            if (block != null)
            {
                block.UpdateGroupSize();
            }
        }
        yield return null;
    }

    private void CreateBlockAt(Vector2Int position, Block.BlockColor color)
    {
        if (!IsValidPosition(position) && position.y < rowHeight) return;

        Block block = blockPool.Get();
        if (block == null)
        {
            Debug.LogError("Failed to get block from pool");
            return;
        }

        block.transform.position = new Vector3(
            position.x * blockSpacing,
            position.y * blockSpacing,
            0
        );

        tileMap[position.x, position.y] = block;
        GridBlocks.Add(block);

        block.Initialize(color, position, this);
    }

    private Block.BlockColor GetRandomColor()
    {
        colorCount = Mathf.Clamp(colorCount, 2, System.Enum.GetValues(typeof(Block.BlockColor)).Length);
        return (Block.BlockColor)Random.Range(0, colorCount);
    }

    public int GetGroupSize(Block block)
    {
        return CountConnectedBlocks(block.GridPosition, block.Color);
    }
    
    private int CountConnectedBlocks(Vector2Int startPosition, Block.BlockColor color)
    {
        if (!IsValidPosition(startPosition)) return 0;
        
        Block startBlock = tileMap[startPosition.x, startPosition.y];

        visitedPositions.Clear();
        Queue<Vector2Int> toVisit = new Queue<Vector2Int>();
        List<Block> connectedBlocks = new List<Block>();
    
        toVisit.Enqueue(startPosition);
        visitedPositions.Add(startPosition);
        connectedBlocks.Add(startBlock);
    
        while (toVisit.Count > 0)
        {
            Vector2Int currentPos = toVisit.Dequeue();

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

                if (!IsValidPosition(neighborPos) || visitedPositions.Contains(neighborPos))
                    continue;

                Block neighbor = tileMap[neighborPos.x, neighborPos.y];

                if (neighbor != null && neighbor.Color == color)
                {
                    toVisit.Enqueue(neighborPos);
                    visitedPositions.Add(neighborPos);
                    connectedBlocks.Add(neighbor);
                }
            }
        }

        int groupSize = connectedBlocks.Count;

        foreach (Block block in connectedBlocks)
        {
            block.GroupSize = groupSize;
            block.UpdateSprite();
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
    public void SpawnBlockAboveGrid(int column, int offset)
    {
        Block block = blockPool.Get();
        if (block == null)
        {
            Debug.LogError("Couldn't get block from pool");
            return;
        }

        // Get existing blocks for context
        Dictionary<Vector2Int, Block.BlockColor> existingBlocks = new Dictionary<Vector2Int, Block.BlockColor>();
        for (int x = 0; x < rowWidth; x++)
        {
            for (int y = 0; y < rowHeight; y++)
            {
                if (tileMap[x, y] != null)
                {
                    existingBlocks[new Vector2Int(x, y)] = tileMap[x, y].Color;
                }
            }
        }

        Vector2Int spawnPosition = new Vector2Int(column, rowHeight - 1);
        Block.BlockColor color = curator.GetColorForSpawn(spawnPosition, existingBlocks);

        // Spawn higher for each missing block
        Vector2Int abovePosition = new Vector2Int(column, rowHeight + offset);
        Vector3 spawnPosition3D = new Vector3(
            column * blockSpacing,
            (rowHeight + offset) * blockSpacing,
            0
        );

        block.transform.position = spawnPosition3D;
        block.Initialize(color, abovePosition, this);
        GridBlocks.Add(block);

        Vector2Int newPos = FindLowestEmptySpace(column);
        MoveBlockToGrid(block, newPos);
    }
}