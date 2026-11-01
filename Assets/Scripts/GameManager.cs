using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Yazı Tipi / Font Ayarları (TMP Font veya .ttf Sürükle)")]
    [Tooltip("Başlıklar, Butonlar ve Manşetler için Font (NormalFont / NinjaAdventure gibi Büyük Harf fontları için idealdir)")]
    public TMP_FontAsset customMainFont;
    [Tooltip("Uzun Metinler, Konuşma Balonu ve Gazete Yazıları için Okunaklı Font (Boşsa temiz varsayılan font kullanılır)")]
    public TMP_FontAsset customBodyFont;
    [Tooltip("Başlıklar için doğrudan .ttf / .otf font dosyası")]
    public Font rawMainTtfFont;
    [Tooltip("Uzun paragraf/gövde metinleri için doğrudan .ttf / .otf font dosyası (Örn: PixelifySans, VT323)")]
    public Font rawBodyTtfFont;

    [Header("Oyun Değerleri")]
    public float money = 500f;
    public float population = 0f;
    public float maxPopulation = 0f;
    public float aqi = 18f;
    public float happiness = 65f;

    public float totalWorkersNeeded = 0f;
    public float totalProductionRate = 0f;

    [Header("İlerleme & Skor Takibi (Arka Planda Tutulur)")]
    public float elapsedPlayTime = 0f;
    public float peakMoney = 500f;
    private bool runScoreSaved = false;

    [Header("Sistem Ayarları")]
    public float turnDuration = 2f;
    public float startingPopulation = 20f;
    public bool isGamePaused = false;
    public List<BaseBuilding> activeBuildings = new List<BaseBuilding>();

    [Header("Görsel Çevre & Gerçekçi Kirlilik (AQI) Ayarları")]
    [Tooltip("Canvas'ta oluşturduğumuz SmogOverlay görselini buraya sürükle")]
    public Image smogOverlay;
    public float stage1AqiThreshold = 100f; // Sararma başlangıcı (Hassas gruplar için sağlıksız)
    public float stage2AqiThreshold = 200f; // Ölüm başlangıcı (Tehlikeli kirlilik)

    // Sahnedeki tüm doğa objelerini ve zemin görsellerini buradan yöneteceğiz
    public List<NatureVisuals> activeNatures = new List<NatureVisuals>();
    private List<SpriteRenderer> groundRenderers = new List<SpriteRenderer>();

    [Header("Harita Tasarım Tarayıcısı")]
    public Transform startingObjectsParent;

    [Header("Rastgele Doğa Üreticisi")]
    public GameObject treeAPrefab;
    public GameObject treeBPrefab;
    public GameObject bushPrefab;
    public GameObject grassPrefab;

    public int treeACount = 10;
    public int treeBCount = 10;
    public int bushCount = 15;
    public int grassCount = 20;

    [Header("Doğa Boşluk (Nefes) Ayarları")]
    public int treeSpacing = 2;
    public int bushSpacing = 1;
    public int grassSpacing = 1;

    [Header("UI (Arayüz) Referansları")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI happinessText;
    public TextMeshProUGUI aqiText;

    private TMP_FontAsset runtimeGeneratedMainFont;
    private TMP_FontAsset runtimeGeneratedBodyFont;
    private TMP_FontAsset defaultCleanFont;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (aqi < 15f) aqi = 18f;
        if (stage1AqiThreshold < 80f) stage1AqiThreshold = 100f;
        if (stage2AqiThreshold < 150f) stage2AqiThreshold = 200f;
        peakMoney = money;

        if (moneyText != null && moneyText.font != null && moneyText.font.name.Contains("LiberationSans"))
        {
            defaultCleanFont = moneyText.font;
        }
        else if (TMP_Settings.defaultFontAsset != null)
        {
            defaultCleanFont = TMP_Settings.defaultFontAsset;
        }

        AutoDetectCustomFontInProject();

        // Yöneticileri (Dil, Ses, Danışman Karakter, Gazete, Menü & Görev, Gökyüzü Kuşları) otomatik başlat
        if (GetComponent<LocalizationManager>() == null && LocalizationManager.Instance == null)
            gameObject.AddComponent<LocalizationManager>();
        if (GetComponent<AudioManager>() == null && AudioManager.Instance == null)
            gameObject.AddComponent<AudioManager>();
        if (GetComponent<AdvisorManager>() == null && AdvisorManager.Instance == null)
            gameObject.AddComponent<AdvisorManager>();
        if (GetComponent<NewspaperManager>() == null && NewspaperManager.Instance == null)
            gameObject.AddComponent<NewspaperManager>();
        if (GetComponent<QuestAndMenuManager>() == null && QuestAndMenuManager.Instance == null)
            gameObject.AddComponent<QuestAndMenuManager>();
        if (GetComponent<BirdFlockManager>() == null && BirdFlockManager.Instance == null)
            gameObject.AddComponent<BirdFlockManager>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoDetectCustomFontInProject();
    }
