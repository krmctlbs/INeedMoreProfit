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
    private SpriteRenderer shadowRenderer;

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

        CreateShadowIfNeeded();
    }

    private void Start()
    {
        SyncBuildingShadow();
    }

    public void CreateShadowIfNeeded()
    {
        if (shadowRenderer != null || mainRenderer == null) return;

        Transform existing = transform.Find("DropShadow");
        if (existing != null)
        {
            shadowRenderer = existing.GetComponent<SpriteRenderer>();
            SyncBuildingShadow();
            return;
        }

        GameObject shadowObj = new GameObject("DropShadow");
        shadowObj.transform.SetParent(transform, false);

        shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = NatureVisuals.GetOrCreateShadowSprite();
        if (mainRenderer.sharedMaterial != null)
        {
            shadowRenderer.sharedMaterial = mainRenderer.sharedMaterial;
        }

        // Ev ve fabrikaların altında net ve belirgin koyu siyah gölge
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.92f);
        SyncBuildingShadow();
    }

    public void SyncBuildingShadow()
    {
        if (shadowRenderer == null || mainRenderer == null) return;

        shadowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        // Zemin üstünde, tüm bina ve doğa objelerinin altında kalması için derinlik
        shadowRenderer.sortingOrder = -10000;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.92f);

        if (mainRenderer.sprite != null)
        {
            Bounds b = mainRenderer.bounds;
            float shadowW = Mathf.Max(1.4f, b.size.x * 1.30f);
            float shadowH = Mathf.Max(0.75f, b.size.y * 0.58f);

            // Binanın taban hizasından belirgin şekilde aşağıya ve sağa taşan koyu gölge
            shadowRenderer.transform.position = new Vector3(
                b.center.x + b.size.x * 0.16f,
                b.min.y - b.size.y * 0.10f,
                0f
            );
            // Gölge sprite'ı 2x2 dünya birimi olduğu için 0.5 ile çarpıyoruz
            shadowRenderer.transform.localScale = new Vector3(shadowW * 0.5f, shadowH * 0.5f, 1f);
        }
    }

    public void UpdateDepth()
    {
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && spriteRenderers[i] != shadowRenderer)
            {
                spriteRenderers[i].sortingOrder = Mathf.RoundToInt(-transform.position.y * 10f);
            }
        }
        CreateShadowIfNeeded();
        SyncBuildingShadow();
    }

    public virtual void OnTurnUpdate() { }

    public bool CanUpgrade() => currentLevel < maxLevel;

    // YENİ: Seviye atladığı an görseli değiştiren şık fonksiyonumuz
    public void UpdateVisuals()
    {
        if (mainRenderer != null && levelSprites.Length > currentLevel && levelSprites[currentLevel] != null)
        {
            mainRenderer.sprite = levelSprites[currentLevel];
            SyncBuildingShadow();
        }
    }

    public void SetHoverColor(Color color)
    {
        foreach (var sr in spriteRenderers)
        {
            if (sr != null && sr != shadowRenderer) sr.color = color;
        }
    }

    public void ResetColor()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && spriteRenderers[i] != shadowRenderer)
                spriteRenderers[i].color = originalColors[i];
        }
    }
}
