using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdmobUnitReward : AdmobUnitBase
{
    private RewardedAd rewardedAd;
    private Supabase supabase;

    void Awake()
    {
        supabase = FindObjectOfType<Supabase>();
    }

    public bool IsReady
    {
        get
        {
            if (AdmobManager.Instance.IsReady == false)
            {
                return false;
            }
            return rewardedAd != null && rewardedAd.CanShowAd();
        }
    }
    protected override void Initialize()
    {
        LoadRewardAd();
    }

    public void LoadRewardAd()
    {
        if (IsReady)
        {
            Debug.Log("Reward ad is already loaded.");
            return;
        }

        // Clean up the old ad before loading a new one.
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        var adRequest = new AdRequest();

        RewardedAd.Load(UnitID, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("rewarded ad failed to load an ad " +
                        error?.ToString());
                    return;
                }

                rewardedAd = ad;
                RegisterEventHandlers(rewardedAd);
            });
    }

    public void ShowRewardAd(Action<Reward> onReward)
    {
        if (IsReady)
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward ad completed successfully.");
                onReward?.Invoke(reward);
            });
        }
        else
        {
            Debug.Log("Reward ad is not ready yet.");
            onReward?.Invoke(null);
        }
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");
            LoadRewardAd();
            StageSelectUI.Instance.ShowRewardPanel();
            supabase.PlayVideo();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);
            LoadRewardAd();
        };
    }
}