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
        if (!enableShadow || shadowRenderer != null) return;
        if (mainRenderer == null) mainRenderer = GetComponentInChildren<SpriteRenderer>();
        if (mainRenderer == null) return;

        // Küçük otlara (Grass) ağaç gölgesi ekleme, sadece Ağaç ve Çalılara ekle
        string objName = gameObject.name.ToLowerInvariant();
        if (objName.Contains("grass") || objName.Contains("ot")) return;

        bool isBush = objName.Contains("bush") || objName.Contains("çalı") || objName.Contains("cali");
        bool isPine = objName.Contains("tree_b") || objName.Contains("tree b");

        GameObject shadowObj = new GameObject("DropShadow");
        shadowObj.transform.SetParent(transform, false);

        // Ağaçların kökleri localY = -0.34 civarında bittiği ve taç genişliği ~3.9 birim olduğu için
        // gölgeyi tam köklerin altına ve taç genişliğine uygun boyutta yerleştiriyoruz
        if (isBush)
        {
            shadowObj.transform.localPosition = new Vector3(0.10f, -0.16f, 0f);
            shadowObj.transform.localScale = new Vector3(2.35f, 0.85f, 1f);
        }
        else if (isPine)
        {
            shadowObj.transform.localPosition = new Vector3(0.18f, -0.14f, 0f);
            shadowObj.transform.localScale = new Vector3(2.95f, 1.05f, 1f);
        }
        else
        {
            // Geniş yapraklı ağaç (Tree_A)
            shadowObj.transform.localPosition = new Vector3(0.20f, -0.24f, 0f);
            shadowObj.transform.localScale = new Vector3(3.45f, 1.22f, 1f);
        }

        shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = GetOrCreateShadowSprite();
        if (mainRenderer.sharedMaterial != null)
        {
            shadowRenderer.sharedMaterial = mainRenderer.sharedMaterial;
        }
        shadowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;
        shadowRenderer.color = new Color(0f, 0f, 0f, isBush ? 0.32f : 0.38f);
    }

    public void SyncShadowOrder()
    {
        if (shadowRenderer != null && mainRenderer != null)
        {
            shadowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;
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

        if (shadowRenderer != null && mainRenderer != null)
        {
            shadowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;

            // Yapraklar döküldükçe (Evre 2) gölge biraz daha hafiflesin
            bool isBush = gameObject.name.ToLowerInvariant().Contains("bush");
            float baseAlpha = isBush ? 0.32f : 0.38f;
            if (pollutionStage == 1) baseAlpha *= 0.90f;
            else if (pollutionStage >= 2) baseAlpha *= 0.65f;

            shadowRenderer.color = new Color(0f, 0f, 0f, baseAlpha);
        }
    }

    private static Sprite GetOrCreateShadowSprite()
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
                float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.22f, 1.0f, dist));
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedShadowSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
        return cachedShadowSprite;
    }
}
