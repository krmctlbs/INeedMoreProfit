using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Genel Ses Ayarı")]
    [Range(0f, 1f)] public float masterVolume = 0.85f;
    [Range(0f, 1f)] public float musicVolume = 0.85f;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;
    [Range(0f, 1f)] public float voiceBlipVolume = 0.16f;
    public bool isMuted = false;

    [Header("Atmosfer & Müzik Dosyaları (Assets/Sounds)")]
    public AudioClip calmBirdsClip;
    public AudioClip badAirThrillClip;
    public AudioClip worstAirThrillClip;
    public AudioClip earDroneClip;

    [Header("Ses Efektleri (SFX)")]
    public AudioClip buildSfx;
    public AudioClip clickSfx;
    public AudioClip newspaperSfx;
    public AudioClip voiceBlipSfx;
    public AudioClip upgradeSfx;
    public AudioClip deleteSfx;
    public AudioClip questCompleteSfx;

    private AudioSource calmSource;
    private AudioSource badThrillSource;
    private AudioSource worstThrillSource;
    private AudioSource earDroneSource;
    private AudioSource sfxSource;
    private AudioSource voiceSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.85f);
        }

        AutoLoadClipsIfNeeded();
        SetupAudioSources();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoLoadClipsIfNeeded();
    }
#endif

    private void AutoLoadClipsIfNeeded()
    {
        // 1. Önce Resources klasöründen yükle (Hem Unity Editor'de hem de alınan Windows .exe Build'de %100 çalışır!)
        if (calmBirdsClip == null)
            calmBirdsClip = Resources.Load<AudioClip>("Sounds/calm_birds");
        if (badAirThrillClip == null)
            badAirThrillClip = Resources.Load<AudioClip>("Sounds/thrill_bad");
        if (worstAirThrillClip == null)
            worstAirThrillClip = Resources.Load<AudioClip>("Sounds/thrill_worst");
        if (earDroneClip == null)
            earDroneClip = Resources.Load<AudioClip>("Sounds/ear_drone");

        if (buildSfx == null)
            buildSfx = Resources.Load<AudioClip>("Sounds/buildsound");
        if (clickSfx == null)
            clickSfx = Resources.Load<AudioClip>("Sounds/clicksound");
        if (newspaperSfx == null)
            newspaperSfx = Resources.Load<AudioClip>("Sounds/newspapersound");

        if (voiceBlipSfx == null)
            voiceBlipSfx = Resources.Load<AudioClip>("Sounds/voice_blip");
        if (upgradeSfx == null)
            upgradeSfx = Resources.Load<AudioClip>("Sounds/upgrade_sound");
        if (deleteSfx == null)
            deleteSfx = Resources.Load<AudioClip>("Sounds/delete_sound");
        if (questCompleteSfx == null)
            questCompleteSfx = Resources.Load<AudioClip>("Sounds/quest_complete");

#if UNITY_EDITOR
        // 2. Editor içi yedek kontrol
        if (calmBirdsClip == null)
            calmBirdsClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/calm and birds_bestairquality.wav");
        if (badAirThrillClip == null)
            badAirThrillClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/thrill_badairquality.mp3");
        if (worstAirThrillClip == null)
            worstAirThrillClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/thrillestworstairquality.mp3");
        if (earDroneClip == null)
            earDroneClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Generated/ear_drone.wav");

        if (buildSfx == null)
            buildSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/buildsound.wav");
        if (clickSfx == null)
            clickSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/click sound.mp3");
        if (newspaperSfx == null)
            newspaperSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/newspapersound.wav");

        if (voiceBlipSfx == null)
            voiceBlipSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Generated/voice_blip.wav");
        if (upgradeSfx == null)
            upgradeSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Generated/upgrade_sound.wav");
        if (deleteSfx == null)
            deleteSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Generated/delete_sound.wav");
        if (questCompleteSfx == null)
            questCompleteSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Generated/quest_complete.wav");
#endif

        if (voiceBlipSfx == null) voiceBlipSfx = CreateRuntimeVoiceBlip();
        if (earDroneClip == null) earDroneClip = CreateRuntimeEarDrone();
    }

    private void SetupAudioSources()
    {
        calmSource = CreateLoopSource("CalmBirds_Layer", calmBirdsClip, 0.8f);
        badThrillSource = CreateLoopSource("BadAirThrill_Layer", badAirThrillClip, 0f);
        worstThrillSource = CreateLoopSource("WorstAirThrill_Layer", worstAirThrillClip, 0f);
        earDroneSource = CreateLoopSource("EarHollowDrone_Layer", earDroneClip, 0f);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
    }

    private AudioSource CreateLoopSource(string sourceName, AudioClip clip, float initialVol)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform);
        AudioSource src = child.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = initialVol * masterVolume * musicVolume;
        if (clip != null) src.Play();
        return src;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // WebGL (Tarayıcı) Autoplay Koruması: Tarayıcılar ilk tıklamaya kadar sesi durdurabildiği için
        // döngüsel ses kaynakları durmuşsa otomatik tekrar başlat
        EnsureLoopSourcePlaying(calmSource);
        EnsureLoopSourcePlaying(badThrillSource);
        EnsureLoopSourcePlaying(worstThrillSource);
        EnsureLoopSourcePlaying(earDroneSource);

        float aqi = GameManager.Instance.aqi;
        float happiness = GameManager.Instance.happiness;
        float effectiveMaster = isMuted ? 0f : (masterVolume * musicVolume);

        // 1. Kuş Cıvıltıları (Calm & Birds):
        // AQI 65'ten sonra kuş sesleri yavaşça pitch-down olur (1.0 -> 0.42),
        // sanki cıvıltılar bir hayalmiş ve baygınlıktan uyanıyormuşuz gibi ağırlaşır ve erir.
        if (calmSource != null)
        {
            float pitchWarp = Mathf.InverseLerp(60f, 180f, aqi);
            float happinessPitchPenalty = Mathf.InverseLerp(50f, 10f, happiness) * 0.12f;
            float targetCalmPitch = Mathf.Clamp(Mathf.Lerp(1.0f, 0.42f, pitchWarp) - happinessPitchPenalty, 0.38f, 1.0f);

            float fadeOut = 1f - Mathf.InverseLerp(120f, 190f, aqi);
            float targetCalmVol = 0.80f * fadeOut * effectiveMaster;

            calmSource.pitch = Mathf.Lerp(calmSource.pitch, targetCalmPitch, Time.unscaledDeltaTime * 1.5f);
            calmSource.volume = Mathf.Lerp(calmSource.volume, targetCalmVol, Time.unscaledDeltaTime * 1.2f);
        }

        // 2. Orta Kirlilik Gerilim Müziği (Thrill - Bad Air Quality):
        if (badThrillSource != null)
        {
            float fadeIn = Mathf.InverseLerp(70f, 135f, aqi);
            float fadeOut = 1f - Mathf.InverseLerp(180f, 235f, aqi);
            float targetBadVol = 0.72f * fadeIn * fadeOut * effectiveMaster;

            float targetBadPitch = Mathf.Lerp(1.0f, 0.88f, Mathf.InverseLerp(130f, 210f, aqi));
            badThrillSource.pitch = Mathf.Lerp(badThrillSource.pitch, targetBadPitch, Time.unscaledDeltaTime * 1.5f);
            badThrillSource.volume = Mathf.Lerp(badThrillSource.volume, targetBadVol, Time.unscaledDeltaTime * 1.2f);
        }

        // 3. Yoğun Kirlilik & Çöküş (Thrillest - Worst Air Quality):
        if (worstThrillSource != null)
        {
            float fadeIn = Mathf.InverseLerp(160f, 225f, aqi);
            float targetWorstVol = 0.82f * fadeIn * effectiveMaster;

            float happinessSlowdown = Mathf.InverseLerp(45f, 0f, happiness);
            float targetWorstPitch = Mathf.Lerp(1.0f, 0.84f, happinessSlowdown);

            worstThrillSource.pitch = Mathf.Lerp(worstThrillSource.pitch, targetWorstPitch, Time.unscaledDeltaTime * 1.5f);
            worstThrillSource.volume = Mathf.Lerp(worstThrillSource.volume, targetWorstVol, Time.unscaledDeltaTime * 1.2f);
        }

        // 4. Kulakta Boşluk ve Uğultu Sesi (Ear Hollow Drone / Tinnitus):
        if (earDroneSource != null)
        {
            float droneFade = Mathf.InverseLerp(115f, 215f, aqi);
            float lowPopBoost = (GameManager.Instance.population <= 5f) ? 0.18f : 0f;
            float targetDroneVol = Mathf.Clamp01(0.50f * droneFade + lowPopBoost) * effectiveMaster;

            float targetDronePitch = Mathf.Lerp(1.0f, 0.78f, Mathf.InverseLerp(155f, 260f, aqi));
            earDroneSource.pitch = Mathf.Lerp(earDroneSource.pitch, targetDronePitch, Time.unscaledDeltaTime * 1.2f);
            earDroneSource.volume = Mathf.Lerp(earDroneSource.volume, targetDroneVol, Time.unscaledDeltaTime * 1.2f);
        }
    }

    private void EnsureLoopSourcePlaying(AudioSource src)
    {
        if (src != null && src.clip != null && !src.isPlaying)
        {
            src.Play();
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
    }

    public void PlayClick() => PlayOneShot(clickSfx, 0.75f);
    public void PlayBuild() => PlayOneShot(buildSfx, 0.85f);
    public void PlayUpgrade() => PlayOneShot(upgradeSfx != null ? upgradeSfx : buildSfx, 0.85f);
    public void PlayDelete() => PlayOneShot(deleteSfx != null ? deleteSfx : clickSfx, 0.80f);
    public void PlayNewspaper() => PlayOneShot(newspaperSfx, 0.95f);
    public void PlayQuestComplete() => PlayOneShot(questCompleteSfx != null ? questCompleteSfx : clickSfx, 0.80f);

    public void PlayVoiceBlip(float minPitch = 0.92f, float maxPitch = 1.12f)
    {
        if (isMuted || voiceSource == null || voiceBlipSfx == null) return;
        voiceSource.pitch = Random.Range(minPitch, maxPitch);
        // Konuşma sesi rahatsız etmemesi için yumuşak seviyede (0.16f) çalınır
        voiceSource.PlayOneShot(voiceBlipSfx, masterVolume * sfxVolume * voiceBlipVolume);
    }

    private void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (isMuted || sfxSource == null || clip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, masterVolume * sfxVolume * volumeScale);
    }

    private AudioClip CreateRuntimeVoiceBlip()
    {
        int sr = 44100;
        int len = Mathf.RoundToInt(sr * 0.05f);
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float env = Mathf.Sin((i / (float)len) * Mathf.PI);
            float freq = 420f + 70f * (i / (float)len);
            data[i] = (0.55f * Mathf.Sin(2f * Mathf.PI * freq * t) + 0.20f * Mathf.Sin(4f * Mathf.PI * freq * t)) * env * 0.45f;
        }
        AudioClip clip = AudioClip.Create("RuntimeVoiceBlip", len, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateRuntimeEarDrone()
    {
        int sr = 44100;
        int len = sr * 4;
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)sr;
            float lfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
            float sub = 0.55f * Mathf.Sin(2f * Mathf.PI * 55f * t) + 0.30f * Mathf.Sin(2f * Mathf.PI * 55.5f * t);
            float ring = 0.05f * Mathf.Sin(2f * Mathf.PI * 2850f * t);
            data[i] = (sub * lfo + ring) * 0.6f;
        }
        AudioClip clip = AudioClip.Create("RuntimeEarDrone", len, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}
