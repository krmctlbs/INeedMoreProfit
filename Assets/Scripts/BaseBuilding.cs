using UnityEngine;

public class BaseBuilding : MonoBehaviour
{
    public int buildingWidth;
    public int buildingHeight;

    [HideInInspector] public int startX;
    [HideInInspector] public int startY;

    public int currentLevel = 0;
    public int maxLevel = 2;

    public float buildCost;
    public float[] upgradeCosts = new float[3];

    [Header("Görsel Yükseltme (Upgrade)")]
    [Tooltip("Çizimin bulunduğu alt objeyi buraya sürükle")]
    public SpriteRenderer mainRenderer;
    [Tooltip("Sırasıyla Seviye 1, Seviye 2 ve Seviye 3 çizimlerini koy")]
    public Sprite[] levelSprites = new Sprite[3];

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    protected virtual void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }

        // Unutkanlığına karşı acemi koruması: Eğer mainRenderer'ı bağlamazsan ilk bulduğunu alır
        if (mainRenderer == null && spriteRenderers.Length > 0)
        {
            mainRenderer = spriteRenderers[0];
        }
    }

    public void UpdateDepth()
    {
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sortingOrder = Mathf.RoundToInt(-transform.position.y * 10f);
        }
    }

    public virtual void OnTurnUpdate() { }

    public bool CanUpgrade() => currentLevel < maxLevel;

    // YENİ: Seviye atladığı an görseli değiştiren şık fonksiyonumuz
    public void UpdateVisuals()
    {
        if (mainRenderer != null && levelSprites.Length > currentLevel && levelSprites[currentLevel] != null)
        {
            mainRenderer.sprite = levelSprites[currentLevel];
        }
    }

    public void SetHoverColor(Color color)
    {
        foreach (var sr in spriteRenderers) sr.color = color;
    }

    public void ResetColor()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = originalColors[i];
        }
    }
}