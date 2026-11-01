using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdvisorManager : MonoBehaviour
{
    public static AdvisorManager Instance;

    [Header("Karakter Portre Sprite'ları (Varsayılan: Seviye 2 Takım Elbiseli)")]
    public Sprite normalSprite;
    public Sprite richSuitSprite;
    public Sprite dirtySadSprite;

    [Header("UI Referansları (Boşsa Otomatik Oluşturulur)")]
    public RectTransform advisorRoot;
    public Image portraitImage;
    public Image portraitFrameImage;
    public GameObject speechBubbleObj;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Daktilo (Typewriter) Ayarları")]
    public float charDelay = 0.032f;
    public float autoHideDelay = 6.5f;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isEndingSequence = false;
    private bool endingStepAdvanceRequested = false;

    private string currentFullText = "";
    private string currentTextEN = "";
    private string currentTextTR = "";
    private Vector2 basePortraitAnchoredPos;
    private GameObject endingBackdropObj;

    // Koşul & Tetikleyici Durumları
    private float gameActiveTimer = 0f;
    private float lastNoFactoryReminderTime = 0f;
    private float lastWorkerShortageReminderTime = -30f;
    private float previousPopulation = 20f;

    private bool welcomedPlayer = false;
    private bool everBuiltFactory = false;
    private bool praisedFirstFactory = false;
    private bool suggestedUpgrade = false;
    private bool warnedStage1Pollution = false;
    private bool warnedStage2Pollution = false;
    private bool warnedPopulationDrop = false;
    private bool announcedGhostTown = false;

    private Queue<(string en, string tr)> messageQueue = new Queue<(string en, string tr)>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        AutoLoadSpritesIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoLoadSpritesIfNeeded();
    }
