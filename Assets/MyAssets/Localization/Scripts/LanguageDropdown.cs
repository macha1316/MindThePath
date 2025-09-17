using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    // 保存キー
    private const string PrefKey = "preferred_locale_code";

    // ロケールID→インデックス対応表
    private readonly List<Locale> _locales = new();

    private async void Awake()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();

        // 利用可能なロケールを取得（en, ja など）
        await LocalizationSettings.InitializationOperation.Task;
        var available = LocalizationSettings.AvailableLocales.Locales;

        dropdown.ClearOptions();
        _locales.Clear();

        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var locale in available)
        {
            // 表示名は「日本語 / English」など。好みで native name や code を併記。
            var display = string.IsNullOrEmpty(locale.Identifier.CultureInfo?.NativeName)
                ? locale.Identifier.Code
                : locale.Identifier.CultureInfo.NativeName;

            options.Add(new TMP_Dropdown.OptionData(display));
            _locales.Add(locale);
        }
        dropdown.AddOptions(options);

        // 既存のユーザー選択を反映（なければ現在のSelectedLocale）
        var saved = PlayerPrefs.GetString(PrefKey, "");
        int index = 0;
        if (!string.IsNullOrEmpty(saved))
        {
            index = _locales.FindIndex(l => l.Identifier.Code == saved);
            if (index < 0) index = 0;
        }
        else
        {
            var current = LocalizationSettings.SelectedLocale;
            index = _locales.FindIndex(l => l == current);
        }

        dropdown.SetValueWithoutNotify(index);
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    private void OnDestroy()
    {
        dropdown?.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        if (index < 0 || index >= _locales.Count) return;

        var chosen = _locales[index];
        LocalizationSettings.SelectedLocale = chosen; // ここで即時切替
        PlayerPrefs.SetString(PrefKey, chosen.Identifier.Code);
        PlayerPrefs.Save();
    }
}
