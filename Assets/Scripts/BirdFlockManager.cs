using System.Collections.Generic;
using UnityEngine;

public class BirdFlockManager : MonoBehaviour
{
    public static BirdFlockManager Instance;

    [Header("Kuş Sürüsü Ayarları")]
    [Tooltip("İsteğe bağlı özel kuş animasyon kareleri (Boşsa piksel-art kuşlar otomatik üretilir)")]
    public Sprite[] customBirdSprites;
    [Tooltip("Kuşların tamamen kesileceği AQI eşiği")]
    public float maxAqiForBirds = 130f;

    private class FlyingBird
    {
        public Transform root;
        public SpriteRenderer bodyRenderer;
        public SpriteRenderer shadowRenderer;
        public Sprite[] frames;
        public Vector2 velocity;
        public float baseSpeed;
        public float flapTimer;
        public float flapSpeed;
        public float waveOffset;
        public int frameStep; // 0: Up, 1: Mid, 2: Down, 3: Mid
        public float glideTimer;
    }

    private List<FlyingBird> activeBirds = new List<FlyingBird>();
    private Sprite[] blueBirdFrames;
    private Sprite[] brownBirdFrames;
    private Sprite birdShadowSprite;
    private Material sharedLitMaterial;

    private float spawnTimer = 1.5f;
    private float mapWidth = 50f;
    private float mapHeight = 35f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        GeneratePixelArtBirdSprites();
    }

    private void Start()
    {
        if (BuildingManager.Instance != null && BuildingManager.Instance.gridManager != null)
        {
            mapWidth = BuildingManager.Instance.gridManager.gridWidth;
            mapHeight = BuildingManager.Instance.gridManager.gridHeight;
        }

        // Sahnedeki ağaçların kullandığı 2D URP materyalini al
        if (GameManager.Instance != null && GameManager.Instance.activeNatures.Count > 0)
        {
            var nv = GameManager.Instance.activeNatures[0];
            if (nv != null && nv.mainRenderer != null)
                sharedLitMaterial = nv.mainRenderer.sharedMaterial;
        }

        // Oyun başında gökyüzünde halihazırda uçan 2 küçük sürü oluştur
        if (GameManager.Instance == null || GameManager.Instance.aqi < 60f)
        {
            SpawnFlock(3, true);
            SpawnFlock(2, true);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.isGamePaused) return;

        float aqi = GameManager.Instance.aqi;

        // Kirlilik arttıkça kuşların çıkma sıklığı ve sayısı azalır, AQI >= 130 olduğunda tamamen biter
        if (aqi < maxAqiForBirds)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                int maxAllowedBirds;
                int flockSize;
                if (aqi < 50f)
                {
                    // Tertemiz hava: Sık sık 2-4'lü kuş sürüleri uçar
                    maxAllowedBirds = 10;
                    flockSize = Random.Range(2, 5);
                    spawnTimer = Random.Range(3.8f, 6.2f);
                }
                else if (aqi < 90f)
                {
                    // Hava hafif kirlenirken: Daha seyrek 1-2 kuş
                    maxAllowedBirds = 5;
                    flockSize = Random.Range(1, 3);
                    spawnTimer = Random.Range(6.5f, 10.5f);
                }
                else
                {
                    // Ağaçlar sararırken (90-130 AQI): Nadiren tek başına kaçan son kuşlar
                    maxAllowedBirds = 2;
                    flockSize = 1;
                    spawnTimer = Random.Range(11f, 16f);
                }

                if (activeBirds.Count < maxAllowedBirds)
                {
                    SpawnFlock(flockSize, false);
                }
            }
        }

        UpdateActiveBirds(aqi);
    }

    private void SpawnFlock(int count, bool startInsideMap)
    {
        bool flyRight = Random.value > 0.45f;
        float dirX = flyRight ? 1f : -1f;
        float dirY = Random.Range(-0.28f, 0.28f);
        Vector2 moveDir = new Vector2(dirX, dirY).normalized;

        float startX;
        float startY = Random.Range(mapHeight * 0.18f, mapHeight * 0.82f);

        if (startInsideMap)
        {
            startX = Random.Range(mapWidth * 0.20f, mapWidth * 0.80f);
        }
        else
        {
            startX = flyRight ? -4f : (mapWidth + 4f);
        }

        float flockBaseSpeed = Random.Range(2.8f, 3.6f);
        bool useBluePalette = Random.value > 0.45f;

        for (int i = 0; i < count; i++)
        {
            // V-formasyonu veya doğal sürü dağılımı
            Vector2 offset = new Vector2(
                -dirX * (i * Random.Range(0.75f, 1.25f)),
                (i % 2 == 0 ? 1f : -1f) * ((i + 1) / 2) * Random.Range(0.65f, 1.05f)
            );

            CreateSingleBird(new Vector2(startX, startY) + offset, moveDir, flockBaseSpeed * Random.Range(0.94f, 1.06f), flyRight, useBluePalette);
        }
    }

    private void ResolveLitMaterialIfNeeded()
    {
        if (sharedLitMaterial != null) return;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.activeNatures.Count > 0 && GameManager.Instance.activeNatures[0] != null && GameManager.Instance.activeNatures[0].mainRenderer != null)
            {
                sharedLitMaterial = GameManager.Instance.activeNatures[0].mainRenderer.sharedMaterial;
                return;
            }
            if (GameManager.Instance.treeAPrefab != null)
            {
                var sr = GameManager.Instance.treeAPrefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sharedLitMaterial = sr.sharedMaterial;
            }
        }
    }

    private void CreateSingleBird(Vector2 pos, Vector2 moveDir, float speed, bool flyRight, bool useBluePalette)
    {
        ResolveLitMaterialIfNeeded();
        GameObject birdObj = new GameObject("SkyBird");
        birdObj.transform.SetParent(transform, false);
        birdObj.transform.position = new Vector3(pos.x, pos.y, 0f);

        float scale = Random.Range(0.88f, 1.12f);
        birdObj.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer bodySR = birdObj.AddComponent<SpriteRenderer>();
        Sprite[] chosenFrames = (customBirdSprites != null && customBirdSprites.Length > 0)
            ? customBirdSprites
            : (useBluePalette ? blueBirdFrames : brownBirdFrames);

        bodySR.sprite = chosenFrames[0];
        bodySR.flipX = !flyRight; // Sağ-sol yönüne göre çevir
        bodySR.sortingOrder = 5000; // Tüm ağaçların ve binaların üstünde uçsun
        if (sharedLitMaterial != null) bodySR.sharedMaterial = sharedLitMaterial;

        // Kuşun yerdeki küçük gölgesi
        GameObject shadowObj = new GameObject("BirdShadow");
        shadowObj.transform.SetParent(birdObj.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.22f, -1.65f, 0f);
        shadowObj.transform.localScale = new Vector3(0.65f, 0.32f, 1f);

        SpriteRenderer shadowSR = shadowObj.AddComponent<SpriteRenderer>();
        shadowSR.sprite = birdShadowSprite;
        shadowSR.sortingOrder = 4990;
        shadowSR.color = new Color(0f, 0f, 0f, 0.20f);
        if (sharedLitMaterial != null) shadowSR.sharedMaterial = sharedLitMaterial;

        FlyingBird fb = new FlyingBird
        {
            root = birdObj.transform,
            bodyRenderer = bodySR,
            shadowRenderer = shadowSR,
            frames = chosenFrames,
            velocity = moveDir,
            baseSpeed = speed,
            flapTimer = Random.Range(0f, 0.2f),
            flapSpeed = Random.Range(0.09f, 0.13f),
            waveOffset = Random.Range(0f, 10f),
            frameStep = Random.Range(0, 4),
            glideTimer = Random.Range(0.5f, 2.0f)
        };

        activeBirds.Add(fb);
    }

    private void UpdateActiveBirds(float aqi)
    {
        // Hava kirlendikçe gökyüzündeki son kuşlar panikle daha hızlı uzaklaşır
        float panicSpeedMultiplier = aqi > 100f ? Mathf.Lerp(1.0f, 1.65f, Mathf.InverseLerp(100f, 180f, aqi)) : 1.0f;

        for (int i = activeBirds.Count - 1; i >= 0; i--)
        {
            FlyingBird b = activeBirds[i];
            if (b.root == null)
            {
                activeBirds.RemoveAt(i);
                continue;
            }

            float currentSpeed = b.baseSpeed * panicSpeedMultiplier;
            Vector3 pos = b.root.position;
            pos.x += b.velocity.x * currentSpeed * Time.deltaTime;
            pos.y += b.velocity.y * currentSpeed * Time.deltaTime;

            // Hafif süzülme dalgası
            pos.y += Mathf.Sin(Time.time * 3.2f + b.waveOffset) * 0.22f * Time.deltaTime;
            b.root.position = pos;

            // Kanat çırpma ve kısa süzülme (glide) animasyonu
            if (b.frames != null && b.frames.Length >= 3)
            {
                b.glideTimer -= Time.deltaTime;
                if (b.glideTimer <= -0.35f)
                {
                    b.glideTimer = Random.Range(1.2f, 2.6f);
                }

                if (b.glideTimer < 0f && aqi < 110f)
                {
                    // Kısa süzülme anı (orta kare)
                    b.bodyRenderer.sprite = b.frames[1];
                }
                else
                {
                    b.flapTimer += Time.deltaTime;
                    if (b.flapTimer >= b.flapSpeed)
                    {
                        b.flapTimer = 0f;
                        b.frameStep = (b.frameStep + 1) % 4;
                        int spriteIndex = (b.frameStep == 3) ? 1 : b.frameStep;
                        b.bodyRenderer.sprite = b.frames[Mathf.Clamp(spriteIndex, 0, b.frames.Length - 1)];
                    }
                }
            }

            // Harita sınırlarından çıktıysa objeyi temizle
            if (pos.x < -8f || pos.x > mapWidth + 8f || pos.y < -8f || pos.y > mapHeight + 8f)
            {
                Destroy(b.root.gameObject);
                activeBirds.RemoveAt(i);
            }
        }
    }

    private void GeneratePixelArtBirdSprites()
    {
        // Mavi-Beyaz Ötücü Kuş & Kahverengi-Krem Serçe (Sağa doğru bakan 20x16 piksel-art kareler)
        Color outline = new Color(0.12f, 0.11f, 0.14f, 1f);
        Color beak = new Color(0.95f, 0.62f, 0.18f, 1f);
        Color belly = new Color(0.95f, 0.94f, 0.90f, 1f);

        Color blueMain = new Color(0.26f, 0.56f, 0.85f, 1f);
        Color blueWing = new Color(0.18f, 0.42f, 0.70f, 1f);

        Color brownMain = new Color(0.62f, 0.42f, 0.26f, 1f);
        Color brownWing = new Color(0.45f, 0.28f, 0.16f, 1f);

        // 0: Boş, 1: Outline, 2: Gövde, 3: Kanat, 4: Karın/Beyaz, 5: Gaga
        string[] frameUp = new string[]
        {
            "......1111..........",
            ".....133331.........",
            "....13333331........",
            "....13333331.1111...",
            ".....1333331122221..",
            ".111..1333222212221.",
            "12221112222222222551",
            ".122222222444442111.",
            "..11114444444411....",
            "......11111111......"
        };

        string[] frameMid = new string[]
        {
            "....................",
            ".............1111...",
            "....111111..122221..",
            "...1333333112221221.",
            ".1113333333222222551",
            "1222223333444442111.",
            ".111114444444411....",
            "......11111111......",
            "....................",
            "...................."
        };

        string[] frameDown = new string[]
        {
            ".............1111...",
            "............122221..",
            ".111...111112221221.",
            "12221112222222222551",
            ".122222233334442111.",
            "..11111333333411....",
            ".....133333311......",
            "....13333311........",
            "....111111..........",
            "...................."
        };

        blueBirdFrames = new Sprite[]
        {
            BuildPixelSprite(frameUp, outline, blueMain, blueWing, belly, beak),
            BuildPixelSprite(frameMid, outline, blueMain, blueWing, belly, beak),
            BuildPixelSprite(frameDown, outline, blueMain, blueWing, belly, beak)
        };

        brownBirdFrames = new Sprite[]
        {
            BuildPixelSprite(frameUp, outline, brownMain, brownWing, belly, beak),
            BuildPixelSprite(frameMid, outline, brownMain, brownWing, belly, beak),
            BuildPixelSprite(frameDown, outline, brownMain, brownWing, belly, beak)
        };

        // Kuş gölgesi için yumuşak elips sprite
        int sw = 32, sh = 32;
        Texture2D sTex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
        sTex.filterMode = FilterMode.Bilinear;
        Color[] sPixels = new Color[sw * sh];
        Vector2 center = new Vector2(15.5f, 15.5f);
        for (int y = 0; y < sh; y++)
        {
            for (int x = 0; x < sw; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / 15.5f;
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.1f, 1f, d));
                sPixels[y * sw + x] = new Color(1f, 1f, 1f, a);
            }
        }
        sTex.SetPixels(sPixels);
        sTex.Apply();
        birdShadowSprite = Sprite.Create(sTex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 32f);
    }

    private Sprite BuildPixelSprite(string[] rows, Color c1, Color c2, Color c3, Color c4, Color c5)
    {
        int h = rows.Length;
        int w = rows[0].Length;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[w * h];
        for (int r = 0; r < h; r++)
        {
            string row = rows[h - 1 - r]; // Üstten alta doğru oku
            for (int c = 0; c < w; c++)
            {
                char ch = row[c];
                Color col = Color.clear;
                if (ch == '1') col = c1;
                else if (ch == '2') col = c2;
                else if (ch == '3') col = c3;
                else if (ch == '4') col = c4;
                else if (ch == '5') col = c5;
                pixels[r * w + c] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 28f);
    }
}
