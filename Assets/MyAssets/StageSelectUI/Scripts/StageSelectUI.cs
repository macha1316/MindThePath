using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectUI : MonoBehaviour
{
    [SerializeField] GameObject stageSelectUI;
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject clearUI;
    [SerializeField] GameObject pauseUI;
    [SerializeField] GameObject optionUI;
    [SerializeField] GameObject stopUI;
    [SerializeField] GameObject operatePlayerUI;
    [SerializeField] GameObject cameraRotateUI;
    [SerializeField] TextMeshProUGUI dimensionText;
    [SerializeField] GameObject titleUI;
    [SerializeField] GameObject titleText;
    [SerializeField] GameObject[] stageSelectButtons;
    [SerializeField] GameObject hintUI;
    [SerializeField] GameObject rewardPanel;
    [SerializeField] TextMeshProUGUI stageNumberText;

    // Tutorial UI
    [SerializeField] GameObject tutorialBg;
    [SerializeField] GameObject tutorialUI;
    [SerializeField] GameObject[] tutorialPages;
    [SerializeField] GameObject nextPageButton;
    [SerializeField] GameObject previousPageButton;
    private int tutorialPageIndex = 0;
    [SerializeField] TextMeshProUGUI tutorialPageText;

    AdmobUnitInterstitial admobUnitInterstitial;
    AdmobUnitReward admobUnitReward;

    public static StageSelectUI Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        CloseAllUI();
        titleUI.SetActive(true);
        if (titleText != null)
        {
            var rect = titleText.GetComponent<RectTransform>();
            rect.DOAnchorPosY(rect.anchoredPosition.y + 20f, 1f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        dimensionText.text = "2D";

        admobUnitInterstitial = FindObjectOfType<AdmobUnitInterstitial>();
        admobUnitReward = FindObjectOfType<AdmobUnitReward>();

        UpdateStageSelectButtons();
        StartCoroutine(AnimateStageButtons());
    }

    public void SelectStageUI(int stageNumber)
    {
        // ステージ開始用UI表示 + ステージ番号の反映
        CloseAllUI();
        startUI.SetActive(true);
        stopUI.SetActive(true);
        operatePlayerUI.SetActive(true);
        cameraRotateUI.SetActive(true);
        if (stageNumberText != null)
        {
            int displayNum = stageNumber + 1; // 0始まり→1始まり表記
            stageNumberText.text = "ステージ" + displayNum.ToString();
        }
    }

    private void CloseAllUI()
    {
        startUI.SetActive(false);
        stopUI.SetActive(false);
        operatePlayerUI.SetActive(false);
        stageSelectUI.SetActive(false);
        clearUI.SetActive(false);
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
        cameraRotateUI.SetActive(false);
        titleUI.SetActive(false);
        hintUI.SetActive(false);
        rewardPanel.SetActive(false);
        tutorialBg.SetActive(false);
        tutorialUI.transform.DOScale(Vector3.zero, 0f).SetEase(Ease.OutBack);
        stageNumberText.text = string.Empty;
    }

    private void SelectStageUI()
    {
        // 引数なしの場合は現在のステージ番号を利用
        int current = StageBuilder.Instance != null ? StageBuilder.Instance.stageNumber : 0;
        SelectStageUI(current);
    }

    public void SetClearUI()
    {
        // ステージクリア時に進行状況を保存
        SaveClearedStage(StageBuilder.Instance.stageNumber);
        CloseAllUI();
        admobUnitInterstitial.ShowInterstitial();
        clearUI.SetActive(true);
    }

    public void SetStageSelectUI()
    {
        CloseAllUI();
        stageSelectUI.SetActive(true);
        // ステージ選択画面を表示するたびにアンロック状態を更新
        UpdateStageSelectButtons();
        StageBuilder.Instance.DestroyStage();
        GameManager.Instance.IsStart = false;
    }

    public void ShowPauseUI()
    {
        CloseAllUI();
        pauseUI.SetActive(true);
    }

    public void ShowOptionUI()
    {
        CloseAllUI();
        optionUI.SetActive(true);
    }

    public void ShowCameraRotateUI()
    {
        cameraRotateUI.SetActive(true);
        dimensionText.text = "2D";
    }

    public void HideCameraRotateUI()
    {
        cameraRotateUI.SetActive(false);
        dimensionText.text = "3D";
    }

    public void HidePauseUI()
    {
        CloseAllUI();
        stopUI.SetActive(true);
        operatePlayerUI.SetActive(true);
        cameraRotateUI.SetActive(true);
        startUI.SetActive(true);
        if (stageNumberText != null)
        {
            int current = StageBuilder.Instance != null ? StageBuilder.Instance.stageNumber : 0;
            stageNumberText.text = "ステージ" + (current + 1).ToString();
        }
    }

    public void ClickTitle()
    {
        CloseAllUI();
        stageSelectUI.SetActive(true);
        CameraController.Instance.titleCamera.Priority = 0;
    }

    public void ShowTutorialUI()
    {
        tutorialPageIndex = 1;
        tutorialPageText.text = tutorialPageIndex.ToString() + " / " + (tutorialPages.Length).ToString();
        tutorialBg.SetActive(true);
        tutorialUI.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        for (int i = 0; i < tutorialPages.Length; i++)
        {
            tutorialPages[i].SetActive(false);
        }
        tutorialPages[0].SetActive(true);
        previousPageButton.SetActive(false);
    }

    IEnumerator HideTutorialUIDeray()
    {
        tutorialUI.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.Linear);
        yield return new WaitForSeconds(0.3f);
        tutorialBg.SetActive(false);
    }

    public void HideTutorialUI()
    {
        StartCoroutine(HideTutorialUIDeray());
    }

    public void NextTutorialPage()
    {
        for (int i = 0; i < tutorialPages.Length; i++)
        {
            if (tutorialPages[i].activeSelf)
            {
                tutorialPages[i].SetActive(false);
                previousPageButton.SetActive(true);
                tutorialPageIndex += 1;
                tutorialPageText.text = tutorialPageIndex.ToString() + " / " + (tutorialPages.Length).ToString();

                if (i + 1 < tutorialPages.Length)
                {
                    tutorialPages[i + 1].SetActive(true);
                    if (i + 1 == tutorialPages.Length - 1)
                    {
                        nextPageButton.SetActive(false);
                    }
                    break;
                }
            }
        }
    }

    public void PreviousTutorialPage()
    {
        for (int i = 0; i < tutorialPages.Length; i++)
        {
            if (tutorialPages[i].activeSelf)
            {
                tutorialPages[i].SetActive(false);
                nextPageButton.SetActive(true);
                tutorialPageIndex -= 1;
                tutorialPageText.text = tutorialPageIndex.ToString() + " / " + (tutorialPages.Length).ToString();
                if (i - 1 >= 0)
                {
                    tutorialPages[i - 1].SetActive(true);
                    if (i - 1 == 0)
                    {
                        previousPageButton.SetActive(false);
                    }
                    break;
                }
            }
        }
    }

    IEnumerator AnimateStageButtons()
    {
        for (int i = 0; i < stageSelectButtons.Length; i++)
        {
            var button = stageSelectButtons[i];
            if (button != null)
            {
                var rect = button.GetComponent<RectTransform>();
                Vector2 originalPos = rect.anchoredPosition;

                // 1回だけ上下動させる
                rect.DOAnchorPosY(originalPos.y + 10f, 0.25f)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        rect.DOAnchorPosY(originalPos.y, 0.25f).SetEase(Ease.InOutSine);
                    });
            }
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(3f);
        StartCoroutine(AnimateStageButtons());
    }

    public void ShowHintUI()
    {
        CloseAllUI();
        hintUI.SetActive(true);
    }

    public void ShowRewardPanel()
    {
        CloseAllUI();
        rewardPanel.SetActive(true);
    }

    public void ShowRewardAd()
    {
        if (admobUnitReward != null && admobUnitReward.IsReady)
        {
            admobUnitReward.ShowRewardAd((reward) =>
            {
                if (reward != null)
                {
                    Debug.Log("Reward type: " + reward.Type);
                    Debug.Log("Reward received: " + reward.Amount);
                    // リワード視聴完了: 現在のステージをスキップ
                    SkipCurrentStage();
                }
            });
        }
        else
        {
            Debug.Log("Reward ad is not ready yet.");
        }
    }

    // リワードで現在のステージをスキップ
    private void SkipCurrentStage()
    {
        // 進行度を更新して次のステージを解放
        int current = StageBuilder.Instance.stageNumber;
        SaveClearedStage(current);

        // リワードパネルを閉じる
        rewardPanel.SetActive(false);

        // 次のステージがある場合はロード、なければステージ選択に戻る
        int nextIndex = current + 1;
        int totalStages = stageSelectButtons != null ? stageSelectButtons.Length : 0;
        if (nextIndex < totalStages)
        {
            StageBuilder.Instance.BuildNextStage();
        }
        else
        {
            SetStageSelectUI();
        }
    }

    // クリア進行度を保存（クリアしたら次のステージを解放）
    public void SaveClearedStage(int stageNumber)
    {
        // 保存値は「解放済みの最大インデックス」として扱う
        int currentUnlocked = PlayerPrefs.GetInt(PlayerPrefsManager.ClearedStageKey, 0);
        int nextToUnlock = stageNumber + 1; // クリアしたら次のインデックスを解放

        // 最大値を更新（戻さない）
        int newUnlocked = Mathf.Max(currentUnlocked, nextToUnlock);

        // 上限保護（ボタン数を超えないように）
        int maxIndex = stageSelectButtons != null ? Mathf.Max(0, stageSelectButtons.Length - 1) : 0;
        newUnlocked = Mathf.Min(newUnlocked, maxIndex);

        if (newUnlocked != currentUnlocked)
        {
            PlayerPrefs.SetInt(PlayerPrefsManager.ClearedStageKey, newUnlocked);
            PlayerPrefs.Save();
        }
    }

    // ステージ選択ボタンの解放状態を反映
    private void UpdateStageSelectButtons()
    {
        int unlockedIndex = PlayerPrefs.GetInt(PlayerPrefsManager.ClearedStageKey, 0);
        for (int i = 0; i < stageSelectButtons.Length; i++)
        {
            bool isUnlocked = i <= unlockedIndex;
            stageSelectButtons[i].SetActive(true);

            var button = stageSelectButtons[i].GetComponent<Button>();
            if (button != null) button.interactable = isUnlocked;

            var image = stageSelectButtons[i].GetComponent<Image>();
            if (image != null)
                image.color = new Color(image.color.r, image.color.g, image.color.b, isUnlocked ? 1f : 0.4f);
        }
    }

    // デバッグ用: クリア済みステージ情報(ClearedStageKey)を削除
    public void ClearClearedStageKey()
    {
        PlayerPrefs.DeleteKey(PlayerPrefsManager.ClearedStageKey);
        PlayerPrefs.Save();
        Debug.Log("ClearedStageKey deleted.");
    }
}
