using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectUI : MonoBehaviour
{
    private const string ClearedStageKey = "ClearedStage";

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
    [SerializeField] GameObject hintButton;

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

        int clearedStage = PlayerPrefs.GetInt(ClearedStageKey, 0);

        for (int i = 0; i < stageSelectButtons.Length; i++)
        {
            bool isUnlocked = i <= clearedStage;
            stageSelectButtons[i].SetActive(true);

            var button = stageSelectButtons[i].GetComponent<Button>();
            if (button != null)
                button.interactable = isUnlocked;

            var image = stageSelectButtons[i].GetComponent<Image>();
            if (image != null)
                image.color = new Color(image.color.r, image.color.g, image.color.b, isUnlocked ? 1f : 0.4f);
        }
        StartCoroutine(AnimateStageButtons());
    }

    public void SelectStageUI(int stageNumber)
    {
        SelectStageUI();
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
    }

    private void SelectStageUI()
    {
        CloseAllUI();
        startUI.SetActive(true);
        stopUI.SetActive(true);
        operatePlayerUI.SetActive(true);
        cameraRotateUI.SetActive(true);
    }

    public void SetClearUI()
    {
        CloseAllUI();
        clearUI.SetActive(true);
    }

    public void SetStageSelectUI()
    {
        CloseAllUI();
        stageSelectUI.SetActive(true);
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
    }

    public void ClickTitle()
    {
        CloseAllUI();
        stageSelectUI.SetActive(true);
        CameraController.Instance.titleCamera.Priority = 0;
        StartCoroutine(StageBuilder.Instance.UpBlocks());
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

    IEnumerator AnimateHintButton()
    {
        if (hintButton == null) yield break;

        RectTransform rect = hintButton.GetComponent<RectTransform>();
        rect.DOScale(1.2f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        yield break;
    }

    public void StopHintButtonAnimation()
    {
        if (hintButton != null)
        {
            RectTransform rect = hintButton.GetComponent<RectTransform>();
            rect.DOKill();
            rect.localScale = Vector3.one;
        }
    }

    // デバッグ用: 最大にしている
    public void SaveClearedStage(int stageNumber)
    {
        int currentCleared = PlayerPrefs.GetInt(ClearedStageKey, 0);
        // if (stageNumber > currentCleared)
        {
            PlayerPrefs.SetInt(ClearedStageKey, 10);
            // PlayerPrefs.SetInt(ClearedStageKey, stageNumber);
            PlayerPrefs.Save();
        }
    }
}