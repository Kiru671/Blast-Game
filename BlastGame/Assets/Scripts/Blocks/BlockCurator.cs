using UnityEngine;
using System.Collections.Generic;

public class BlockCurator : MonoBehaviour // Not really working as intended as of now...
{
    [SerializeField, Range(3, 5)] private int minimumGroupSize = 3;
    [SerializeField, Range(1, 10)] private int minimumInitialGroups = 3;
    [SerializeField, Range(0.3f, 0.7f)] private float spawnGroupProbability = 0.4f;
    [SerializeField] private BlockManager blockManager;
    
    private int currentInitialGroups = 0;

    private void Awake()
    {
        if (blockManager == null)
        {
            Debug.LogError("BlockCurator needs a BlockManager component.");
            enabled = false;
            return;
        }
    }

    public void ResetInitialGroupCount()
    {
        currentInitialGroups = 0;
    }

    public Block.BlockColor GetColorForPosition(Vector2Int position, Dictionary<Vector2Int, Block.BlockColor> existingBlocks)
    {
        if (currentInitialGroups < minimumInitialGroups)
        {
            Block.BlockColor groupColor = GetColorForGroup(position, existingBlocks);
            if (groupColor != Block.BlockColor.Pink)
            {
                currentInitialGroups++;
                return groupColor;
            }
        }

        return (Block.BlockColor)Random.Range(0, blockManager.ColorCount);
    }

    public Block.BlockColor GetColorForSpawn(Vector2Int position, Dictionary<Vector2Int, Block.BlockColor> existingBlocks)
    {
        if (Random.value <= spawnGroupProbability)
        {
            Block.BlockColor groupColor = GetColorForGroup(position, existingBlocks);
            if (groupColor != Block.BlockColor.Pink)
            {
                return groupColor;
            }
        }

        return (Block.BlockColor)Random.Range(0, blockManager.ColorCount);
    }

    private Block.BlockColor GetColorForGroup(Vector2Int position, Dictionary<Vector2Int, Block.BlockColor> existingBlocks)
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        for (int i = 0; i < blockManager.ColorCount; i++)
        {
            Block.BlockColor testColor = (Block.BlockColor)i;
            List<Vector2Int> groupPositions = new List<Vector2Int>();
            groupPositions.Add(position);

            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            visited.Add(position);
            CheckNeighborsRecursively(position, testColor, existingBlocks, visited, groupPositions);

            if (groupPositions.Count >= minimumGroupSize)
            {
                return testColor;
            }
        }

        return Block.BlockColor.Pink;
    }

    private void CheckNeighborsRecursively(
        Vector2Int pos, 
        Block.BlockColor color, 
        Dictionary<Vector2Int, Block.BlockColor> existingBlocks, 
        HashSet<Vector2Int> visited,
        List<Vector2Int> groupPositions)
    {
        if (groupPositions.Count >= minimumGroupSize)
            return;

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = pos + dir;
            
            if (!IsValidPosition(neighborPos) || visited.Contains(neighborPos))
                continue;

            visited.Add(neighborPos);

            if (!existingBlocks.TryGetValue(neighborPos, out Block.BlockColor neighborColor) ||
                neighborColor == color)
            {
                groupPositions.Add(neighborPos);
                CheckNeighborsRecursively(neighborPos, color, existingBlocks, visited, groupPositions);
            }
        }
    }

    private bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < blockManager.RowWidth &&
               position.y >= 0 && position.y < blockManager.RowHeight;
    }
}