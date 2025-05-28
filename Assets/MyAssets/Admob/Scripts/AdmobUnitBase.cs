using System.Collections;
using UnityEngine;
using GoogleMobileAds.Common;

public abstract class AdmobUnitBase : MonoBehaviour
{
    [SerializeField] private string unitIDAndroid;
    [SerializeField] private string unitIDIOS;

    protected string UnitID
    {
        get
        {
#if UNITY_ANDROID
            return unitIDAndroid;
#elif UNITY_IOS
            return unitIDIOS;
#else
            return "";
#endif
        }
    }
    private void OnAppStateChangedBase(AppState state)
    {
        Debug.Log("App State changed to : " + state);
        OnAppStateChanged(state);
    }

    private IEnumerator Start()
    {
        while (AdmobManager.Instance.IsReady == false)
        {
            yield return 0;
        }
        Initialize();
    }
    protected virtual void Initialize()
    {
        // AdsManagerの初期化が終わったあとに呼ばれる
    }
    protected virtual void OnAppStateChanged(AppState state)
    {
    }
}