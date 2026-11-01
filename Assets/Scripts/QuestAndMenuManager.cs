using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class QuestAndMenuManager : MonoBehaviour
{
    public static QuestAndMenuManager Instance;

    [Header("Menü Ayarları")]
    public bool showMainMenuOnStart = true;

    private GameObject menuOverlay;
    private GameObject howToPlayPanel;
    private TextMeshProUGUI badgeText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI playBtnText;
    private TextMeshProUGUI howToBtnText;
    private TextMeshProUGUI langBtnText;
    private TextMeshProUGUI soundBtnText;
    private TextMeshProUGUI restartBtnText;
    private TextMeshProUGUI quitBtnText;
    private TextMeshProUGUI scoreBoardTitleText;
    private TextMeshProUGUI scoreBoardBodyText;
    private TextMeshProUGUI briefingText;
    private TextMeshProUGUI howToTitleText;
    private TextMeshProUGUI howToContentText;
    private TextMeshProUGUI closeHowBtnText;
    private Button restartMenuBtn;

    // Oyun İçi Sağ Üst Görev & Hızlı Butonlar
    private GameObject topRightBarObj;
    private GameObject questCardObj;
    private GameObject toolBarObj;
    private TextMeshProUGUI topLangBtnText;
    private TextMeshProUGUI topMenuBtnText;
    private TextMeshProUGUI questHeaderText;
    private TextMeshProUGUI questDescText;
    private Image questCardBg;

    // Sağ Alt Araç Bilgi Etiketi
    private TextMeshProUGUI toolInfoText;

    private bool hasGameStarted = false;
    private int currentQuestIndex = 0;
    private int initialHouseCount = -1;
    private float questFlashTimer = 0f;

    public bool IsMenuOpen => menuOverlay != null && menuOverlay.activeSelf;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    private void Start()
    {
        EnsureHUDCreated();
        EnsureMainMenuCreated();

        LocalizationManager.OnLanguageChanged += RefreshAllTexts;

        if (GameManager.Instance != null)
        {
            initialHouseCount = GameManager.Instance.GetHouseCount();
            GameManager.Instance.ApplyFontsToAllUI();
        }

        RefreshAllTexts();

        if (showMainMenuOnStart)
        {
            OpenMenu(true);
        }
        else
        {
            hasGameStarted = true;
            CloseMenu();
        }
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshAllTexts;
    }

    private void Update()
    {
        // ESC tuşu ile Pause Menüsü aç/kapat
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && hasGameStarted)
        {
            if (menuOverlay != null && menuOverlay.activeSelf) CloseMenu();
            else OpenMenu(false);
        }

        if (GameManager.Instance != null && initialHouseCount < 0)
        {
            initialHouseCount = GameManager.Instance.GetHouseCount();
        }

        UpdateQuestProgress();
        UpdateToolInfoText();

        if (questFlashTimer > 0f)
        {
            questFlashTimer -= Time.unscaledDeltaTime;
            if (questCardBg != null)
            {
                float pulse = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
                questCardBg.color = Color.Lerp(new Color(0.14f, 0.13f, 0.11f, 0.88f), new Color(0.28f, 0.45f, 0.18f, 0.94f), pulse);
            }
        }
        else if (questCardBg != null)
        {
            questCardBg.color = new Color(0.14f, 0.13f, 0.11f, 0.88f);
        }
    }

    private void UpdateQuestProgress()
    {
        if (GameManager.Instance == null) return;
        var gm = GameManager.Instance;

        int factories = gm.GetFactoryCount();
        int houses = gm.GetHouseCount();
        bool anyUpgraded = gm.HasAnyUpgradedBuilding();
        int targetHouses = Mathf.Max(5, initialHouseCount + 1);

        bool completedStep = false;

        if (currentQuestIndex == 0 && factories >= 1)
        {
            currentQuestIndex = 1;
            completedStep = true;
        }
        else if (currentQuestIndex == 1 && houses >= targetHouses)
        {
            currentQuestIndex = 2;
            completedStep = true;
        }
        else if (currentQuestIndex == 2 && anyUpgraded)
        {
            currentQuestIndex = 3;
            completedStep = true;
        }
        else if (currentQuestIndex == 3 && factories >= 3)
        {
            currentQuestIndex = 4;
            completedStep = true;
        }
        else if (currentQuestIndex == 4 && gm.money >= 1000f)
        {
            currentQuestIndex = 5;
            completedStep = true;
        }

        if (completedStep)
        {
            AudioManager.Instance?.PlayQuestComplete();
            questFlashTimer = 2.0f;
            gm.money += 50f;
            gm.ForceUpdateUI();
        }

        RefreshQuestText(factories, houses, targetHouses);
    }

    private void RefreshQuestText(int factories, int houses, int targetHouses)
    {
        if (questHeaderText == null || questDescText == null) return;

        questHeaderText.text = LocalizationManager.Get("OBJECTIVE (+$50 Bonus)", "AKTİF GÖREV (+50$)");

        switch (currentQuestIndex)
        {
            case 0:
                questDescText.text = LocalizationManager.Get(
                    $"1/5: Build First Factory ({factories}/1)\n<size=8.8><color=#D2C5A8>Select Factory at bottom-right ($100).</color></size>",
                    $"1/5: İlk Fabrikanı Kur ({factories}/1)\n<size=8.8><color=#D2C5A8>Sağ alttan Fabrika seçip kur (100$).</color></size>"
                );
                break;
            case 1:
                int builtNew = Mathf.Clamp(houses - initialHouseCount, 0, 1);
                questDescText.text = LocalizationManager.Get(
                    $"2/5: Build a House ({builtNew}/1)\n<size=8.8><color=#D2C5A8>Provide workers by building a House ($50).</color></size>",
                    $"2/5: Yeni Ev İnşa Et ({builtNew}/1)\n<size=8.8><color=#D2C5A8>İşçiler için sağ alttan Ev kur (50$).</color></size>"
                );
                break;
            case 2:
                questDescText.text = LocalizationManager.Get(
                    "3/5: Upgrade a Building (0/1)\n<size=8.8><color=#D2C5A8>Click Upgrade (arrow) & click a building.</color></size>",
                    "3/5: Bir Binayı Yükselt (0/1)\n<size=8.8><color=#D2C5A8>Yükseltme (ok) aracını seçip binaya tıkla.</color></size>"
                );
                break;
            case 3:
                questDescText.text = LocalizationManager.Get(
                    $"4/5: Expand Industry ({Mathf.Min(factories, 3)}/3)\n<size=8.8><color=#D2C5A8>Reach 3 active Factories in the city.</color></size>",
                    $"4/5: Sanayiyi Büyüt ({Mathf.Min(factories, 3)}/3)\n<size=8.8><color=#D2C5A8>Şehirdeki fabrika sayısını 3'e çıkar.</color></size>"
                );
                break;
            case 4:
                int curMoney = GameManager.Instance != null ? Mathf.FloorToInt(GameManager.Instance.money) : 0;
                questDescText.text = LocalizationManager.Get(
                    $"5/5: Reach $1,000 (${curMoney}/$1000)\n<size=8.8><color=#D2C5A8>Upgrade buildings to maximize profit!</color></size>",
                    $"5/5: 1.000$ Kâra Ulaş (${curMoney}/$1000)\n<size=8.8><color=#D2C5A8>Binaları yükselterek geliri katla!</color></size>"
                );
                break;
            default:
                questHeaderText.text = LocalizationManager.Get("MAXIMIZE PROFIT!", "MAKSİMUM KÂR!");
                questDescText.text = LocalizationManager.Get(
                    "Keep expanding your industrial empire and beat your record!",
                    "Sanayi imparatorluğunu büyütmeye ve rekor kırmaya devam et!"
                );
                break;
        }
    }

    private void UpdateToolInfoText()
    {
        if (toolInfoText == null || BuildingManager.Instance == null) return;

        var bm = BuildingManager.Instance;
        switch (bm.currentMode)
        {
            case ActionMode.Cursor:
                toolInfoText.text = LocalizationManager.Get("Cursor | Right-Click: Pan | Scroll: Zoom", "Gezinme | Sağ Tık: Kamera | Tekerlek: Zoom");
                break;
            case ActionMode.Build:
                if (bm.selectedPrefab != null && bm.selectedPrefab.name.Contains("Factory"))
                {
                    toolInfoText.text = LocalizationManager.Get("Factory ($100) — +Income, 5 Workers", "Fabrika (100$) — +Gelir, 5 İşçi");
                }
                else
                {
                    toolInfoText.text = LocalizationManager.Get("House ($50) — +10 Max Population", "Ev (50$) — +10 Maks. Nüfus");
                }
                break;
            case ActionMode.Delete:
                toolInfoText.text = LocalizationManager.Get("Demolish — Click building or tree", "Yıkım — Bina veya ağaca tıkla");
                break;
            case ActionMode.Upgrade:
                toolInfoText.text = LocalizationManager.Get("Upgrade — House ($75/$150) | Factory ($150/$300)", "Yükselt — Ev (75$/150$) | Fabrika (150$/300$)");
                break;
        }
    }

    public void OpenMenu(bool isInitialStart)
    {
        EnsureMainMenuCreated();
        if (!isInitialStart) AudioManager.Instance?.PlayClick();

        menuOverlay.SetActive(true);
        menuOverlay.transform.SetAsLastSibling();

        // Menü açıkken arka plandaki HUD elemanlarını gizle (tam ekran temiz ve profesyonel görünüm)
        SetHUDPanelsActive(false);

        if (restartMenuBtn != null)
        {
            restartMenuBtn.gameObject.SetActive(hasGameStarted);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPaused(true);
            GameManager.Instance.ApplyFontsToAllUI();
        }
        RefreshAllTexts();
    }

    public void CloseMenu()
    {
        AudioManager.Instance?.PlayClick();
        hasGameStarted = true;
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (menuOverlay != null) menuOverlay.SetActive(false);

        SetHUDPanelsActive(true);

        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
    }

    private void SetHUDPanelsActive(bool visible)
    {
        if (topRightBarObj != null) topRightBarObj.SetActive(visible);
        if (questCardObj != null) questCardObj.SetActive(visible);
        if (toolBarObj != null) toolBarObj.SetActive(visible);
        if (GameManager.Instance != null) GameManager.Instance.SetSceneHUDVisible(visible);
        if (AdvisorManager.Instance != null) AdvisorManager.Instance.SetHUDVisible(visible);
    }

    public void OnToggleLanguageClicked()
    {
        AudioManager.Instance?.PlayClick();
        LocalizationManager.ToggleLanguage();
        if (GameManager.Instance != null) GameManager.Instance.ForceUpdateUI();
    }

    public void OnToggleSoundClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMute();
            AudioManager.Instance.PlayClick();
        }
        RefreshAllTexts();
    }

    public void OnRestartClicked()
    {
        AudioManager.Instance?.PlayClick();
        if (GameManager.Instance != null) GameManager.Instance.SaveRunScoreIfNeeded();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnQuitClicked()
    {
        AudioManager.Instance?.PlayClick();
        if (GameManager.Instance != null) GameManager.Instance.SaveRunScoreIfNeeded();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // WebGL tarayıcı sekmesini kapatamadığı için oyunu sıfırlayıp Ana Menüye döner
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#else
        Application.Quit();
#endif
    }

    private void RefreshAllTexts()
    {
        if (badgeText != null)
            badgeText.text = LocalizationManager.Get("EXECUTIVE MAYORAL SUITE  •  INDUSTRIAL TYCOON", "BELEDİYE BAŞKANLIĞI  •  SANAYİ VE EKONOMİ YÖNETİMİ");

        // Plot Twist korunuyor: Başlık "I NEED MORE PROFIT!", alt başlıkta "Collapse" ibaresi kesinlikle yok!
        if (titleText != null)
            titleText.text = LocalizationManager.Get("I NEED MORE PROFIT!", "DAHA FAZLA KÂR LAZIM!");
        if (subtitleText != null)
            subtitleText.text = LocalizationManager.Get("Industrial City-Building & Economic Tycoon Simulation", "Endüstriyel Şehir Kurma ve Ekonomi Simülasyonu");

        if (playBtnText != null)
            playBtnText.text = hasGameStarted
                ? LocalizationManager.Get("RESUME CITY", "OYUNA DEVAM ET")
                : LocalizationManager.Get("START GAME", "OYUNA BAŞLA");

        if (howToBtnText != null)
            howToBtnText.text = LocalizationManager.Get("HOW TO PLAY", "NASIL OYNANIR?");

        // "(Click for TR)" kaldırıldı
        if (langBtnText != null)
            langBtnText.text = LocalizationManager.Get("LANGUAGE: ENGLISH", "DİL: TÜRKÇE");

        bool muted = AudioManager.Instance != null && AudioManager.Instance.isMuted;
        if (soundBtnText != null)
        {
            soundBtnText.text = muted
                ? LocalizationManager.Get("AUDIO: MUTED", "SES: KAPALI")
                : LocalizationManager.Get("AUDIO: ON", "SES: AÇIK");
        }

        if (restartBtnText != null)
            restartBtnText.text = LocalizationManager.Get("RESTART CITY", "YENİDEN BAŞLAT");

        if (quitBtnText != null)
            quitBtnText.text = LocalizationManager.Get("QUIT GAME", "OYUNDAN ÇIK");

        if (scoreBoardTitleText != null)
            scoreBoardTitleText.text = LocalizationManager.Get("PROFIT RECORDS & RUN HISTORY", "KÂR REKORLARI & OYUN GEÇMİŞİ");

        if (scoreBoardBodyText != null && GameManager.Instance != null)
            scoreBoardBodyText.text = GameManager.Instance.GetFormattedScoreHistory();

        if (briefingText != null)
        {
            briefingText.text = LocalizationManager.Get(
                "<b>MAYOR'S BRIEFING:</b>\nBuild Factories, house your workforce, upgrade production lines, and grow the richest industrial city in history.\n\n<color=#E6C15A><i>Tip: Keep an eye on Air Quality (AQI) and Citizen Happiness while expanding your profits.</i></color>",
                "<b>BAŞKANIN GÖREV ÖZETİ:</b>\nFabrikalar kurun, işçileriniz için konutlar inşa edin, üretim hatlarını yükseltin ve tarihin en yüksek kârına ulaşın.\n\n<color=#E6C15A><i>İpucu: Kârınızı büyütürken Hava Kalitesi (AQI) ve Mutluluk değerlerine dikkat edin.</i></color>"
            );
        }

        if (howToTitleText != null)
            howToTitleText.text = LocalizationManager.Get("MAYOR'S GUIDE — HOW TO PLAY", "BAŞKANIN REHBERİ — NASIL OYNANIR?");

        // Plot twist'i ele vermeyen estetik How to Play metni
        if (howToContentText != null)
        {
            howToContentText.text = LocalizationManager.Get(
                "<color=#F0C85A><b>1. CAMERA & NAVIGATION</b></color>\n" +
                "Hold <b>Right-Click & Drag</b> to pan across your city. Use the <b>Mouse Wheel</b> to zoom in and out.\n\n" +
                "<color=#F0C85A><b>2. FACTORIES & PRODUCTION ($100)</b></color>\n" +
                "Factories generate steady profit every turn. Each factory requires <b>Workers</b> to operate at full efficiency.\n\n" +
                "<color=#F0C85A><b>3. HOUSES & WORKFORCE ($50)</b></color>\n" +
                "Build Houses to increase your city's maximum <b>Population</b> capacity so new workers move in.\n\n" +
                "<color=#F0C85A><b>4. UPGRADING BUILDINGS</b></color>\n" +
                "Select the <b>Upgrade (Arrow)</b> tool to level up Houses ($75 / $150) and Factories ($150 / $300) to Level 2 & 3.\n\n" +
                "<color=#8CD974><b>• MAYOR'S TIP:</b></color> Pay attention to <b>Air Quality (AQI)</b> and <b>Citizen Happiness</b> in the top-left panel while growing your city!",
                "<color=#F0C85A><b>1. KAMERA VE GEZİNME</b></color>\n" +
                "Haritada gezinmek için <b>Sağ Tık Basılı Tutup Sürükleyin</b>. Yakınlaşmak için <b>Fare Tekerleğini</b> kullanın.\n\n" +
                "<color=#F0C85A><b>2. FABRİKALAR VE ÜRETİM (100$)</b></color>\n" +
                "Fabrikalar her tur düzenli kâr üretir. Tam kapasite çalışmaları için yeterli sayıda <b>İşçiye</b> ihtiyaç duyarlar.\n\n" +
                "<color=#F0C85A><b>3. EVLER VE İŞ GÜCÜ (50$)</b></color>\n" +
                "Maksimum <b>Nüfus</b> kapasitesini artırmak ve fabrikalara işçi sağlamak için Evler inşa edin.\n\n" +
                "<color=#F0C85A><b>4. BİNA YÜKSELTME (UPGRADE)</b></color>\n" +
                "Sağ alttaki <b>Yükseltme (Ok)</b> aracını seçerek Evleri (75$/150$) ve Fabrikaları (150$/300$) Seviye 2 ve 3'e çıkarın.\n\n" +
                "<color=#8CD974><b>• BAŞKANIN İPUCU:</b></color> Şehrinizi büyütürken sol üstteki <b>Hava Kalitesi (AQI)</b> ve <b>Mutluluk</b> değerlerine dikkat edin!"
            );
        }

        if (closeHowBtnText != null)
            closeHowBtnText.text = LocalizationManager.Get("GOT IT", "ANLADIM");

        if (topLangBtnText != null)
            topLangBtnText.text = LocalizationManager.Get("EN / TR", "TR / EN");
        if (topMenuBtnText != null)
            topMenuBtnText.text = LocalizationManager.Get("MENU [ESC]", "MENÜ [ESC]");

        if (GameManager.Instance != null)
        {
            RefreshQuestText(GameManager.Instance.GetFactoryCount(), GameManager.Instance.GetHouseCount(), Mathf.Max(5, initialHouseCount + 1));
        }
    }

    private void EnsureHUDCreated()
    {
        if (questCardBg != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Sağ Üst Hızlı Butonlar (800x600 CanvasScaler ile uyumlu kompakt boyut)
        topRightBarObj = new GameObject("TopRightHUD_Bar", typeof(RectTransform));
        topRightBarObj.transform.SetParent(canvas.transform, false);
        RectTransform topBarRT = topRightBarObj.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(1f, 1f);
        topBarRT.anchorMax = new Vector2(1f, 1f);
        topBarRT.pivot = new Vector2(1f, 1f);
        topBarRT.anchoredPosition = new Vector2(-10f, -8f);
        topBarRT.sizeDelta = new Vector2(160f, 22f);

        Button langQuickBtn = CreateHudButton(topBarRT, "LangQuickBtn", new Vector2(-88f, 0f), new Vector2(58f, 20f), out topLangBtnText);
        langQuickBtn.onClick.AddListener(OnToggleLanguageClicked);

        Button menuQuickBtn = CreateHudButton(topBarRT, "MenuQuickBtn", new Vector2(0f, 0f), new Vector2(82f, 20f), out topMenuBtnText);
        menuQuickBtn.onClick.AddListener(() => OpenMenu(false));

        // Sağ Üst Görev (Objective) Kartı (Kompakt, ekranı kapatmaz)
        questCardObj = new GameObject("QuestObjective_Card", typeof(RectTransform), typeof(Image));
        questCardObj.transform.SetParent(canvas.transform, false);
        RectTransform questRT = questCardObj.GetComponent<RectTransform>();
        questRT.anchorMin = new Vector2(1f, 1f);
        questRT.anchorMax = new Vector2(1f, 1f);
        questRT.pivot = new Vector2(1f, 1f);
        questRT.anchoredPosition = new Vector2(-10f, -32f);
        questRT.sizeDelta = new Vector2(205f, 50f);

        questCardBg = questCardObj.GetComponent<Image>();
        questCardBg.color = new Color(0.14f, 0.13f, 0.11f, 0.88f);

        Outline qOutline = questCardObj.AddComponent<Outline>();
        qOutline.effectColor = new Color(0.82f, 0.68f, 0.36f, 0.90f);
        qOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject qHeadObj = new GameObject("QuestHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        qHeadObj.transform.SetParent(questRT, false);
        RectTransform qHeadRT = qHeadObj.GetComponent<RectTransform>();
        qHeadRT.anchorMin = new Vector2(0f, 1f);
        qHeadRT.anchorMax = new Vector2(1f, 1f);
        qHeadRT.pivot = new Vector2(0.5f, 1f);
        qHeadRT.anchoredPosition = new Vector2(0f, -4f);
        qHeadRT.sizeDelta = new Vector2(-14f, 14f);

        questHeaderText = qHeadObj.GetComponent<TextMeshProUGUI>();
        questHeaderText.fontSize = 9.0f;
        questHeaderText.fontStyle = FontStyles.Bold;
        questHeaderText.color = new Color(0.95f, 0.80f, 0.35f, 1f);
        questHeaderText.alignment = TextAlignmentOptions.Left;

        GameObject qDescObj = new GameObject("QuestDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
        qDescObj.transform.SetParent(questRT, false);
        RectTransform qDescRT = qDescObj.GetComponent<RectTransform>();
        qDescRT.anchorMin = Vector2.zero;
        qDescRT.anchorMax = Vector2.one;
        qDescRT.offsetMin = new Vector2(7f, 4f);
        qDescRT.offsetMax = new Vector2(-7f, -17f);

        questDescText = qDescObj.GetComponent<TextMeshProUGUI>();
        questDescText.fontSize = 9.5f;
        questDescText.color = Color.white;
        questDescText.alignment = TextAlignmentOptions.TopLeft;
        questDescText.textWrappingMode = TextWrappingModes.Normal;

        // Sağ Alt Araç / Maliyet Bilgi Çubuğu (Butonların üstüne binmeyecek şekilde ince şerit)
        toolBarObj = new GameObject("ToolCostTooltip_Bar", typeof(RectTransform), typeof(Image));
        toolBarObj.transform.SetParent(canvas.transform, false);
        RectTransform toolRT = toolBarObj.GetComponent<RectTransform>();
        toolRT.anchorMin = new Vector2(0.5f, 0.5f);
        toolRT.anchorMax = new Vector2(0.5f, 0.5f);
        toolRT.pivot = new Vector2(0.5f, 0.5f);
        toolRT.anchoredPosition = new Vector2(262f, -162f);
        toolRT.sizeDelta = new Vector2(246f, 17f);

        toolBarObj.GetComponent<Image>().color = new Color(0.12f, 0.11f, 0.10f, 0.84f);

        GameObject toolTxtObj = new GameObject("ToolInfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
        toolTxtObj.transform.SetParent(toolRT, false);
        RectTransform toolTxtRT = toolTxtObj.GetComponent<RectTransform>();
        toolTxtRT.anchorMin = Vector2.zero;
        toolTxtRT.anchorMax = Vector2.one;
        toolTxtRT.sizeDelta = new Vector2(-8f, 0f);

        toolInfoText = toolTxtObj.GetComponent<TextMeshProUGUI>();
        toolInfoText.fontSize = 8.5f;
        toolInfoText.color = new Color(0.95f, 0.92f, 0.82f, 1f);
        toolInfoText.alignment = TextAlignmentOptions.Center;
        toolInfoText.raycastTarget = false;
    }

    private Button CreateHudButton(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, out TextMeshProUGUI label)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.18f, 0.16f, 0.14f, 0.92f);

        Outline ol = btnObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.75f, 0.62f, 0.32f, 0.9f);
        ol.effectDistance = new Vector2(1f, -1f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(rt, false);
        RectTransform txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        label = txtObj.GetComponent<TextMeshProUGUI>();
        label.fontSize = 9.0f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.96f, 0.92f, 0.82f, 1f);
        label.alignment = TextAlignmentOptions.Center;

        return btnObj.GetComponent<Button>();
    }

    private void EnsureMainMenuCreated()
    {
        if (menuOverlay != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // TAM EKRAN PROFESYONEL ANA MENÜ ARKA PLANI
        menuOverlay = new GameObject("MainMenu_Overlay", typeof(RectTransform), typeof(Image));
        menuOverlay.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = menuOverlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        menuOverlay.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.06f, 0.96f);

        // Tam Ekran İç Çerçeve (Executive Frame)
        GameObject frameObj = new GameObject("FullScreenFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(overlayRT, false);
        RectTransform frameRT = frameObj.GetComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = new Vector2(22f, 18f);
        frameRT.offsetMax = new Vector2(-22f, -18f);
        frameObj.GetComponent<Image>().color = new Color(0.13f, 0.12f, 0.10f, 0.98f);

        Outline frameOutline = frameObj.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.82f, 0.66f, 0.32f, 1f);
        frameOutline.effectDistance = new Vector2(2f, -2f);

        // Üst Altın Dekoratif Şerit
        GameObject topStripe = new GameObject("TopGoldStripe", typeof(RectTransform), typeof(Image));
        topStripe.transform.SetParent(frameRT, false);
        RectTransform stripeRT = topStripe.GetComponent<RectTransform>();
        stripeRT.anchorMin = new Vector2(0f, 1f);
        stripeRT.anchorMax = new Vector2(1f, 1f);
        stripeRT.pivot = new Vector2(0.5f, 1f);
        stripeRT.anchoredPosition = Vector2.zero;
        stripeRT.sizeDelta = new Vector2(0f, 4f);
        topStripe.GetComponent<Image>().color = new Color(0.88f, 0.70f, 0.30f, 1f);

        // Üst Rozet (Badge)
        badgeText = CreateLabel(frameRT, "BadgeText", "", 9.5f, FontStyles.Bold, new Vector2(0f, -16f), new Vector2(680f, 16f), TextAlignmentOptions.Center, new Color(0.75f, 0.65f, 0.42f, 1f));

        // Ana Oyun Başlığı ("I NEED MORE PROFIT!")
        titleText = CreateLabel(frameRT, "GameTitle", "I NEED MORE PROFIT!", 30f, FontStyles.Bold, new Vector2(0f, -34f), new Vector2(700f, 38f), TextAlignmentOptions.Center, new Color(0.96f, 0.82f, 0.36f, 1f));

        // Alt Başlık
        subtitleText = CreateLabel(frameRT, "GameSubtitle", "", 11.5f, FontStyles.Italic, new Vector2(0f, -72f), new Vector2(700f, 20f), TextAlignmentOptions.Center, new Color(0.80f, 0.76f, 0.68f, 1f));

        // Ayırıcı Çizgi
        GameObject divLine = new GameObject("HeaderDivider", typeof(RectTransform), typeof(Image));
        divLine.transform.SetParent(frameRT, false);
        RectTransform divRT = divLine.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.5f, 1f);
        divRT.anchorMax = new Vector2(0.5f, 1f);
        divRT.pivot = new Vector2(0.5f, 1f);
        divRT.anchoredPosition = new Vector2(0f, -96f);
        divRT.sizeDelta = new Vector2(680f, 1.5f);
        divLine.GetComponent<Image>().color = new Color(0.82f, 0.66f, 0.32f, 0.45f);

        // SOL SÜTUN: Menü Butonları
        Vector2 leftColX = new Vector2(-175f, 0f);
        Button playBtn = CreateMenuButton(frameRT, "PlayBtn", leftColX + new Vector2(0f, -118f), new Vector2(270f, 34f), new Color(0.22f, 0.46f, 0.22f, 1f), out playBtnText);
        playBtn.onClick.AddListener(CloseMenu);

        Button howBtn = CreateMenuButton(frameRT, "HowToPlayBtn", leftColX + new Vector2(0f, -160f), new Vector2(270f, 32f), new Color(0.22f, 0.19f, 0.16f, 1f), out howToBtnText);
        howBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
        });

        Button langBtn = CreateMenuButton(frameRT, "LanguageBtn", leftColX + new Vector2(0f, -200f), new Vector2(270f, 32f), new Color(0.22f, 0.19f, 0.16f, 1f), out langBtnText);
        langBtn.onClick.AddListener(OnToggleLanguageClicked);

        Button sndBtn = CreateMenuButton(frameRT, "SoundBtn", leftColX + new Vector2(0f, -240f), new Vector2(270f, 32f), new Color(0.22f, 0.19f, 0.16f, 1f), out soundBtnText);
        sndBtn.onClick.AddListener(OnToggleSoundClicked);

        restartMenuBtn = CreateMenuButton(frameRT, "RestartBtn", leftColX + new Vector2(0f, -280f), new Vector2(270f, 32f), new Color(0.46f, 0.28f, 0.14f, 1f), out restartBtnText);
        restartMenuBtn.onClick.AddListener(OnRestartClicked);

        Button quitBtn = CreateMenuButton(frameRT, "QuitBtn", leftColX + new Vector2(0f, -320f), new Vector2(270f, 32f), new Color(0.48f, 0.16f, 0.14f, 1f), out quitBtnText);
        quitBtn.onClick.AddListener(OnQuitClicked);

        // SAĞ SÜTUN: Skor Geçmişi & Başkanın Özeti (Score History & Briefing)
        GameObject rightPanel = new GameObject("ScoreAndBriefing_Card", typeof(RectTransform), typeof(Image));
        rightPanel.transform.SetParent(frameRT, false);
        RectTransform rightRT = rightPanel.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.5f, 1f);
        rightRT.anchorMax = new Vector2(0.5f, 1f);
        rightRT.pivot = new Vector2(0.5f, 1f);
        rightRT.anchoredPosition = new Vector2(155f, -118f);
        rightRT.sizeDelta = new Vector2(340f, 234f);
        rightPanel.GetComponent<Image>().color = new Color(0.09f, 0.08f, 0.07f, 0.95f);

        Outline rightOl = rightPanel.AddComponent<Outline>();
        rightOl.effectColor = new Color(0.75f, 0.60f, 0.30f, 0.65f);
        rightOl.effectDistance = new Vector2(1.5f, -1.5f);

        scoreBoardTitleText = CreateLabel(rightRT, "ScoreTitle", "PROFIT RECORDS", 11.5f, FontStyles.Bold, new Vector2(0f, -10f), new Vector2(315f, 18f), TextAlignmentOptions.Left, new Color(0.95f, 0.80f, 0.36f, 1f));
        scoreBoardBodyText = CreateLabel(rightRT, "ScoreBody", "", 10.5f, FontStyles.Normal, new Vector2(0f, -32f), new Vector2(315f, 88f), TextAlignmentOptions.TopLeft, new Color(0.92f, 0.89f, 0.82f, 1f));

        GameObject innerLine = new GameObject("InnerDivider", typeof(RectTransform), typeof(Image));
        innerLine.transform.SetParent(rightRT, false);
        RectTransform inLineRT = innerLine.GetComponent<RectTransform>();
        inLineRT.anchorMin = new Vector2(0.5f, 1f);
        inLineRT.anchorMax = new Vector2(0.5f, 1f);
        inLineRT.pivot = new Vector2(0.5f, 1f);
        inLineRT.anchoredPosition = new Vector2(0f, -124f);
        inLineRT.sizeDelta = new Vector2(315f, 1f);
        innerLine.GetComponent<Image>().color = new Color(0.75f, 0.60f, 0.30f, 0.35f);

        briefingText = CreateLabel(rightRT, "BriefingText", "", 10.0f, FontStyles.Normal, new Vector2(0f, -132f), new Vector2(315f, 94f), TextAlignmentOptions.TopLeft, new Color(0.85f, 0.82f, 0.75f, 1f));

        // ESTETİK NASIL OYNANIR (HOW TO PLAY) PANELİ
        howToPlayPanel = new GameObject("HowToPlay_Panel", typeof(RectTransform), typeof(Image));
        howToPlayPanel.transform.SetParent(overlayRT, false);
        RectTransform howRT = howToPlayPanel.GetComponent<RectTransform>();
        howRT.anchorMin = new Vector2(0.5f, 0.5f);
        howRT.anchorMax = new Vector2(0.5f, 0.5f);
        howRT.pivot = new Vector2(0.5f, 0.5f);
        howRT.anchoredPosition = Vector2.zero;
        howRT.sizeDelta = new Vector2(560f, 360f);
        howToPlayPanel.GetComponent<Image>().color = new Color(0.12f, 0.11f, 0.09f, 0.99f);

        Outline howOl = howToPlayPanel.AddComponent<Outline>();
        howOl.effectColor = new Color(0.88f, 0.72f, 0.34f, 1f);
        howOl.effectDistance = new Vector2(2.5f, -2.5f);

        howToTitleText = CreateLabel(howRT, "HowToTitle", "HOW TO PLAY", 16f, FontStyles.Bold, new Vector2(0f, -14f), new Vector2(510f, 24f), TextAlignmentOptions.Center, new Color(0.96f, 0.82f, 0.36f, 1f));

        GameObject howLine = new GameObject("HowDivider", typeof(RectTransform), typeof(Image));
        howLine.transform.SetParent(howRT, false);
        RectTransform howLineRT = howLine.GetComponent<RectTransform>();
        howLineRT.anchorMin = new Vector2(0.5f, 1f);
        howLineRT.anchorMax = new Vector2(0.5f, 1f);
        howLineRT.pivot = new Vector2(0.5f, 1f);
        howLineRT.anchoredPosition = new Vector2(0f, -42f);
        howLineRT.sizeDelta = new Vector2(510f, 1.5f);
        howLine.GetComponent<Image>().color = new Color(0.85f, 0.70f, 0.32f, 0.5f);

        howToContentText = CreateLabel(howRT, "HowToContent", "", 11.2f, FontStyles.Normal, new Vector2(0f, -52f), new Vector2(510f, 250f), TextAlignmentOptions.TopLeft, new Color(0.92f, 0.89f, 0.82f, 1f));

        Button closeHowBtn = CreateMenuButton(howRT, "CloseHowBtn", new Vector2(0f, -314f), new Vector2(200f, 32f), new Color(0.24f, 0.46f, 0.22f, 1f), out closeHowBtnText);
        closeHowBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            howToPlayPanel.SetActive(false);
        });

        howToPlayPanel.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float fontSize, FontStyles style, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private Button CreateMenuButton(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color bgColor, out TextMeshProUGUI label)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Outline ol = btnObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.85f, 0.72f, 0.42f, 0.65f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(rt, false);
        RectTransform txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        label = txtObj.GetComponent<TextMeshProUGUI>();
        label.fontSize = 12f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.97f, 0.94f, 0.88f, 1f);
        label.alignment = TextAlignmentOptions.Center;

        return btnObj.GetComponent<Button>();
    }
}
