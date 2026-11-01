using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class NatureScript : MonoBehaviour
{
    // Farenin tam altında kalan ızgara (grid) karesini matematiksel olarak bulur
    private TileScript GetUnderlyingTile()
    {
        if (Camera.main == null || Mouse.current == null) return null;
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(screenPos);
        int x = Mathf.RoundToInt(mousePos.x);
        int y = Mathf.RoundToInt(mousePos.y);

        var grid = BuildingManager.Instance.gridManager;
        if (grid != null && x >= 0 && x < grid.gridWidth && y >= 0 && y < grid.gridHeight)
            return grid.gridMatrix[x, y];

        return null;
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGamePaused) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (BuildingManager.Instance.currentMode == ActionMode.Delete)
        {
            GameObject rootObj = transform.parent != null ? transform.parent.gameObject : gameObject;
            BuildingManager.Instance.DeleteNature(rootObj);
        }
        else
        {
            TileScript tile = GetUnderlyingTile();
            if (tile != null) BuildingManager.Instance.HandleTileClick(tile);
        }
    }

    private void OnMouseOver()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGamePaused) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        TileScript tile = GetUnderlyingTile();
        if (tile != null) BuildingManager.Instance.HandleTileHover(tile);
    }

    private void OnMouseExit()
    {
        if (BuildingManager.Instance != null) BuildingManager.Instance.ClearHover();
    }
}
