using UnityEngine;
using UnityEngine.EventSystems;

public class TileScript : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public bool isTileFull = false;

    public BaseBuilding occupiedBy;

    // Ağaç, ot veya çalılıkların referansını burada tutuyoruz
    public GameObject environmentObj;

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (BuildingManager.Instance != null) BuildingManager.Instance.HandleTileClick(this);
    }

    private void OnMouseEnter()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (BuildingManager.Instance != null) BuildingManager.Instance.ClearHover();
            return;
        }
        if (BuildingManager.Instance != null) BuildingManager.Instance.HandleTileHover(this);
    }

    private void OnMouseExit()
    {
        if (BuildingManager.Instance != null) BuildingManager.Instance.ClearHover();
    }
}