#endif

    private void AutoDetectCustomFontInProject()
    {
        // 1. Önce Resources klasöründen yükle (Hem Unity Editor'de hem de Windows .exe Build'de %100 çalışır!)
        if (defaultCleanFont == null)
        {
            defaultCleanFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
        if (customMainFont == null && rawMainTtfFont == null)
        {
            rawMainTtfFont = Resources.Load<Font>("Fonts/NormalFont");
        }

#if UNITY_EDITOR
        if (defaultCleanFont == null)
        {
            defaultCleanFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        if (customMainFont == null && rawMainTtfFont == null)
        {
            string[] fontGuids = UnityEditor.AssetDatabase.FindAssets("t:Font");
            foreach (string guid in fontGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("LiberationSans") && !path.StartsWith("Packages/"))
                {
                    rawMainTtfFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(path);
                    break;
                }
            }
        }
#endif
    }

    public TMP_FontAsset GetHeaderFont()
    {
        if (customMainFont != null) return customMainFont;
        if (rawMainTtfFont != null)
        {
            if (runtimeGeneratedMainFont == null)
            {
                runtimeGeneratedMainFont = TMP_FontAsset.CreateFontAsset(rawMainTtfFont);
                if (runtimeGeneratedMainFont != null && runtimeGeneratedMainFont.atlasTexture != null)
                {
                    runtimeGeneratedMainFont.atlasTexture.filterMode = FilterMode.Point;
                }
            }
            return runtimeGeneratedMainFont;
        }
        return defaultCleanFont;
    }

    public TMP_FontAsset GetBodyFont()
    {
        if (customBodyFont != null) return customBodyFont;
        if (rawBodyTtfFont != null)
        {
            if (runtimeGeneratedBodyFont == null)
            {
                runtimeGeneratedBodyFont = TMP_FontAsset.CreateFontAsset(rawBodyTtfFont);
            }
            return runtimeGeneratedBodyFont;
        }
        return defaultCleanFont;
    }

    public void ApplyFontsToAllUI()
    {
        TMP_FontAsset headerF = GetHeaderFont();
        TMP_FontAsset bodyF = GetBodyFont();

        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        foreach (var txt in allTexts)
        {
            if (txt == null) continue;
            string objName = txt.gameObject.name;

            // NinjaAdventure (NormalFont.ttf) sadece BÜYÜK HARF başlık ve butonlarda harika durur;
            // küçük harfli paragraf/cümlelerde harf kalınlığı bozulduğu ve boşluk (space) dar olduğu için
            // başlık/butonlara HeaderFont (+geniş kelime boşluğu), paragraf ve sayılara ise temiz BodyFont uyguluyoruz!
            bool isHeaderOrButton =
                objName == "GameTitle" ||
                objName == "Masthead" ||
                objName == "Headline" ||
                objName == "HowToTitle" ||
                objName == "ScoreTitle" ||
                objName == "BadgeText" ||
                objName == "QuestHeader" ||
                objName == "SpeakerName" ||
                (txt.transform.parent != null && txt.transform.parent.GetComponent<Button>() != null);

            if (isHeaderOrButton && headerF != null)
            {
                txt.font = headerF;
                txt.wordSpacing = 22f; // Kelimeler arası boşluğun yapışmasını engeller
                txt.characterSpacing = 2.5f;
            }
            else if (bodyF != null)
            {
                txt.font = bodyF;
                txt.wordSpacing = 0f;
                txt.characterSpacing = 0f;
            }
        }
    }

    private void Start()
    {
        CacheGroundRenderers();
        CompactExistingSceneHUD();

        ScanStartingObjects();
        SpawnRandomNature();

        UpdateEnvironmentVisuals();

        InvokeRepeating(nameof(ProcessTurn), turnDuration, turnDuration);
        UpdateUI();
        ApplyFontsToAllUI();
    }

    private void Update()
    {
        if (!isGamePaused)
        {
            elapsedPlayTime += Time.deltaTime;
        }
        if (money > peakMoney)
        {
            peakMoney = money;
        }
    }

    // 6-8.5 dakikalık dengeli oyun süresi için dakikaya göre hız çarpanı
    // 0-1 dk: 0.55x | 1-2.5 dk: 0.70x | 2.5-4.5 dk: 0.88x | 4.5-6.5 dk: 1.08x | 6.5+ dk: 1.28x
    public float GetPaceMultiplier()
    {
        float minutes = elapsedPlayTime / 60f;
        if (minutes < 1.0f) return 0.55f;
        if (minutes < 2.5f) return 0.70f;
        if (minutes < 4.5f) return 0.88f;
        if (minutes < 6.5f) return 1.08f;
        return 1.28f;
    }

    private void CacheGroundRenderers()
    {
        groundRenderers.Clear();
        GameObject floorParent = GameObject.Find("FloorMainParent");
        if (floorParent != null)
        {
            SpriteRenderer[] srs = floorParent.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                if (sr != null) groundRenderers.Add(sr);
            }
        }
    }

    private void CompactExistingSceneHUD()
    {
        GameObject valuesObj = GameObject.Find("Values");
        if (valuesObj != null)
        {
            RectTransform rt = valuesObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = new Vector3(0.72f, 0.72f, 1f);
                rt.anchoredPosition = new Vector2(-105f, 58f);
            }
        }

        string[] btnNames = { "Upgrade", "Cursor", "Delete", "House", "Factory" };
        float[] targetX = { 162f, 212f, 262f, 312f, 362f };
        for (int i = 0; i < btnNames.Length; i++)
        {
            GameObject btnObj = GameObject.Find(btnNames[i]);
            if (btnObj != null)
            {
                RectTransform brt = btnObj.GetComponent<RectTransform>();
                if (brt != null)
                {
                    brt.sizeDelta = new Vector2(46f, 46f);
                    brt.anchoredPosition = new Vector2(targetX[i], -196f);
                }
            }
        }
    }

    public void SetSceneHUDVisible(bool visible)
    {
        GameObject valuesObj = GameObject.Find("Values");
        if (valuesObj != null)
        {
            CanvasGroup cg = valuesObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = valuesObj.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
        }

        string[] btnNames = { "Upgrade", "Cursor", "Delete", "House", "Factory" };
        for (int i = 0; i < btnNames.Length; i++)
        {
            GameObject btnObj = GameObject.Find(btnNames[i]);
            if (btnObj != null)
            {
                CanvasGroup cg = btnObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = btnObj.AddComponent<CanvasGroup>();
                cg.alpha = visible ? 1f : 0f;
                cg.blocksRaycasts = visible;
            }
        }
    }

    public void SetPaused(bool paused)
    {
        isGamePaused = paused;
    }

    public int GetFactoryCount()
    {
        int count = 0;
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            if (activeBuildings[i] != null && activeBuildings[i] is Factory) count++;
        }
        return count;
    }

    public int GetHouseCount()
    {
        int count = 0;
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            if (activeBuildings[i] != null && activeBuildings[i] is House) count++;
        }
        return count;
    }

    public bool HasAnyUpgradedBuilding()
    {
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            if (activeBuildings[i] != null && activeBuildings[i].currentLevel > 0) return true;
        }
        return false;
    }

    public void SaveRunScoreIfNeeded()
    {
        if (runScoreSaved) return;
        if (elapsedPlayTime < 10f && GetFactoryCount() == 0) return;

        runScoreSaved = true;
        int curPeak = Mathf.FloorToInt(Mathf.Max(peakMoney, money));
        int curAqi = Mathf.FloorToInt(aqi);
        int curTime = Mathf.FloorToInt(elapsedPlayTime);

        int bestProfit = PlayerPrefs.GetInt("BestPeakProfit", 0);
        if (curPeak > bestProfit)
        {
            PlayerPrefs.SetInt("BestPeakProfit", curPeak);
        }

        string run2 = PlayerPrefs.GetString("RunHistory_1", "");
        if (!string.IsNullOrEmpty(run2)) PlayerPrefs.SetString("RunHistory_3", PlayerPrefs.GetString("RunHistory_2", ""));

        int mins = curTime / 60;
        int secs = curTime % 60;
        string newRecord = $"${curPeak}  |  AQI: {curAqi}  |  {mins:00}:{secs:00}";
        PlayerPrefs.SetString("RunHistory_2", PlayerPrefs.GetString("RunHistory_1", ""));
        PlayerPrefs.SetString("RunHistory_1", newRecord);

        PlayerPrefs.SetInt("LastPeakProfit", curPeak);
        PlayerPrefs.SetInt("LastFinalAqi", curAqi);
        PlayerPrefs.SetInt("LastPlayTime", curTime);
        PlayerPrefs.Save();
    }

    public string GetFormattedScoreHistory()
    {
        int bestProfit = PlayerPrefs.GetInt("BestPeakProfit", 0);
        string r1 = PlayerPrefs.GetString("RunHistory_1", "");
        string r2 = PlayerPrefs.GetString("RunHistory_2", "");
        string r3 = PlayerPrefs.GetString("RunHistory_3", "");

        if (bestProfit <= 0 && string.IsNullOrEmpty(r1))
        {
            return LocalizationManager.Get(
                "No completed runs yet. Build your city and chase maximum profit!",
                "Henüz tamamlanmış oyun yok. Şehrini kur ve maksimum kâra ulaş!"
            );
        }

        string header = LocalizationManager.Get(
            $"<b>BEST PEAK PROFIT:</b> <color=#F5CF60>${bestProfit}</color>",
            $"<b>EN YÜKSEK KÂR REKORU:</b> <color=#F5CF60>${bestProfit}</color>"
        );

        string recentTitle = LocalizationManager.Get("Recent Runs (Max Profit | Last AQI | Time):", "Son Oyunlar (Maks. Kâr | Son AQI | Süre):");
        string lines = $"{header}\n<size=10.5><color=#C8BFA8>{recentTitle}</color></size>";
        if (!string.IsNullOrEmpty(r1)) lines += $"\n• {r1}";
        if (!string.IsNullOrEmpty(r2)) lines += $"\n• {r2}";
        if (!string.IsNullOrEmpty(r3)) lines += $"\n• {r3}";
        return lines;
    }

    private void ScanStartingObjects()
    {
        if (startingObjectsParent == null) return;
        var grid = BuildingManager.Instance.gridManager;
        if (grid == null || grid.gridMatrix == null) return;

        foreach (Transform child in startingObjectsParent)
        {
            int startX = Mathf.RoundToInt(child.position.x);
            int startY = Mathf.RoundToInt(child.position.y);

            if (startX < 0 || startX >= grid.gridWidth || startY < 0 || startY >= grid.gridHeight)
            {
                Destroy(child.gameObject);
                continue;
            }

            TileScript tile = grid.gridMatrix[startX, startY];
            BaseBuilding buildingData = child.GetComponent<BaseBuilding>();

            if (buildingData != null)
            {
                float centerX = startX + (buildingData.buildingWidth - 1) / 2f;
                float centerY = startY + (buildingData.buildingHeight - 1) / 2f;
                child.position = new Vector3(centerX, centerY, 0f);

                buildingData.startX = startX;
                buildingData.startY = startY;
                buildingData.UpdateDepth();

                for (int x = startX; x < startX + buildingData.buildingWidth; x++)
                {
                    for (int y = startY; y < startY + buildingData.buildingHeight; y++)
                    {
                        if (x < grid.gridWidth && y < grid.gridHeight)
                        {
                            grid.gridMatrix[x, y].isTileFull = true;
                            grid.gridMatrix[x, y].occupiedBy = buildingData;
                        }
                    }
                }
                RegisterBuilding(buildingData, false);
            }
            else
            {
                if (tile.environmentObj == null && !tile.isTileFull)
                {
                    child.position = new Vector3(startX, startY, 0f);
                    SpriteRenderer[] srs = child.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sr in srs) sr.sortingOrder = Mathf.RoundToInt(-child.position.y * 10f);
                    tile.environmentObj = child.gameObject;

                    NatureVisuals nv = child.GetComponent<NatureVisuals>();
                    if (nv != null)
                    {
                        nv.CreateShadowIfNeeded();
                        nv.SyncShadowOrder();
                        activeNatures.Add(nv);
                    }
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }
        }

        maxPopulation = 0f;
        foreach (var b in activeBuildings)
        {
            if (b is House house) maxPopulation += house.populationCapacity[house.currentLevel];
        }
        population = Mathf.Clamp(startingPopulation, 0f, maxPopulation == 0 ? startingPopulation : maxPopulation);
    }

    private void SpawnRandomNature()
    {
        SpawnSpecificNature(treeAPrefab, treeACount, 3, 3, treeSpacing);
        SpawnSpecificNature(treeBPrefab, treeBCount, 3, 3, treeSpacing);
        SpawnSpecificNature(bushPrefab, bushCount, 2, 1, bushSpacing);
        SpawnSpecificNature(grassPrefab, grassCount, 1, 1, grassSpacing);
    }

    private void SpawnSpecificNature(GameObject prefab, int targetCount, int width, int height, int spacing)
    {
        if (prefab == null) return;
        var grid = BuildingManager.Instance.gridManager;
        int placedCount = 0;
        int attempts = 0;
        int maxAttempts = 5000;

        while (placedCount < targetCount && attempts < maxAttempts)
        {
            attempts++;
            int randX = Random.Range(0, grid.gridWidth - width + 1);
            int randY = Random.Range(0, grid.gridHeight - height + 1);

            if (CanPlaceNature(randX, randY, width, height, spacing))
            {
                GameObject newNature = Instantiate(prefab);
                if (startingObjectsParent != null) newNature.transform.SetParent(startingObjectsParent);

                float centerX = randX + (width - 1) / 2f;
                float centerY = randY + (height - 1) / 2f;
                newNature.transform.position = new Vector3(centerX, centerY, 0f);

                SpriteRenderer[] srs = newNature.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in srs) sr.sortingOrder = Mathf.RoundToInt(-randY * 10f);

                for (int x = randX; x < randX + width; x++)
                {
                    for (int y = randY; y < randY + height; y++)
                    {
                        grid.gridMatrix[x, y].environmentObj = newNature;
                    }
                }

                NatureVisuals nv = newNature.GetComponent<NatureVisuals>();
                if (nv != null)
                {
                    nv.CreateShadowIfNeeded();
                    nv.SyncShadowOrder();
                    activeNatures.Add(nv);
                }

                placedCount++;
            }
        }
    }

    private bool CanPlaceNature(int startX, int startY, int width, int height, int spacing)
    {
        var grid = BuildingManager.Instance.gridManager;
        if (startX + width > grid.gridWidth || startY + height > grid.gridHeight) return false;

        int checkStartX = Mathf.Max(0, startX - spacing);
        int checkStartY = Mathf.Max(0, startY - spacing);
        int checkEndX = Mathf.Min(grid.gridWidth - 1, startX + width - 1 + spacing);
        int checkEndY = Mathf.Min(grid.gridHeight - 1, startY + height - 1 + spacing);

        for (int x = checkStartX; x <= checkEndX; x++)
        {
            for (int y = checkStartY; y <= checkEndY; y++)
            {
                TileScript tile = grid.gridMatrix[x, y];
                if (tile.isTileFull || tile.environmentObj != null) return false;
            }
        }
        return true;
    }

    public void RegisterBuilding(BaseBuilding building, bool checkEvents = true)
    {
        if (!activeBuildings.Contains(building)) activeBuildings.Add(building);
        RecalculateCapacities();
        UpdateUI();
        if (checkEvents)
        {
            NewspaperManager.Instance?.CheckMilestones();
        }
    }

    public void UnregisterBuilding(BaseBuilding building)
    {
        if (activeBuildings.Contains(building)) activeBuildings.Remove(building);
        RecalculateCapacities();
        UpdateUI();
    }

    public void RecalculateCapacities()
    {
        maxPopulation = 0f;
        totalWorkersNeeded = 0f;
        foreach (var building in activeBuildings)
        {
            if (building is Factory factory)
                totalWorkersNeeded += factory.workerNeed[factory.currentLevel];
            else if (building is House house)
                maxPopulation += house.populationCapacity[house.currentLevel];
        }
    }

    private void ProcessTurn()
    {
        if (isGamePaused) return;

        float rawProduction = 0f;
        float rawAqi = 0f;

        maxPopulation = 0f;
        totalWorkersNeeded = 0f;

        foreach (var building in activeBuildings)
        {
            if (building is Factory factory)
            {
                rawProduction += factory.moneyProduction[factory.currentLevel];
                rawAqi += factory.pollutionRate[factory.currentLevel];
                totalWorkersNeeded += factory.workerNeed[factory.currentLevel];
            }
            else if (building is House house)
            {
                maxPopulation += house.populationCapacity[house.currentLevel];
            }
        }

        float efficiency = 1f;
        if (totalWorkersNeeded > 0)
        {
            efficiency = Mathf.Clamp01(population / totalWorkersNeeded);
        }

        float pace = GetPaceMultiplier();

        // Gelir ve kirlilik dakikaya bağlı hız çarpanı ile ölçeklenir
        totalProductionRate = rawProduction * efficiency * pace;

        // 6-8.5 dakikalık dengeli ilerleme için yumuşatılmış kirlilik eğrisi
        float scaledPollution = rawAqi;
        if (rawAqi > 1.15f)
        {
            scaledPollution = 1.15f + Mathf.Pow(rawAqi - 1.15f, 0.55f) * 0.52f;
        }

        int factoryCount = GetFactoryCount();
        float industrialCreep = (factoryCount > 0 && elapsedPlayTime > 75f)
            ? Mathf.InverseLerp(75f, 300f, elapsedPlayTime) * 0.42f
            : 0f;

        float actualAqi = (scaledPollution * efficiency + industrialCreep) * pace;
        actualAqi = Mathf.Min(actualAqi, 2.05f);

        money += totalProductionRate;
        if (money > peakMoney) peakMoney = money;

        aqi += actualAqi;

        CalculateHappiness();
        UpdatePopulation();
        UpdateEnvironmentVisuals();
        UpdateUI();

        NewspaperManager.Instance?.CheckMilestones();
    }

    private void UpdateEnvironmentVisuals()
    {
        // Kirlilik Evresini Belirle (0: Temiz < 100, 1: Sararmış >= 100, 2: Kurumuş >= 200)
        int currentStage = 0;
        if (aqi >= stage2AqiThreshold) currentStage = 2;
        else if (aqi >= stage1AqiThreshold) currentStage = 1;

        for (int i = activeNatures.Count - 1; i >= 0; i--)
        {
            if (activeNatures[i] != null)
            {
                activeNatures[i].UpdateVisual(currentStage, aqi);
            }
            else
            {
                activeNatures.RemoveAt(i);
            }
        }

        // Zemin çimlerinin (FloorMainParent) AQI arttıkça kademeli olarak sararıp solması
        float grassWitherT = Mathf.InverseLerp(65f, 215f, aqi);
        Color targetGrassColor = Color.Lerp(Color.white, new Color(0.83f, 0.71f, 0.43f, 1f), grassWitherT);
        for (int i = 0; i < groundRenderers.Count; i++)
        {
            if (groundRenderers[i] != null)
            {
                groundRenderers[i].color = Color.Lerp(groundRenderers[i].color, targetGrassColor, 0.4f);
            }
        }

        // SmogOverlay: Maksimum opaklık 0.40f olarak sınırlandı
        if (smogOverlay != null)
        {
            Color c = smogOverlay.color;
            float effectiveSmogAqi = Mathf.Max(0f, aqi - 45f);
            float maxSmogRange = 205f;
            float targetAlpha = Mathf.Clamp((effectiveSmogAqi / maxSmogRange) * 0.40f, 0f, 0.40f);
            c.a = Mathf.Lerp(c.a, targetAlpha, 0.5f);
            smogOverlay.color = c;
        }
    }

    private void CalculateHappiness()
    {
        float baseHappiness = 65f;
        float economyBonus = Mathf.Clamp(totalProductionRate * 0.60f, 0f, 26f);

        // AQI 60'a kadar ceza yok. AQI 200+ olduğunda mutluluk çok daha sert ve hızlı düşer!
        float effectiveAqi = Mathf.Max(0f, aqi - 60f);
        float pollutionPenalty = Mathf.Pow(effectiveAqi / 25f, 2f);
        if (aqi >= 200f)
        {
            // AQI 200'ü geçince zehirli duman mutluluğu hızla sıfıra çeker
            pollutionPenalty += (aqi - 195f) * 1.55f;
        }

        float requiredProduction = population * 0.35f;
        float productionDeficitPenalty = 0f;

        if (GetFactoryCount() > 0 && totalProductionRate < requiredProduction)
        {
            productionDeficitPenalty = Mathf.Clamp((requiredProduction - totalProductionRate) * 0.25f, 0f, 12f);
        }

        happiness = Mathf.Clamp(baseHappiness + economyBonus - pollutionPenalty - productionDeficitPenalty, 0f, 100f);
    }

    private void UpdatePopulation()
    {
        if (maxPopulation > 0)
        {
            float growthFactor = (happiness - 35f) * 0.042f;
            if (growthFactor > 0f)
            {
                growthFactor = Mathf.Min(growthFactor, 0.95f);
            }
            else
            {
                // AQI 200+ olduğunda göç hızı belirginleşir ve yüksek ev kapasitesinde bile ~45-60 saniyede tamamlanır
                if (aqi >= 200f)
                {
                    float proportionalDrain = Mathf.Max(2.2f, maxPopulation * 0.048f);
                    growthFactor = Mathf.Min(growthFactor * 1.65f, -1.6f);
                    growthFactor = Mathf.Max(growthFactor, -proportionalDrain);
                }
                else
                {
                    growthFactor = Mathf.Max(growthFactor, -1.15f);
                }
            }

            population += growthFactor;

            if (population < 0.90f && happiness < 30f)
            {
                population = 0f;
            }
        }
        population = Mathf.Clamp(population, 0f, maxPopulation);
    }

    public void ForceUpdateUI()
    {
        RecalculateCapacities();
        UpdateEnvironmentVisuals();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = $"${Mathf.FloorToInt(money)}";

        if (populationText != null)
        {
            string needLabel = LocalizationManager.Get("Need", "İhtiyaç");
            populationText.text = $"{Mathf.FloorToInt(population)} / {maxPopulation} ({needLabel}: {totalWorkersNeeded})";
        }

        if (happinessText != null)
            happinessText.text = $"{Mathf.FloorToInt(happiness)}%";

        if (aqiText != null)
        {
            int aqiInt = Mathf.FloorToInt(aqi);
            string status;
            if (aqiInt < 50) status = LocalizationManager.Get("Good", "İyi");
            else if (aqiInt < 100) status = LocalizationManager.Get("Moderate", "Orta");
            else if (aqiInt < 200) status = LocalizationManager.Get("Unhealthy", "Kirli");
            else status = LocalizationManager.Get("Hazardous!", "Tehlikeli!");

            aqiText.text = $"{aqiInt} ({status})";
        }
    }
}
