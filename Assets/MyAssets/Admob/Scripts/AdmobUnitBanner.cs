using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdmobUnitBanner : AdmobUnitBase
{
    private BannerView bannerView;

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
            UnitID,
            AdSize.Banner,
            AdPosition.Bottom);

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