using UnityEngine;

public class NatureVisuals : MonoBehaviour
{
    [Header("Görsel Referansları")]
    [Tooltip("Çizimin olduğu alt objeyi buraya sürükle")]
    public SpriteRenderer mainRenderer;

    [Tooltip("Sırasıyla: Temiz (Canlı), Kirli (Sararmış), Çok Kirli (Ölü)")]
    public Sprite[] stateSprites = new Sprite[3];

    [Header("Gölge Ayarları")]
    public bool enableShadow = true;

    private SpriteRenderer shadowRenderer;
    private static Sprite cachedShadowSprite;

    private void Awake()
    {
        if (mainRenderer == null)
        {
            mainRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        CreateShadowIfNeeded();
        SyncShadowOrder();
    }

    public void CreateShadowIfNeeded()
    {
        if (!enableShadow) return;
        if (mainRenderer == null)
        {
            SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i].gameObject.name != "DropShadow")
                {
                    mainRenderer = srs[i];
                    break;
                }
            }
        }
        if (mainRenderer == null) return;

        if (shadowRenderer == null)
        {
            Transform existing = transform.Find("DropShadow");
            if (existing != null) shadowRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (shadowRenderer == null)
        {
            GameObject shadowObj = new GameObject("DropShadow");
            shadowObj.transform.SetParent(transform, false);
            shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = GetOrCreateShadowSprite();
            if (mainRenderer.sharedMaterial != null)
            {
                shadowRenderer.sharedMaterial = mainRenderer.sharedMaterial;
            }
        }

        UpdateShadowTransformAndColor(0);
    }

    private void UpdateShadowTransformAndColor(int pollutionStage)
    {
        if (shadowRenderer == null || mainRenderer == null) return;

        string objName = gameObject.name.ToLowerInvariant();
        bool isGrass = objName.Contains("grass") || objName.Contains("ot");
        bool isBush = objName.Contains("bush") || objName.Contains("çalı") || objName.Contains("cali");

        Bounds b = mainRenderer.bounds;
        float w = b.size.x > 0.1f ? b.size.x : 1f;
        float h = b.size.y > 0.1f ? b.size.y : 1f;

        float shadowW, shadowH;
        Vector3 worldPos;
        float baseAlpha;

        if (isGrass)
        {
            // Çimen / Ot gölgesi: Tabanın altından ve sağına doğru uzanan net koyu gölge
            shadowW = w * 1.35f;
            shadowH = h * 0.65f;
            worldPos = new Vector3(b.center.x + w * 0.14f, b.min.y - h * 0.08f, 0f);
            baseAlpha = 0.88f;
        }
        else if (isBush)
        {
            // Çalı gölgesi: Çalının altından ve sağından net taşan koyu gölge
            shadowW = w * 1.32f;
            shadowH = h * 0.65f;
            worldPos = new Vector3(b.center.x + w * 0.15f, b.min.y - h * 0.08f, 0f);
            baseAlpha = 0.90f;
        }
        else
        {
            // Ağaç (Tree A & Tree B) gölgesi: Gövdenin altından genişçe yere yayılan ve sağ-alta taşan net siyah gölge
            shadowW = w * 1.35f;
            shadowH = h * 0.50f;
            worldPos = new Vector3(b.center.x + w * 0.18f, b.min.y - h * 0.08f, 0f);
            baseAlpha = 0.92f;
        }

        shadowRenderer.transform.position = worldPos;
        // cachedShadowSprite 2x2 birim olduğu için 0.5f ile çarpıyoruz
        shadowRenderer.transform.localScale = new Vector3(shadowW * 0.5f, shadowH * 0.5f, 1f);
        shadowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        // Zemin katmanının (-1 ve -2) üstünde, tüm doğa ve binaların (SortingLayer 0) altında çizilsin
        shadowRenderer.sortingOrder = -10000;
        shadowRenderer.color = new Color(0f, 0f, 0f, baseAlpha);
    }

    public void SyncShadowOrder()
    {
        if (shadowRenderer != null && mainRenderer != null)
        {
            shadowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = -10000;
        }
    }

    // GameManager hava durumuna göre bu fonksiyonu tetikleyecek
    public void UpdateVisual(int pollutionStage, float aqi = 18f)
    {
        CreateShadowIfNeeded();

        if (mainRenderer != null && stateSprites != null && stateSprites.Length > pollutionStage)
        {
            if (stateSprites[pollutionStage] != null)
            {
                mainRenderer.sprite = stateSprites[pollutionStage];
            }
        }

        // Otların (Grass) AQI arttıkça hafifçe sararıp solması
        if (mainRenderer != null)
        {
            string objName = gameObject.name.ToLowerInvariant();
            if (objName.Contains("grass") || objName.Contains("ot"))
            {
                float t = Mathf.InverseLerp(60f, 200f, aqi);
                mainRenderer.color = Color.Lerp(Color.white, new Color(0.85f, 0.74f, 0.46f, 1f), t);
            }
        }

        UpdateShadowTransformAndColor(pollutionStage);
    }

    public static Sprite GetOrCreateShadowSprite()
    {
        if (cachedShadowSprite != null) return cachedShadowSprite;

        int width = 64;
        int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        float maxRadius = width * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - center.x) / maxRadius;
                float dy = (y - center.y) / maxRadius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // İç %88'lik kısım TAM KOYU SİYAH (1.0), sadece en dış %12'lik sınırda hafif piksel yumuşaması
                float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.88f, 1.0f, dist));
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // PPU = 32f -> 64x64 doku 2.0 x 2.0 dünya birimi boyutundadır
        cachedShadowSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        return cachedShadowSprite;
    }
}