#endif

    private void AutoLoadSpritesIfNeeded()
    {
        // 1. Önce Resources klasöründen yükle (Hem Unity Editor'de hem de Windows .exe Build'de %100 çalışır!)
        if (richSuitSprite == null)
            richSuitSprite = LoadResourceSprite("Advisor/Advisor_Level2");
        if (normalSprite == null)
            normalSprite = richSuitSprite != null ? richSuitSprite : LoadResourceSprite("Advisor/Advisor_Level1");
        if (dirtySadSprite == null)
            dirtySadSprite = LoadResourceSprite("Advisor/Advisor_Level3");

#if UNITY_EDITOR
        // 2. Editor içi yedek kontrol
        if (richSuitSprite == null)
            richSuitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PNGs/İnsnalar/Mavi Adam - Seviye 2Asset 7.png");
        if (normalSprite == null)
            normalSprite = richSuitSprite;
        if (dirtySadSprite == null)
            dirtySadSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PNGs/İnsnalar/Mavi Adam - Seviye 3Asset 1.png");
#endif
    }

    private Sprite LoadResourceSprite(string resPath)
    {
        Sprite sp = Resources.Load<Sprite>(resPath);
        if (sp != null) return sp;

        Texture2D tex = Resources.Load<Texture2D>(resPath);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return null;
    }

    private void Start()
    {
        EnsureUICreated();
        LocalizationManager.OnLanguageChanged += RefreshActiveLanguageText;
        if (GameManager.Instance != null)
        {
            previousPopulation = GameManager.Instance.population;
            GameManager.Instance.ApplyFontsToAllUI();
        }
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshActiveLanguageText;
    }

    public void SetHUDVisible(bool visible)
    {
        if (advisorRoot != null && !isEndingSequence)
        {
            advisorRoot.gameObject.SetActive(visible);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // Konuşurken karakter portresini zıplat — ANCAK oyun sonu dramatik konuşmasında (isEndingSequence) KESİNLİKLE zıplamasın!
        if (portraitImage != null)
        {
            RectTransform rt = portraitImage.rectTransform;
            if (isTyping && !isEndingSequence)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 16f)) * 3.5f;
                rt.anchoredPosition = basePortraitAnchoredPos + new Vector2(0f, bob);
            }
            else
            {
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, basePortraitAnchoredPos, Time.unscaledDeltaTime * 12f);
            }
        }

        UpdatePortraitState();

        if (isEndingSequence) return;
        if (GameManager.Instance.isGamePaused) return;

        gameActiveTimer += Time.deltaTime;
        EvaluateConditions();

        if (!isTyping && (speechBubbleObj == null || !speechBubbleObj.activeSelf) && messageQueue.Count > 0)
        {
            var next = messageQueue.Dequeue();
            StartDialogue(next.en, next.tr);
        }
    }

    private void UpdatePortraitState()
    {
        if (portraitImage == null) return;

        float aqi = GameManager.Instance.aqi;
        float happiness = GameManager.Instance.happiness;

        // Kirlilik yükseldiğinde veya oyun sonunda Seviye 3 (Kirli/Üzgün), aksi halde her zaman Seviye 2 (Takım Elbiseli Mavi Adam)
        if (isEndingSequence || aqi >= 175f || happiness < 32f || GameManager.Instance.population <= 1f)
        {
            if (dirtySadSprite != null) portraitImage.sprite = dirtySadSprite;
        }
        else
        {
            if (richSuitSprite != null) portraitImage.sprite = richSuitSprite;
            else if (normalSprite != null) portraitImage.sprite = normalSprite;
        }
    }

    private void EvaluateConditions()
    {
        var gm = GameManager.Instance;
        int factoryCount = gm.GetFactoryCount();
        bool anyUpgraded = gm.HasAnyUpgradedBuilding();

        if (factoryCount > 0) everBuiltFactory = true;

        // 1. Oyun başı karşılama (1.5 saniye sonra, daraltmayan kısa hoş geldin mesajı)
        if (!welcomedPlayer && gameActiveTimer >= 1.5f)
        {
            welcomedPlayer = true;
            lastNoFactoryReminderTime = 0f; // İlk kontrol 30. saniyede yapılacak
            ShowCustomDialogue(
                "Welcome, Mr. Mayor! Everything is ready for your leadership. Let's turn this peaceful town into a profitable empire!",
                "Hoş geldin Başkan! Şehir liderliğine hazır. Bu sakin kasabayı bol kazançlı bir ticaret merkezine dönüştürelim!",
                true
            );
            return;
        }

        // 2. Fabrika kontrolü: İlk kez 30. saniyede kontrol et, sonrasında sadece 45 saniyede bir hiç fabrika yoksa uyar!
        if (welcomedPlayer && factoryCount == 0 && gameActiveTimer >= 30f && (lastNoFactoryReminderTime <= 0.01f || (gameActiveTimer - lastNoFactoryReminderTime) >= 45f))
        {
            lastNoFactoryReminderTime = gameActiveTimer;
            if (everBuiltFactory)
            {
                // Oyuncu fabrikaları yıktıysa
                ShowCustomDialogue(
                    "Mr. Mayor, we have no active factories left! We need income to keep the city running—let's build a Factory.",
                    "Başkan, şehirde hiç aktif fabrika kalmadı! Gelire ihtiyacımız var, sağ alttan bir Fabrika kuralım.",
                    false
                );
            }
            else
            {
                // 30. saniyede hâlâ hiç fabrika kurmadıysa
                ShowCustomDialogue(
                    "Mr. Mayor, we need income! Select the Factory icon at the bottom-right ($100) and place it on the grid.",
                    "Başkan, gelire ihtiyacımız var! Sağ alttan Fabrika ikonunu seçip (100$) haritaya yerleştirelim.",
                    false
                );
            }
            return;
        }

        // 3. İlk fabrika kurulduğunda tebrik et
        if (!praisedFirstFactory && factoryCount >= 1)
        {
            praisedFirstFactory = true;
            ShowCustomDialogue(
                "Splendid! Our first Factory is running! Watch the profits grow—build Houses when our factories need more workers.",
                "Harika! İlk fabrikamız üretime başladı! Kazancımız artıyor; işçi gerektikçe yeni Evler inşa edebiliriz.",
                false
            );
        }

        // 4. İşçi açığı varsa (45 saniyede bir nazikçe hatırlat)
        if (factoryCount > 0 && gm.totalWorkersNeeded > gm.population + 0.5f && (gameActiveTimer - lastWorkerShortageReminderTime) >= 45f)
        {
            lastWorkerShortageReminderTime = gameActiveTimer;
            ShowCustomDialogue(
                "Our factories need more workers to run at full capacity! Build a House ($50) or upgrade existing Houses.",
                "Fabrikalarımız tam kapasite çalışmak için işçiye ihtiyaç duyuyor! Yeni Ev kurmalı veya evleri yükseltmeliyiz.",
                false
            );
        }

        // 5. Para biriktiyse ve henüz hiç bina yükseltilmediyse Upgrade öner
        if (!suggestedUpgrade && factoryCount >= 1 && gm.money >= 180f && !anyUpgraded && gameActiveTimer >= 40f)
        {
            suggestedUpgrade = true;
            ShowCustomDialogue(
                "We have good capital! Use the Upgrade tool (up-arrow button) on Houses or Factories to multiply our profits.",
                "Kasamızda güzel sermaye birikti! Sağ alttaki Yükseltme (ok) butonuyla binaları geliştirip kârımızı katlayalım.",
                false
            );
        }

        // 6. Kirlilik Evre 1 (AQI >= 100)
        if (!warnedStage1Pollution && gm.aqi >= gm.stage1AqiThreshold)
        {
            warnedStage1Pollution = true;
            ShowCustomDialogue(
                "Hmm, the leaves are turning yellow and AQI passed 100... Don't mind that, look at our revenue! Keep expanding!",
                "Hmm, yapraklar sararmaya başladı ve AQI 100'ü geçti... Boş ver Başkan, kasaya akan paraya bak! Büyümeye devam!",
                true
            );
        }

        // 7. Kirlilik Evre 2 (AQI >= 200)
        if (!warnedStage2Pollution && gm.aqi >= gm.stage2AqiThreshold)
        {
            warnedStage2Pollution = true;
            ShowCustomDialogue(
                "*Cough, cough*... The air feels heavy and brown today, doesn't it? Still, profits are at an all-time high!",
                "*Öhö öhö*... Hava bugün oldukça ağırlaştı sanki, değil mi? Olsun, kârımız rekor kırıyor!",
                true
            );
        }

        // 8. Nüfus düşmeye başladığında (Mutluluk < 35 ve nüfus azalıyor)
        if (!warnedPopulationDrop && gm.happiness < 35f && gm.population < previousPopulation - 0.1f && gm.population > 1f)
        {
            warnedPopulationDrop = true;
            ShowCustomDialogue(
                "Mr. Mayor! Citizens can't breathe in this toxic smog anymore—they are packing up and leaving the city!",
                "Başkan! Halk bu kirli havada artık nefes alamıyor, bavullarını toplayıp şehri terk etmeye başladılar!",
                true
            );
        }

        // 9. Şehir tamamen boşaldığında (Nüfus <= 0) -> Ortaya gelip dramatik COP31 konuşması yap!
        if (!announcedGhostTown && praisedFirstFactory && gm.population <= 0.1f)
        {
            announcedGhostTown = true;
            TriggerDramaticEndingSequence();
        }

        previousPopulation = gm.population;
    }

    public bool HasTriggeredEnding => announcedGhostTown;

    public void TriggerDramaticEndingSequence()
    {
        if (isEndingSequence) return;
        announcedGhostTown = true;
        isEndingSequence = true;

        EnsureUICreated();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        messageQueue.Clear();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPaused(true);
            GameManager.Instance.SaveRunScoreIfNeeded();
        }

        StartCoroutine(DramaticEndingCoroutine());
    }

    private IEnumerator DramaticEndingCoroutine()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null && endingBackdropObj == null)
        {
            endingBackdropObj = new GameObject("EndingAdvisor_Backdrop", typeof(RectTransform), typeof(Image));
            endingBackdropObj.transform.SetParent(canvas.transform, false);
            RectTransform bdRT = endingBackdropObj.GetComponent<RectTransform>();
            bdRT.anchorMin = Vector2.zero;
            bdRT.anchorMax = Vector2.one;
            bdRT.sizeDelta = Vector2.zero;
            endingBackdropObj.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.02f, 0f);
        }

        if (endingBackdropObj != null)
        {
            endingBackdropObj.SetActive(true);
            endingBackdropObj.transform.SetAsLastSibling();
        }

        advisorRoot.gameObject.SetActive(true);
        advisorRoot.SetAsLastSibling();

        if (dirtySadSprite != null && portraitImage != null)
        {
            portraitImage.sprite = dirtySadSprite;
        }

        // Karakteri sol alttan ekranın tam ortasına yumuşakça taşı
        Vector2 startAnchorMin = advisorRoot.anchorMin;
        Vector2 startAnchorMax = advisorRoot.anchorMax;
        Vector2 startPivot = advisorRoot.pivot;
        Vector2 startPos = advisorRoot.anchoredPosition;

        advisorRoot.anchorMin = new Vector2(0.5f, 0.5f);
        advisorRoot.anchorMax = new Vector2(0.5f, 0.5f);
        advisorRoot.pivot = new Vector2(0.5f, 0.5f);
        advisorRoot.sizeDelta = new Vector2(440f, 110f);

        RectTransform bubbleRT = speechBubbleObj.GetComponent<RectTransform>();
        bubbleRT.anchoredPosition = new Vector2(82f, 4f);
        bubbleRT.sizeDelta = new Vector2(350f, 102f);

        Vector2 fromCenterPos = new Vector2(-180f, -180f);
        Vector2 toCenterPos = new Vector2(0f, 10f);
        advisorRoot.anchoredPosition = fromCenterPos;

        Image bdImg = endingBackdropObj != null ? endingBackdropObj.GetComponent<Image>() : null;
        float moveElapsed = 0f;
        float moveDur = 0.75f;
        while (moveElapsed < moveDur)
        {
            moveElapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, moveElapsed / moveDur);
            advisorRoot.anchoredPosition = Vector2.Lerp(fromCenterPos, toCenterPos, t);
            advisorRoot.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.12f, 1.12f, 1f), t);
            if (bdImg != null) bdImg.color = new Color(0.04f, 0.03f, 0.02f, Mathf.Lerp(0f, 0.78f, t));
            yield return null;
        }

        // COP31 Jürisi için 3 aşamalı duygusal ve çarpıcı kapanış monoloğu (Zıplamadan, ağır ve ciddi tonla)
        (string en, string tr)[] endingLines = new (string en, string tr)[]
        {
            (
                "Look around you, Mr. Mayor... Everyone is gone. Not a single citizen is left to breathe under this poisoned sky and live with this corrupted climate. [Click]",
                "Etrafına bir bak Başkan... Herkes gitti. Bu zehirli gökyüzünün altında nefes alacak ve mahvolmuş iklime katlanacak tek bir insan bile kalmadı. [Tıkla]"
            ),
            (
                "We had a living forest, clean air, and a peaceful home. You didn't take care of nature nor the climate, you only chased endless profit while the trees died and the earth withered. [Click]",
                "Yemyeşil bir ormanımız, tertemiz havamız ve huzurlu bir yuvamız vardı. Sen ne doğayı ne de iklimi korudun; ağaçlar kururken sadece daha fazla kârın peşinden koştun. [Tıkla]"
            ),
            (
                "You cannot eat money, and no city can survive when its environment is destroyed. I won't stay in these ashes either... I am leaving too. Goodbye.",
                "Parayı yiyemezsin Başkan; çevreyi önemsemeden kurulan hiçbir gelecek ayakta kalamaz. Ben de artık bu küllerin içinde kalamam... Ben de gidiyorum. Hoşça kal."
            )
        };

        if (speakerNameText != null)
        {
            speakerNameText.text = LocalizationManager.Get("THE LAST CITIZEN (ADVISOR)", "SON VATANDAŞ (DANIŞMAN)");
        }
        speechBubbleObj.SetActive(true);

        for (int lineIdx = 0; lineIdx < endingLines.Length; lineIdx++)
        {
            currentTextEN = endingLines[lineIdx].en;
            currentTextTR = endingLines[lineIdx].tr;
            currentFullText = LocalizationManager.Get(currentTextEN, currentTextTR);

            isTyping = true;
            endingStepAdvanceRequested = false;
            dialogueText.text = "";

            int charCounter = 0;
            for (int i = 0; i < currentFullText.Length; i++)
            {
                if (endingStepAdvanceRequested)
                {
                    dialogueText.text = currentFullText;
                    break;
                }

                dialogueText.text += currentFullText[i];
                char c = currentFullText[i];
                if (!char.IsWhiteSpace(c))
                {
                    charCounter++;
                    if (charCounter % 3 == 1)
                    {
                        // Daha ağır, hüzünlü ses tonu
                        AudioManager.Instance?.PlayVoiceBlip(0.74f, 0.84f);
                    }
                }

                float wait = 0.038f;
                if (c == '.' || c == '!' || c == '?') wait = 0.18f;
                else if (c == ',') wait = 0.09f;
                yield return new WaitForSecondsRealtime(wait);
            }

            isTyping = false;
            endingStepAdvanceRequested = false;

            // Oyuncu tıklayana kadar veya 5.5 saniye boyunca bekle
            float waitTimer = 0f;
            while (waitTimer < 5.5f && !endingStepAdvanceRequested)
            {
                waitTimer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // Danışman da şehri terk eder (Portre yavaşça silinir)
        float fadeDur = 1.0f;
        float fadeElapsed = 0f;
        Color pCol = portraitImage != null ? portraitImage.color : Color.white;
        while (fadeElapsed < fadeDur)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(fadeElapsed / fadeDur);
            if (portraitImage != null) portraitImage.color = new Color(pCol.r, pCol.g, pCol.b, alpha);
            yield return null;
        }

        speechBubbleObj.SetActive(false);
        advisorRoot.gameObject.SetActive(false);
        if (endingBackdropObj != null) endingBackdropObj.SetActive(false);

        // Şimdi Final Gazetesi ve Skor Özeti ekrana gelsin
        NewspaperManager.Instance?.ShowFinalGhostTownNewspaper();
    }

    public void ShowCustomDialogue(string textEN, string textTR, bool immediate = false)
    {
        if (isEndingSequence) return;
        EnsureUICreated();
        if (immediate)
        {
            StartDialogue(textEN, textTR);
        }
        else
        {
            if (isTyping || (speechBubbleObj != null && speechBubbleObj.activeSelf))
            {
                messageQueue.Enqueue((textEN, textTR));
            }
            else
            {
                StartDialogue(textEN, textTR);
            }
        }
    }

    private void StartDialogue(string textEN, string textTR)
    {
        currentTextEN = textEN;
        currentTextTR = textTR;
        currentFullText = LocalizationManager.Get(textEN, textTR);

        if (speakerNameText != null)
        {
            speakerNameText.text = LocalizationManager.Get("CITY ADVISOR", "ŞEHİR DANIŞMANI");
        }

        if (speechBubbleObj != null) speechBubbleObj.SetActive(true);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypewriterRoutine(currentFullText));
    }

    private IEnumerator TypewriterRoutine(string textToType)
    {
        isTyping = true;
        dialogueText.text = "";

        int charCounter = 0;
        for (int i = 0; i < textToType.Length; i++)
        {
            dialogueText.text += textToType[i];
            char c = textToType[i];

            if (!char.IsWhiteSpace(c))
            {
                charCounter++;
                if (charCounter % 3 == 1)
                {
                    AudioManager.Instance?.PlayVoiceBlip(0.92f, 1.10f);
                }
            }

            float wait = charDelay;
            if (c == '.' || c == '!' || c == '?') wait = charDelay * 4.5f;
            else if (c == ',') wait = charDelay * 2.2f;

            yield return new WaitForSecondsRealtime(wait);
        }

        isTyping = false;
        yield return new WaitForSecondsRealtime(autoHideDelay);

        if (speechBubbleObj != null && !isEndingSequence)
        {
            speechBubbleObj.SetActive(false);
        }
    }

    public void OnBubbleClicked()
    {
        if (isEndingSequence)
        {
            endingStepAdvanceRequested = true;
            return;
        }

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            isTyping = false;
            if (dialogueText != null) dialogueText.text = currentFullText;
            typingCoroutine = StartCoroutine(CloseBubbleAfterDelay(3.5f));
        }
        else
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (speechBubbleObj != null) speechBubbleObj.SetActive(false);
        }
    }

    private IEnumerator CloseBubbleAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (speechBubbleObj != null && !isEndingSequence) speechBubbleObj.SetActive(false);
    }

    private void RefreshActiveLanguageText()
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = isEndingSequence
                ? LocalizationManager.Get("THE LAST CITIZEN (ADVISOR)", "SON VATANDAŞ (DANIŞMAN)")
                : LocalizationManager.Get("CITY ADVISOR", "ŞEHİR DANIŞMANI");
        }
        if (!string.IsNullOrEmpty(currentTextEN) && dialogueText != null)
        {
            currentFullText = LocalizationManager.Get(currentTextEN, currentTextTR);
            if (!isTyping)
            {
                dialogueText.text = currentFullText;
            }
        }
    }

    private void EnsureUICreated()
    {
        if (advisorRoot != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Kompakt Sol Alt Konteyner (800x600 CanvasScaler ile tam uyumlu, ekranı kaplamaz)
        GameObject rootObj = new GameObject("AdvisorUI_Container", typeof(RectTransform));
        rootObj.transform.SetParent(canvas.transform, false);
        advisorRoot = rootObj.GetComponent<RectTransform>();
        advisorRoot.anchorMin = new Vector2(0f, 0f);
        advisorRoot.anchorMax = new Vector2(0f, 0f);
        advisorRoot.pivot = new Vector2(0f, 0f);
        advisorRoot.anchoredPosition = new Vector2(10f, 10f);
        advisorRoot.sizeDelta = new Vector2(335f, 84f);

        // Karakter Portre Çerçevesi (Kompakt)
        GameObject frameObj = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(advisorRoot, false);
        RectTransform frameRT = frameObj.GetComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0f, 0f);
        frameRT.anchorMax = new Vector2(0f, 0f);
        frameRT.pivot = new Vector2(0f, 0f);
        frameRT.anchoredPosition = Vector2.zero;
        frameRT.sizeDelta = new Vector2(64f, 78f);
        portraitFrameImage = frameObj.GetComponent<Image>();
        portraitFrameImage.color = new Color(0.14f, 0.12f, 0.10f, 0.94f);

        Outline frameOutline = frameObj.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.82f, 0.68f, 0.36f, 1f);
        frameOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Karakter Sprite (Seviye 2 Mavi Adam varsayılan)
        GameObject portObj = new GameObject("CharacterPortrait", typeof(RectTransform), typeof(Image));
        portObj.transform.SetParent(frameRT, false);
        RectTransform portRT = portObj.GetComponent<RectTransform>();
        portRT.anchorMin = new Vector2(0.5f, 0f);
        portRT.anchorMax = new Vector2(0.5f, 0f);
        portRT.pivot = new Vector2(0.5f, 0f);
        portRT.anchoredPosition = new Vector2(0f, 4f);
        portRT.sizeDelta = new Vector2(54f, 68f);
        basePortraitAnchoredPos = portRT.anchoredPosition;

        portraitImage = portObj.GetComponent<Image>();
        portraitImage.preserveAspect = true;
        if (richSuitSprite != null) portraitImage.sprite = richSuitSprite;
        else if (normalSprite != null) portraitImage.sprite = normalSprite;

        Button portBtn = frameObj.AddComponent<Button>();
        portBtn.onClick.AddListener(() =>
        {
            if (isEndingSequence) { endingStepAdvanceRequested = true; return; }
            AudioManager.Instance?.PlayClick();
            if (!string.IsNullOrEmpty(currentTextEN))
            {
                StartDialogue(currentTextEN, currentTextTR);
            }
        });

        // Kompakt Konuşma Baloncuğu (Speech Bubble)
        speechBubbleObj = new GameObject("SpeechBubble", typeof(RectTransform), typeof(Image), typeof(Button));
        speechBubbleObj.transform.SetParent(advisorRoot, false);
        RectTransform bubbleRT = speechBubbleObj.GetComponent<RectTransform>();
        bubbleRT.anchorMin = new Vector2(0f, 0f);
        bubbleRT.anchorMax = new Vector2(0f, 0f);
        bubbleRT.pivot = new Vector2(0f, 0f);
        bubbleRT.anchoredPosition = new Vector2(72f, 2f);
        bubbleRT.sizeDelta = new Vector2(258f, 76f);

        Image bubbleImg = speechBubbleObj.GetComponent<Image>();
        bubbleImg.color = new Color(0.98f, 0.95f, 0.88f, 0.96f);

        Outline bubbleOutline = speechBubbleObj.AddComponent<Outline>();
        bubbleOutline.effectColor = new Color(0.22f, 0.18f, 0.14f, 1f);
        bubbleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button bubbleBtn = speechBubbleObj.GetComponent<Button>();
        bubbleBtn.onClick.AddListener(OnBubbleClicked);

        // Başlık Şeridi
        GameObject nameObj = new GameObject("SpeakerName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(bubbleRT, false);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, -4f);
        nameRT.sizeDelta = new Vector2(-14f, 14f);

        speakerNameText = nameObj.GetComponent<TextMeshProUGUI>();
        speakerNameText.text = LocalizationManager.Get("CITY ADVISOR", "ŞEHİR DANIŞMANI");
        speakerNameText.fontSize = 9.5f;
        speakerNameText.fontStyle = FontStyles.Bold;
        speakerNameText.color = new Color(0.55f, 0.32f, 0.12f, 1f);
        speakerNameText.alignment = TextAlignmentOptions.Left;
        speakerNameText.raycastTarget = false;

        // Diyalog Metni
        GameObject textObj = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(bubbleRT, false);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.offsetMin = new Vector2(8f, 6f);
        textRT.offsetMax = new Vector2(-8f, -18f);

        dialogueText = textObj.GetComponent<TextMeshProUGUI>();
        dialogueText.fontSize = 10.2f;
        dialogueText.color = new Color(0.15f, 0.13f, 0.11f, 1f);
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.raycastTarget = false;

        speechBubbleObj.SetActive(false);

        if (QuestAndMenuManager.Instance != null && QuestAndMenuManager.Instance.IsMenuOpen)
        {
            advisorRoot.gameObject.SetActive(false);
        }
    }
}
