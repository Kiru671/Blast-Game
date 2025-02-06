using UnityEngine;

public class Block : MonoBehaviour
{
    public enum BlockColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple
    }

    [System.Serializable]
    public class ColorSprites
    {
        public BlockColor color;
        public Sprite[] groupSizeSprites;
    }

    [SerializeField] private ColorSprites[] colorSprites;
    private SpriteRenderer spriteRenderer;

    public BlockColor Color { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public int GroupSize;
    private bool isFalling;
    
    private BlockManager blockManager;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            Debug.Log("SpriteRenderer added to Block");
        }
    }

    private void OnMouseDown()
    {
        Debug.Log("Block clicked");
        if (isFalling) return; // Prevent clicking on moving blocks
        if (blockManager != null && GroupSize >= 2) // Only remove groups of 3 or more
        {
            blockManager.HandleBlockClick(this);
        }
    }

    public void Initialize(BlockColor color, Vector2Int position, BlockManager manager)
    {
        Color = color;
        GridPosition = position;
        blockManager = manager;
        
        UpdateGroupSize();
        UpdateSprite();
        UpdateSortingOrder();
    }

    public void UpdateGroupSize()
    {
        if (blockManager != null && !isFalling)
        {
            int previousGroupSize = GroupSize;
            GroupSize = blockManager.GetGroupSize(this);
        
            if (previousGroupSize != GroupSize)
            {
                UpdateSprite();
            }
        }
    }

    public void UpdateSprite()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer is missing on Block: " + gameObject.name);
            return;
        }

        ColorSprites colorSet = System.Array.Find(colorSprites, x => x.color == Color);
        if (colorSet == null || colorSet.groupSizeSprites == null || colorSet.groupSizeSprites.Length < 4)
        {
            Debug.LogError($"Sprite configuration error for color {Color} on Block: {gameObject.name}");
            return;
        }

        int spriteIndex = 0;
        if (GroupSize >= 5 && GroupSize <= 6)
            spriteIndex = 1;
        else if (GroupSize >= 7 && GroupSize <= 8)
            spriteIndex = 2;
        else if (GroupSize >= 9)
            spriteIndex = 3;

        if (spriteIndex < colorSet.groupSizeSprites.Length)
        {
            spriteRenderer.sprite = colorSet.groupSizeSprites[spriteIndex];
            Debug.Log($"✅ Updated sprite for {Color} block with group size {GroupSize}");
        }
    }

    private void UpdateSortingOrder()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = GridPosition.y;
        }
    }

    public void SetGridPosition(Vector2Int newPosition)
    {
        GridPosition = newPosition;
        UpdateSortingOrder();
        UpdateGroupSize();
    }

    public void MoveToPosition(Vector3 targetPosition)
    {
        transform.position = targetPosition;
    }
}