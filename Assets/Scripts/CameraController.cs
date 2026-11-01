using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Referanslar")]
    public GridManager gridManager;

    [Header("Zoom Ayarları")]
    public float minZoom = 2f;
    public float maxZoom = 15f;
    public float zoomSpeed = 0.01f;

    [Header("Sınır Ayarları")]
    [Tooltip("Kameranın gridin dışına en fazla kaç birim çıkabileceği")]
    public float boundaryOffset = 20f;

    private Vector3 dragOrigin;

    void Start()
    {
        CenterCamera();
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (GameManager.Instance != null && GameManager.Instance.isGamePaused) return;

        HandlePan();
        HandleZoom();
        ClampCameraPosition();
    }

    private void CenterCamera()
    {
        if (gridManager == null) return;

        float centerX = (gridManager.gridWidth - 1) / 2f;
        float centerY = (gridManager.gridHeight - 1) / 2f;
        transform.position = new Vector3(centerX, centerY, -10f);
    }

    private void HandlePan()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            dragOrigin = Camera.main.ScreenToWorldPoint(mousePos);
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 difference = dragOrigin - Camera.main.ScreenToWorldPoint(mousePos);
            Camera.main.transform.position += difference;
        }
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll != 0.0f)
        {
            Camera.main.orthographicSize -= scroll * zoomSpeed;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);
        }
    }

    private void ClampCameraPosition()
    {
        if (gridManager == null) return;

        float minX = -boundaryOffset;
        float maxX = (gridManager.gridWidth - 1) + boundaryOffset;

        float minY = -boundaryOffset;
        float maxY = (gridManager.gridHeight - 1) + boundaryOffset;

        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);

        transform.position = clampedPos;
    }
}
