using UnityEngine;

public class SetCamera : MonoBehaviour
{
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private float cameraZOffset = -10f; // Adjust for 2D view

    private void Start()
    {
        if (blockManager == null)
        {
            Debug.LogError("CameraController: BlockManager reference is missing.");
            return;
        }
        
        CenterCameraOnGrid();
    }

    private void CenterCameraOnGrid()
    {
        int rowWidth = blockManager.RowWidth;
        int rowHeight = blockManager.RowHeight;
        float spacing = blockManager.BlockSpacing;

        Vector3 centerPosition = new Vector3(
            (rowWidth - 1) * spacing / 2f,
            (rowHeight - 1) * spacing / 2f,
            cameraZOffset
        );

        transform.position = centerPosition;
    }
}