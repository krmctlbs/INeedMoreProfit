using System;
using UnityEngine;

public enum GameLanguage
{
    English = 0,
    Turkish = 1
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    public static GameLanguage CurrentLanguage = GameLanguage.English;
    public static event Action OnLanguageChanged;

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

        if (PlayerPrefs.HasKey("GameLanguage"))
        {
            CurrentLanguage = (GameLanguage)PlayerPrefs.GetInt("GameLanguage", 0);
        }
        else
        {
            CurrentLanguage = GameLanguage.English;
        }
    }

    public static void ToggleLanguage()
    {
        CurrentLanguage = (CurrentLanguage == GameLanguage.English) ? GameLanguage.Turkish : GameLanguage.English;
        PlayerPrefs.SetInt("GameLanguage", (int)CurrentLanguage);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public static void SetLanguage(GameLanguage lang)
    {
        if (CurrentLanguage == lang) return;
        CurrentLanguage = lang;
        PlayerPrefs.SetInt("GameLanguage", (int)CurrentLanguage);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public static string Get(string englishText, string turkishText)
    {
        return CurrentLanguage == GameLanguage.Turkish ? turkishText : englishText;
    }

    public static string GetLanguageCode()
    {
        return CurrentLanguage == GameLanguage.Turkish ? "TR" : "EN";
    }
}
