using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Minimal binder: a Slider directly controls an AudioMixer exposed parameter, with persistence.
public class AudioMixerParameterSlider : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string parameterName = "bgm"; // e.g., "bgm" or "se"
    [SerializeField] private Slider slider; // 0..1

    [Header("Persistence")]
    [Tooltip("Save and load slider value via PlayerPrefs")] 
    [SerializeField] private bool persist = true;
    [Tooltip("Optional custom PlayerPrefs key. If empty, uses 'AudioMixer.{parameterName}'")] 
    [SerializeField] private string prefsKey = "";
    [Tooltip("Initial slider value when no saved value exists")] 
    [Range(0f,1f)] [SerializeField] private float defaultValue = 0.5f; // center default

    [Header("dB Mapping")]
    [Tooltip("Minimum dB when slider is 0 (mute)")]
    [SerializeField] private float minDb = -80f;
    [Tooltip("Maximum dB when slider is 1 (0dB recommended)")]
    [SerializeField] private float maxDb = 0f;

    private bool _hooked;

    private void Reset()
    {
        // Auto-assign Slider if placed on the same GameObject
        if (slider == null) slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null || mixer == null || string.IsNullOrEmpty(parameterName)) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;

        // Determine initial linear value
        float initial = defaultValue;
        string key = GetPrefsKey();
        if (persist && !string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key))
        {
            initial = Mathf.Clamp01(PlayerPrefs.GetFloat(key, defaultValue));
        }
        else if (mixer.GetFloat(parameterName, out float currentDb))
        {
            initial = DbToLinear(currentDb);
        }

        slider.SetValueWithoutNotify(initial);
        slider.onValueChanged.AddListener(OnSliderChanged);
        _hooked = true;

        // Apply once to ensure mixer reflects slider value
        OnSliderChanged(slider.value);
    }

    private void Start()
    {
        // Re-apply in Start to win initialization order races
        if (slider != null && mixer != null && !string.IsNullOrEmpty(parameterName))
        {
            OnSliderChanged(slider.value);
            // Optionally apply again after one frame for safety
            StartCoroutine(DeferredApply());
        }
    }

    private void OnDisable()
    {
        if (_hooked && slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderChanged);
            _hooked = false;
        }
    }

    private System.Collections.IEnumerator DeferredApply()
    {
        yield return null; // wait one frame
        if (slider != null && mixer != null && !string.IsNullOrEmpty(parameterName))
        {
            OnSliderChanged(slider.value);
        }
    }

    private void OnSliderChanged(float value)
    {
        if (mixer == null || string.IsNullOrEmpty(parameterName)) return;
        float db = LinearToDb(Mathf.Clamp01(value));
        mixer.SetFloat(parameterName, db);

        if (persist)
        {
            string key = GetPrefsKey();
            if (!string.IsNullOrEmpty(key))
            {
                PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }
    }

    private string GetPrefsKey()
    {
        if (!string.IsNullOrEmpty(prefsKey)) return prefsKey;
        if (!string.IsNullOrEmpty(parameterName)) return $"AudioMixer.{parameterName}";
        return string.Empty;
    }

    private float LinearToDb(float value)
    {
        if (value <= 0.0001f) return minDb;
        // Map 0..1 to dB using logarithmic law (20*log10)
        float db = Mathf.Log10(value) * 20f;
        // Clamp into configured range
        return Mathf.Clamp(db, minDb, maxDb);
    }

    private float DbToLinear(float db)
    {
        if (db <= minDb) return 0f;
        db = Mathf.Clamp(db, minDb, maxDb);
        return Mathf.Pow(10f, db / 20f);
    }
}
