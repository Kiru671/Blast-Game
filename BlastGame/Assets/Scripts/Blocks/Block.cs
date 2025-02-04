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
        public Sprite[] groupSizeSprites; // Array of sprites for each group size (index 0 = size 1, index 1 = size 2, etc.)
    }

    [SerializeField] private ColorSprites[] colorSprites;
    private SpriteRenderer spriteRenderer;

    public BlockColor Color { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public int GroupSize;
    
    private BlockManager blockManager;

    private void Awake()
    {
        // Automatically get the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // Add SpriteRenderer if it doesn't exist
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            Debug.Log("SpriteRenderer added to Block");
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
        if (blockManager != null)
        {
            int previousGroupSize = GroupSize;
            GroupSize = blockManager.GetGroupSize(this);
            
            if (previousGroupSize != GroupSize)
            {
                UpdateSprite();
            }
        }
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer is missing on Block: " + gameObject.name);
            return;
        }

        Debug.Log($"Updating sprite for Block {gameObject.name} - Color: {Color}, GroupSize: {GroupSize}");

        ColorSprites colorSet = System.Array.Find(colorSprites, x => x.color == Color);
        if (colorSet == null)
        {
            Debug.LogError($"No sprite set found for color {Color} on Block: {gameObject.name}");
            return;
        }

        if (colorSet.groupSizeSprites == null || colorSet.groupSizeSprites.Length < 4)
        {
            Debug.LogError($"Not enough sprites assigned for color {Color} on Block: {gameObject.name}. Expected at least 4, found {colorSet.groupSizeSprites.Length}");
            return;
        }

        // Determine sprite index based on group size thresholds
        int spriteIndex = 0; // Default sprite
        if (GroupSize >= 5 && GroupSize <= 6)
            spriteIndex = 1;
        else if (GroupSize >= 7 && GroupSize <= 8)
            spriteIndex = 2;
        else if (GroupSize >= 9)
            spriteIndex = 3;

        if (spriteIndex >= colorSet.groupSizeSprites.Length)
        {
            Debug.LogError($"Sprite index {spriteIndex} out of range for {Color} on Block: {gameObject.name}");
            return;
        }

        spriteRenderer.sprite = colorSet.groupSizeSprites[spriteIndex];

        Debug.Log($"✅ Assigned sprite index {spriteIndex} for color {Color} with group size {GroupSize} on {gameObject.name}");
    }




    private void UpdateSortingOrder()
    {
        if (spriteRenderer != null)
        {
            // Higher rows (larger Y values) should have higher sorting order to appear in front
            spriteRenderer.sortingOrder = GridPosition.y;
        }
    }

    public void SetGridPosition(Vector2Int newPosition)
    {
        GridPosition = newPosition;
        UpdateSortingOrder();
        UpdateGroupSize();
    }

    private void OnValidate()
    {
        if (colorSprites == null) return;
        
        foreach (var colorSet in colorSprites)
        {
            if (colorSet.groupSizeSprites == null || colorSet.groupSizeSprites.Length == 0)
            {
                Debug.LogWarning($"Missing sprites for {colorSet.color} color in {gameObject.name}");
            }
        }
    }

    // Debug method to verify sprite assignment
    public void VerifySprites()
    {
        Debug.Log($"Block {gameObject.name} - Color: {Color}, GroupSize: {GroupSize}");
        Debug.Log($"SpriteRenderer exists: {spriteRenderer != null}");
        Debug.Log($"ColorSprites array length: {colorSprites?.Length ?? 0}");
        
        if (colorSprites != null)
        {
            foreach (var colorSet in colorSprites)
            {
                Debug.Log($"Color {colorSet.color} has {colorSet.groupSizeSprites?.Length ?? 0} sprites");
            }
        }
    }
}