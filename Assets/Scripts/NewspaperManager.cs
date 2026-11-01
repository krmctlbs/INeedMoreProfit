using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class NewspaperManager : MonoBehaviour
{
    public static NewspaperManager Instance;

    [Header("Gazete Görsel & Font Ayarları (İsteğe Bağlı)")]
    public Sprite customNewspaperSprite;
    [Tooltip("Gazeteye özel TMP Font Asset sürükleyebilirsin (Boşsa GameManager'daki fontu kullanır)")]
    public TMP_FontAsset customNewspaperFont;

    private GameObject overlayRoot;
    private RectTransform paperRect;
    private TextMeshProUGUI mastheadText;
    private TextMeshProUGUI editionDateText;
    private TextMeshProUGUI headlineText;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI editorialTipText;
    private Button continueButton;
    private TextMeshProUGUI continueButtonText;
    private Button restartButton;
    private TextMeshProUGUI restartButtonText;

    // Dönüm Noktası (Checkpoint) Bayrakları
    private bool shownFirstFactory = false;
    private bool shownIndustrialBoom = false;
    private bool shownStage1Pollution = false;
    private bool shownStage2Pollution = false;
    private bool shownGhostTownEnding = false;

    private string activeHeadEN, activeHeadTR;
    private string activeBodyEN, activeBodyTR;
    private string activeTipEN, activeTipTR;
    private string activeEditionEN, activeEditionTR;
    private bool isEndingEdition = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    private void Start()
    {
        EnsureUICreated();
        LocalizationManager.OnLanguageChanged += RefreshLanguage;

        if (customNewspaperFont != null)
        {
            ApplyFont(customNewspaperFont);
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyFontsToAllUI();
        }
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLanguage;
    }

    public void ApplyFont(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null) return;
        EnsureUICreated();
        if (mastheadText != null) { mastheadText.font = fontAsset; mastheadText.wordSpacing = 22f; }
        if (headlineText != null) { headlineText.font = fontAsset; headlineText.wordSpacing = 22f; }
        if (continueButtonText != null) { continueButtonText.font = fontAsset; continueButtonText.wordSpacing = 20f; }
        if (restartButtonText != null) { restartButtonText.font = fontAsset; restartButtonText.wordSpacing = 20f; }
    }

    public void CheckMilestones()
    {
        if (GameManager.Instance == null) return;
        if (overlayRoot != null && overlayRoot.activeSelf) return;

        var gm = GameManager.Instance;
        int factories = gm.GetFactoryCount();

        // 1. İlk Fabrika Kurulduğunda
        if (!shownFirstFactory && factories >= 1)
        {
            shownFirstFactory = true;
            ShowNewspaper(
                "SPECIAL EDITION — VOL. I", "ÖZEL BASKI — SAYI I",
                "INDUSTRIAL BOOM BEGINS!",
                "SANAYİ DEVRİMİ BAŞLADI!",
                "Our town's very first factory has officially opened its doors! Smoke rises proudly from the chimneys as citizens celebrate new job opportunities and a bright economic future.",
                "Kasabamızın ilk fabrikası bugün coşkuyla açıldı! Bacalardan yükselen dumanlar, halk için yeni iş kapıları ve parlak bir ekonomik gelecek vaat ediyor.",
                "Editorial Tip: Build more Houses for workers and construct new Factories to multiply our city's profit!",
                "Editörün Notu: İşçiler için yeni Evler kurun ve şehir kârını katlamak için daha fazla Fabrika inşa edin!",
                false
            );
            return;
        }

        // 2. Sanayi Büyümesi (3 Fabrika veya 800$ Para)
        if (!shownIndustrialBoom && shownFirstFactory && (factories >= 3 || gm.money >= 800f) && gm.aqi < gm.stage1AqiThreshold)
        {
            shownIndustrialBoom = true;
            ShowNewspaper(
                "ECONOMY EDITION — VOL. II", "EKONOMİ BASKISI — SAYI II",
                "GOLDEN AGE OF PRODUCTION!",
                "ÜRETİMDE ALTIN ÇAĞ!",
                "City coffers are overflowing! Investors are praising the Mayor's relentless industrial expansion and demanding higher-level factories to maximize profits.",
                "Şehir kasası dolup taşıyor! Yatırımcılar Başkanın sanayi hamlesini alkışlıyor ve kârı zirveye taşımak için binaların yükseltilmesini bekliyor.",
                "Editorial Tip: Select the Upgrade tool (arrow icon) to level up your Houses and Factories to Level 2 & 3!",
                "Editörün Notu: Yükseltme (ok) aracını seçerek Ev ve Fabrikalarınızı Seviye 2 ve 3'e yükseltin!",
                false
            );
            return;
        }

        // 3. Kirlilik Evre 1 (AQI >= 100 - Ağaçlar Sarardığında)
        if (!shownStage1Pollution && gm.aqi >= gm.stage1AqiThreshold)
        {
            shownStage1Pollution = true;
            ShowNewspaper(
                "ENVIRONMENT REPORT — VOL. III", "ÇEVRE RAPORU — SAYI III",
                "WHY ARE THE TREES WITHERING?",
                "AĞAÇLAR NEDEN SARARIYOR?",
                "Scientists warn that the Air Quality Index (AQI) has surpassed 100, turning local forests yellow. City officials dismissed the panic: 'It's the natural price of progress—keep the factories running!'",
                "Uzmanlar Hava Kalitesi İndeksinin (AQI) 100'ü aşmasıyla ağaçların sarardığını belirterek uyardı. Yetkililer ise 'Büyümenin doğal bedeli, üretime tam gaz devam!' açıklamasını yaptı.",
                "Editorial Tip: Don't let environmental rumors slow you down—more factories mean bigger profits!",
                "Editörün Notu: Çevre söylentilerinin üretimi yavaşlatmasına izin vermeyin; daha çok fabrika, daha çok kazanç!",
                false
            );
            return;
        }

        // 4. Kirlilik Evre 2 (AQI >= 200 - Kahverengi Sis & Kuruyan Doğa)
        if (!shownStage2Pollution && gm.aqi >= gm.stage2AqiThreshold)
        {
            shownStage2Pollution = true;
            ShowNewspaper(
                "EMERGENCY ISSUE — VOL. IV", "ACİL DURUM BASKISI — SAYI IV",
                "THE SKY HAS VANISHED!",
                "GÖKYÜZÜ KAYBOLDU!",
                "A suffocating blanket of thick brown smog has swallowed the city as AQI exceeds 200. Trees have completely died and hospitals are overwhelmed with coughing citizens preparing to flee.",
                "AQI 200 sınırını aşarken boğucu kahverengi bir sis şehri yuttu! Ağaçlar tamamen kururken, nefes alamayan vatandaşlar şehri terk etmeye hazırlanıyor.",
                "Editorial Warning: Happiness is plummeting! If citizens leave the city, who will run the factories?",
                "Editörün Uyarısı: Mutluluk hızla çöküyor! Halk şehri terk ederse bu devasa fabrikaları kim çalıştıracak?",
                false
            );
            return;
        }

        // 5. Çöküş / Oyun Sonu (Nüfus <= 0): Önce Danışman ortaya gelip dramatik konuşmasını yapar, ardından bu gazete açılır!
        if (!shownGhostTownEnding && shownFirstFactory && gm.population <= 0.1f)
        {
            shownGhostTownEnding = true;
            if (AdvisorManager.Instance != null && !AdvisorManager.Instance.HasTriggeredEnding)
            {
                AdvisorManager.Instance.TriggerDramaticEndingSequence();
            }
            else
            {
                ShowFinalGhostTownNewspaper();
            }
        }
    }

    public void ShowFinalGhostTownNewspaper()
    {
        if (GameManager.Instance == null) return;
        var gm = GameManager.Instance;
        gm.SaveRunScoreIfNeeded();

        int peakProfit = Mathf.FloorToInt(Mathf.Max(gm.peakMoney, gm.money));
        int finalAqi = Mathf.FloorToInt(gm.aqi);
        int totalSecs = Mathf.FloorToInt(gm.elapsedPlayTime);
        int mins = totalSecs / 60;
        int secs = totalSecs % 60;
        int bestEver = PlayerPrefs.GetInt("BestPeakProfit", peakProfit);

        ShowNewspaper(
            "FINAL EDITION — ISSUE #0", "SON BASKI — SAYI #0",
            "GHOST TOWN: NO ONE IS LEFT!",
            "HAYALET ŞEHİR: KİMSE KALMADI!",
            "Driven away by lethal pollution and a ruined ecosystem, every single citizen—and even your loyal Advisor—has abandoned the city. Your factories stand silent in a lifeless wasteland. You chased endless profit, but forgot that you cannot breathe money.",
            "Zehirli havaya ve yok edilen doğaya dayanamayan tüm vatandaşlar —ve hatta sadık Danışmanın bile— şehri terk etti. Fabrikaların artık cansız bir çölün ortasında sessizce duruyor. Sınırsız kâr peşinde koştun ama paranın solunamayacağını unuttun.",
            $"RUN SUMMARY — Max Profit: ${peakProfit} (Best: ${bestEver}) | Final AQI: {finalAqi} | Time: {mins:00}:{secs:00}",
            $"OYUN ÖZETİ — Maks. Kâr: ${peakProfit} (Rekor: ${bestEver}) | Son AQI: {finalAqi} | Süre: {mins:00}:{secs:00}",
            true
        );
    }

    public void ShowNewspaper(
        string editionEN, string editionTR,
        string headEN, string headTR,
        string bodyTextEN, string bodyTextTR,
        string tipEN, string tipTR,
        bool isEnding = false)
    {
        EnsureUICreated();

        activeEditionEN = editionEN;
        activeEditionTR = editionTR;
        activeHeadEN = headEN;
        activeHeadTR = headTR;
        activeBodyEN = bodyTextEN;
        activeBodyTR = bodyTextTR;
        activeTipEN = tipEN;
        activeTipTR = tipTR;
        isEndingEdition = isEnding;

        RefreshLanguage();

        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();

        if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
        AudioManager.Instance?.PlayNewspaper();

        StopAllCoroutines();
        StartCoroutine(AnimateNewspaperSpinIn());
    }

    private IEnumerator AnimateNewspaperSpinIn()
    {
        float duration = 0.48f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            paperRect.localScale = Vector3.one * Mathf.Lerp(0.15f, 1.0f, ease);
            float angle = Mathf.Lerp(-540f, -1.2f, ease);
            paperRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        paperRect.localScale = Vector3.one;
        paperRect.localRotation = Quaternion.Euler(0f, 0f, -1.0f);
    }

    public void CloseNewspaper()
    {
        AudioManager.Instance?.PlayClick();
        if (overlayRoot != null) overlayRoot.SetActive(false);

        if (isEndingEdition && QuestAndMenuManager.Instance != null)
        {
            // Oyun sonu gazetesinde "Ana Menü"ye dön
            QuestAndMenuManager.Instance.OpenMenu(true);
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPaused(false);
        }
    }

    public void RestartGame()
    {
        AudioManager.Instance?.PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RefreshLanguage()
    {
        if (mastheadText != null)
            mastheadText.text = LocalizationManager.Get("THE DAILY INDUSTRIAL", "SANAYİ POSTASI");
        if (editionDateText != null)
            editionDateText.text = LocalizationManager.Get(activeEditionEN, activeEditionTR);
        if (headlineText != null)
            headlineText.text = LocalizationManager.Get(activeHeadEN, activeHeadTR);
        if (bodyText != null)
            bodyText.text = LocalizationManager.Get(activeBodyEN, activeBodyTR);
        if (editorialTipText != null)
            editorialTipText.text = LocalizationManager.Get(activeTipEN, activeTipTR);

        if (continueButton != null)
        {
            continueButton.GetComponent<RectTransform>().anchoredPosition = isEndingEdition ? new Vector2(105f, 24f) : new Vector2(0f, 24f);
        }

        if (continueButtonText != null)
        {
            continueButtonText.text = isEndingEdition
                ? LocalizationManager.Get("MAIN MENU", "ANA MENÜ")
                : LocalizationManager.Get("CONTINUE", "DEVAM ET");
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(isEndingEdition);
            restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-105f, 24f);
            if (restartButtonText != null)
                restartButtonText.text = LocalizationManager.Get("PLAY AGAIN", "TEKRAR OYNA");
        }
    }

    private void EnsureUICreated()
    {
        if (overlayRoot != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Karartma Arka Planı
        overlayRoot = new GameObject("NewspaperModal_Overlay", typeof(RectTransform), typeof(Image));
        overlayRoot.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = overlayRoot.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        Image overlayImg = overlayRoot.GetComponent<Image>();
        overlayImg.color = new Color(0.05f, 0.04f, 0.03f, 0.78f);

        // Gazete Sayfası (800x600 referans çözünürlüğüne uyumlu kompakt boyut)
        GameObject paperObj = new GameObject("NewspaperPage", typeof(RectTransform), typeof(Image));
        paperObj.transform.SetParent(overlayRT, false);
        paperRect = paperObj.GetComponent<RectTransform>();
        paperRect.anchorMin = new Vector2(0.5f, 0.5f);
        paperRect.anchorMax = new Vector2(0.5f, 0.5f);
        paperRect.pivot = new Vector2(0.5f, 0.5f);
        paperRect.anchoredPosition = Vector2.zero;
        paperRect.sizeDelta = new Vector2(485f, 365f);

        Image paperImg = paperObj.GetComponent<Image>();
        if (customNewspaperSprite != null)
        {
            paperImg.sprite = customNewspaperSprite;
            paperImg.color = Color.white;
        }
        else
        {
            paperImg.color = new Color(0.93f, 0.89f, 0.79f, 1f);
        }

        Outline paperOutline = paperObj.AddComponent<Outline>();
        paperOutline.effectColor = new Color(0.18f, 0.15f, 0.12f, 1f);
        paperOutline.effectDistance = new Vector2(3.5f, -3.5f);

        // Gazete Başlığı (Masthead)
        mastheadText = CreateText(paperRect, "Masthead", "THE DAILY INDUSTRIAL", 24f, FontStyles.Bold, new Vector2(0f, -14f), new Vector2(440f, 32f), TextAlignmentOptions.Center);
        mastheadText.color = new Color(0.12f, 0.10f, 0.09f, 1f);

        // Üst Çizgi
        CreateLine(paperRect, new Vector2(0f, -48f), 440f, 2.5f);
        editionDateText = CreateText(paperRect, "EditionInfo", "SPECIAL EDITION", 10.5f, FontStyles.Italic, new Vector2(0f, -53f), new Vector2(440f, 18f), TextAlignmentOptions.Center);
        editionDateText.color = new Color(0.32f, 0.28f, 0.22f, 1f);
        CreateLine(paperRect, new Vector2(0f, -72f), 440f, 1.2f);

        // Ana Manşet (Headline)
        headlineText = CreateText(paperRect, "Headline", "HEADLINE", 19.5f, FontStyles.Bold, new Vector2(0f, -84f), new Vector2(435f, 52f), TextAlignmentOptions.Center);
        headlineText.color = new Color(0.58f, 0.12f, 0.08f, 1f);

        // Haber Gövdesi (Body - Sol hizalı ve temiz okunaklı)
        bodyText = CreateText(paperRect, "ArticleBody", "Body text...", 13.5f, FontStyles.Normal, new Vector2(0f, -142f), new Vector2(425f, 105f), TextAlignmentOptions.TopLeft);
        bodyText.color = new Color(0.16f, 0.14f, 0.12f, 1f);

        // Editör Notu / Rapor Kutusu
        GameObject tipBox = new GameObject("EditorialBox", typeof(RectTransform), typeof(Image));
        tipBox.transform.SetParent(paperRect, false);
        RectTransform tipBoxRT = tipBox.GetComponent<RectTransform>();
        tipBoxRT.anchorMin = new Vector2(0.5f, 1f);
        tipBoxRT.anchorMax = new Vector2(0.5f, 1f);
        tipBoxRT.pivot = new Vector2(0.5f, 1f);
        tipBoxRT.anchoredPosition = new Vector2(0f, -256f);
        tipBoxRT.sizeDelta = new Vector2(430f, 46f);
        tipBox.GetComponent<Image>().color = new Color(0.85f, 0.80f, 0.68f, 1f);

        editorialTipText = CreateText(tipBoxRT, "EditorialTipText", "Tip...", 11.2f, FontStyles.Bold, new Vector2(0f, -5f), new Vector2(410f, 36f), TextAlignmentOptions.Center);
        editorialTipText.color = new Color(0.22f, 0.18f, 0.12f, 1f);

        // Devam Et Butonu
        continueButton = CreatePaperButton(paperRect, "ContinueBtn", new Vector2(0f, 24f), new Color(0.20f, 0.17f, 0.14f, 1f), out continueButtonText);
        continueButton.onClick.AddListener(CloseNewspaper);

        // Tekrar Oyna Butonu
        restartButton = CreatePaperButton(paperRect, "RestartBtn", new Vector2(-105f, 24f), new Color(0.60f, 0.16f, 0.12f, 1f), out restartButtonText);
        restartButton.onClick.AddListener(RestartGame);
        restartButton.gameObject.SetActive(false);

        overlayRoot.SetActive(false);
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string content, float fontSize, FontStyles style, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align)
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
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private void CreateLine(RectTransform parent, Vector2 anchoredPos, float width, float height)
    {
        GameObject line = new GameObject("DividerLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(width, height);
        line.GetComponent<Image>().color = new Color(0.18f, 0.15f, 0.12f, 0.9f);
    }

    private Button CreatePaperButton(RectTransform parent, string name, Vector2 anchoredPos, Color bgColor, out TextMeshProUGUI btnTmp)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(165f, 32f);

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(rt, false);
        RectTransform txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        btnTmp = txtObj.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "CONTINUE";
        btnTmp.fontSize = 12.5f;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = new Color(0.96f, 0.93f, 0.85f, 1f);
        btnTmp.alignment = TextAlignmentOptions.Center;

        return btnObj.GetComponent<Button>();
    }
}
