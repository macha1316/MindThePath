using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdmobUnitBanner : AdmobUnitBase
{
    private BannerView bannerView;
#if UNITY_ANDROID
    private string bannerUnitId = "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IPHONE
    private string bannerUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
    private string bannerUnitId = "unused";
#endif

    protected override void Initialize()
    {
        ShowBanner();
    }

    public void ShowBanner()
    {
        if (bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            bannerView.Destroy();
            bannerView = null;
        }

        bannerView = new BannerView(
            bannerUnitId,
            AdSize.Banner,
            AdPosition.Top);

        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("ロードされました - 表示します");
            bannerView.Show();
        };
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.Log("ロード失敗しました");
        };

        //リクエストを生成
        var adRequest = new AdRequest();
        bannerView.LoadAd(adRequest);
    }
}