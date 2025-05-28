using UnityEngine;
using GoogleMobileAds.Api;

public class AdmobManager : MonoBehaviour
{
    public static AdmobManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AdmobManager>();
            }
            return instance;
        }
    }
    public static AdmobManager instance;


    private bool isReady = false;
    public bool IsReady
    {
        get
        {
            return isReady;
        }
    }
    void Start()
    {
        if (this != Instance)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize(initStatus =>
        {
            isReady = true;
            Debug.Log(initStatus);
        });
    }
}