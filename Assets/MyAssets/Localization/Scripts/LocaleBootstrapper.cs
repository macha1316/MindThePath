using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocaleBootstrapper : MonoBehaviour
{
    private const string PrefKey = "preferred_locale_code";

    private async void Awake()
    {
        await LocalizationSettings.InitializationOperation.Task;

        var saved = PlayerPrefs.GetString(PrefKey, "");
        if (string.IsNullOrEmpty(saved)) return;

        var target = LocalizationSettings.AvailableLocales.GetLocale(saved);
        if (target != null)
            LocalizationSettings.SelectedLocale = target;
    }
}
