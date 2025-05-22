using TMPro;
using UnityEditor.SceneManagement;
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

    public static StageSelectUI Instance;


    [SerializeField] GameObject[] stageSelectButtons;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        CloseAllUI();
        titleUI.SetActive(true);
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

