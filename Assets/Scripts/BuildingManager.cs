using UnityEngine;
using UnityEngine.UI;

public enum ActionMode { Cursor, Build, Delete, Upgrade }

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    [Header("Referanslar")]
    public GridManager gridManager;

    [Header("UI Buton Görselleri (Image)")]
    public Image cursorButtonImage;
    public Image deleteButtonImage;
    public Image upgradeButtonImage;
    public Image factoryButtonImage;
    public Image houseButtonImage;

    [Header("Durum Makinesi")]
    public ActionMode currentMode = ActionMode.Cursor;
    public GameObject selectedPrefab;
    public BaseBuilding selectedBuildingData;

    [Header("Silüet (Ghost) Sistemi")]
    public GameObject ghostRoot;
    public SpriteRenderer ghostRenderer;
    public Color validColor = new Color(0f, 1f, 0f, 0.6f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.6f);

    private BaseBuilding hoveredBuildingToHighlight;
    private GameObject hoveredNatureToHighlight;
    private bool initialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        SetModeCursor();
        initialized = true;
    }

    public void SetModeCursor()
    {
        if (initialized) AudioManager.Instance?.PlayClick();
        currentMode = ActionMode.Cursor;
        selectedPrefab = null;
        selectedBuildingData = null;
        UpdateUIButtonVisuals(cursorButtonImage);
        HideGhost();
    }

    public void SetModeDelete()
    {
        AudioManager.Instance?.PlayClick();
        currentMode = ActionMode.Delete;
        selectedPrefab = null;
        selectedBuildingData = null;
        UpdateUIButtonVisuals(deleteButtonImage);
        HideGhost();
    }

    public void SetModeUpgrade()
    {
        AudioManager.Instance?.PlayClick();
        currentMode = ActionMode.Upgrade;
        selectedPrefab = null;
        selectedBuildingData = null;
        UpdateUIButtonVisuals(upgradeButtonImage);
        HideGhost();
    }

    public void SelectBuildingToPlace(GameObject prefab)
    {
        AudioManager.Instance?.PlayClick();
        currentMode = ActionMode.Build;
        selectedPrefab = prefab;
        selectedBuildingData = prefab.GetComponent<BaseBuilding>();

        if (prefab.name.Contains("Factory")) UpdateUIButtonVisuals(factoryButtonImage);
        else UpdateUIButtonVisuals(houseButtonImage);

        SpriteRenderer prefabRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
        if (prefabRenderer != null && ghostRenderer != null)
        {
            ghostRenderer.sprite = prefabRenderer.sprite;
            ghostRenderer.transform.localPosition = prefabRenderer.transform.localPosition;
        }
    }

    private void UpdateUIButtonVisuals(Image selectedImage)
    {
        if (cursorButtonImage) SetImageAlpha(cursorButtonImage, 0.7f);
        if (deleteButtonImage) SetImageAlpha(deleteButtonImage, 0.7f);
        if (upgradeButtonImage) SetImageAlpha(upgradeButtonImage, 0.7f);
        if (factoryButtonImage) SetImageAlpha(factoryButtonImage, 0.7f);
        if (houseButtonImage) SetImageAlpha(houseButtonImage, 0.7f);

        if (selectedImage) SetImageAlpha(selectedImage, 1f);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    public void HandleTileHover(TileScript hoveredTile)
    {
        ClearHover();
        if (GameManager.Instance != null && GameManager.Instance.isGamePaused) return;

        if (currentMode == ActionMode.Build && selectedBuildingData != null)
        {
            ShowGhost(hoveredTile);
        }
        else if (currentMode == ActionMode.Delete)
        {
            if (hoveredTile.isTileFull && hoveredTile.occupiedBy != null)
            {
                hoveredBuildingToHighlight = hoveredTile.occupiedBy;
                hoveredBuildingToHighlight.SetHoverColor(new Color(1f, 0.4f, 0.4f, 1f));
            }
            else if (hoveredTile.environmentObj != null)
            {
                hoveredNatureToHighlight = hoveredTile.environmentObj;
                SpriteRenderer[] srs = hoveredNatureToHighlight.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in srs)
                {
                    sr.color = new Color(1f, 0.4f, 0.4f, 1f);
                }
            }
        }
        else if (currentMode == ActionMode.Upgrade)
        {
            if (hoveredTile.isTileFull && hoveredTile.occupiedBy != null && hoveredTile.occupiedBy.CanUpgrade())
            {
                hoveredBuildingToHighlight = hoveredTile.occupiedBy;
                hoveredBuildingToHighlight.SetHoverColor(new Color(1f, 0.9f, 0.4f, 1f));
            }
        }
    }

    public void ClearHover()
    {
        if (hoveredBuildingToHighlight != null)
        {
            hoveredBuildingToHighlight.ResetColor();
            hoveredBuildingToHighlight = null;
        }

        if (hoveredNatureToHighlight != null)
        {
            SpriteRenderer[] srs = hoveredNatureToHighlight.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs)
            {
                sr.color = Color.white;
            }
            hoveredNatureToHighlight = null;
        }

        HideGhost();
    }

    private void ShowGhost(TileScript tile)
    {
        if (ghostRoot == null || ghostRenderer == null) return;

        ghostRoot.SetActive(true);

        int w = selectedBuildingData.buildingWidth;
        int h = selectedBuildingData.buildingHeight;

        bool canPlace = CanPlaceBuilding(tile.gridX, tile.gridY, w, h);
        bool hasMoney = GameManager.Instance.money >= selectedBuildingData.buildCost;

        ghostRenderer.color = (canPlace && hasMoney) ? validColor : invalidColor;

        float centerX = tile.gridX + (w - 1) / 2f;
        float centerY = tile.gridY + (h - 1) / 2f;
        ghostRoot.transform.position = new Vector3(centerX, centerY, -1f);
    }

    private void HideGhost()
    {
        if (ghostRoot != null) ghostRoot.SetActive(false);
    }

    public void HandleTileClick(TileScript clickedTile)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGamePaused) return;

        if (currentMode == ActionMode.Build && selectedPrefab != null)
        {
            PlaceBuilding(clickedTile.gridX, clickedTile.gridY, selectedBuildingData.buildingWidth, selectedBuildingData.buildingHeight, selectedPrefab);
        }
        else if (currentMode == ActionMode.Delete)
        {
            DeleteAt(clickedTile);
        }
        else if (currentMode == ActionMode.Upgrade)
        {
            UpgradeBuildingAt(clickedTile);
        }
    }

    public bool CanPlaceBuilding(int startX, int startY, int bWidth, int bHeight)
    {
        if (startX + bWidth > gridManager.gridWidth || startY + bHeight > gridManager.gridHeight) return false;

        for (int x = startX; x < startX + bWidth; x++)
        {
            for (int y = startY; y < startY + bHeight; y++)
            {
                if (gridManager.gridMatrix[x, y].isTileFull) return false;
            }
        }
        return true;
    }

    public void PlaceBuilding(int startX, int startY, int bWidth, int bHeight, GameObject buildingPrefab, bool isFree = false)
    {
        if (CanPlaceBuilding(startX, startY, bWidth, bHeight))
        {
            if (!isFree)
            {
                if (GameManager.Instance.money < selectedBuildingData.buildCost)
                {
                    AdvisorManager.Instance?.ShowCustomDialogue(
                        "Not enough money in the treasury, Mr. Mayor! Wait for our factories to generate more income.",
                        "Kasada yeterli paramız yok Başkan! Fabrikaların biraz daha gelir üretmesini bekleyelim.",
                        true
                    );
                    return;
                }
                GameManager.Instance.money -= selectedBuildingData.buildCost;
            }

            GameObject newBuilding = Instantiate(buildingPrefab);
            BaseBuilding buildingData = newBuilding.GetComponent<BaseBuilding>();

            buildingData.startX = startX;
            buildingData.startY = startY;

            float centerX = startX + (bWidth - 1) / 2f;
            float centerY = startY + (bHeight - 1) / 2f;
            newBuilding.transform.position = new Vector3(centerX, centerY, 0);

            buildingData.UpdateDepth();

            for (int x = startX; x < startX + bWidth; x++)
            {
                for (int y = startY; y < startY + bHeight; y++)
                {
                    TileScript currentTile = gridManager.gridMatrix[x, y];

                    if (currentTile.environmentObj != null)
                    {
                        Destroy(currentTile.environmentObj);
                        currentTile.environmentObj = null;
                    }

                    currentTile.isTileFull = true;
                    currentTile.occupiedBy = buildingData;
                }
            }

            if (!isFree) AudioManager.Instance?.PlayBuild();

            GameManager.Instance.RegisterBuilding(buildingData, true);
            HandleTileHover(gridManager.gridMatrix[startX, startY]);
        }
    }

    private void UpgradeBuildingAt(TileScript tile)
    {
        if (tile.isTileFull && tile.occupiedBy != null)
        {
            BaseBuilding target = tile.occupiedBy;

            if (target.CanUpgrade())
            {
                float cost = target.upgradeCosts[target.currentLevel + 1];
                if (GameManager.Instance.money >= cost)
                {
                    GameManager.Instance.money -= cost;
                    target.currentLevel++;

                    target.UpdateVisuals();
                    AudioManager.Instance?.PlayUpgrade();

                    GameManager.Instance.ForceUpdateUI();
                }
                else
                {
                    AdvisorManager.Instance?.ShowCustomDialogue(
                        $"Upgrading this building requires ${Mathf.FloorToInt(cost)}, Mr. Mayor!",
                        $"Bu binayı yükseltmek için {Mathf.FloorToInt(cost)}$ gerekiyor Başkan!",
                        true
                    );
                }
            }
        }
    }

    public void DeleteNature(GameObject targetEnv)
    {
        if (targetEnv == null) return;

        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int y = 0; y < gridManager.gridHeight; y++)
            {
                if (gridManager.gridMatrix[x, y].environmentObj == targetEnv)
                {
                    gridManager.gridMatrix[x, y].environmentObj = null;
                }
            }
        }
        Destroy(targetEnv);
        AudioManager.Instance?.PlayDelete();
        hoveredBuildingToHighlight = null;
        hoveredNatureToHighlight = null;
    }

    private void DeleteAt(TileScript tile)
    {
        if (tile.isTileFull && tile.occupiedBy != null)
        {
            BaseBuilding target = tile.occupiedBy;

            for (int x = target.startX; x < target.startX + target.buildingWidth; x++)
            {
                for (int y = target.startY; y < target.startY + target.buildingHeight; y++)
                {
                    gridManager.gridMatrix[x, y].isTileFull = false;
                    gridManager.gridMatrix[x, y].occupiedBy = null;
                }
            }

            GameManager.Instance.UnregisterBuilding(target);
            Destroy(target.gameObject);
            AudioManager.Instance?.PlayDelete();
            hoveredBuildingToHighlight = null;
        }
        else if (tile.environmentObj != null)
        {
            DeleteNature(tile.environmentObj);
        }
    }
}
