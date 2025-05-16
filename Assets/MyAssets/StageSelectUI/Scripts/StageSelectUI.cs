using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StageSelectUI : MonoBehaviour
{
    private const string ClearedStageKey = "ClearedStage";

    [SerializeField] GameObject stageSelectUI;
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject clearUI;
    [SerializeField] GameObject pauseUI;
    [SerializeField] GameObject optionUI;
    [SerializeField] GameObject stopUI;
    [SerializeField] TextMeshProUGUI speedText;

    public static StageSelectUI Instance;


    [SerializeField] GameObject[] stageSelectButtons;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        startUI.SetActive(false);
        clearUI.SetActive(false);
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
        stageSelectUI.SetActive(true);

        int clearedStage = PlayerPrefs.GetInt(ClearedStageKey, 0);

        for (int i = 0; i < stageSelectButtons.Length; i++)
        {
            bool isUnlocked = i <= clearedStage;
            stageSelectButtons[i].SetActive(true);

            var button = stageSelectButtons[i].GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.interactable = isUnlocked;

            var image = stageSelectButtons[i].GetComponent<UnityEngine.UI.Image>();
            if (image != null)
                image.color = new Color(image.color.r, image.color.g, image.color.b, isUnlocked ? 1f : 0.4f);
        }
    }

    public void SelectStageUI(int stageNumber)
    {
        SelectStageUI();
    }

    private void SelectStageUI()
    {
        startUI.SetActive(true);
        stageSelectUI.SetActive(false);
        clearUI.SetActive(false);
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
    }

    public void SetClearUI()
    {
        startUI.SetActive(false);
        stageSelectUI.SetActive(false);
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
        clearUI.SetActive(true);
    }

    public void SetGameSpeedText(string txt)
    {
        speedText.text = txt;
    }

    public void SetStageSelectUI()
    {
        startUI.SetActive(false);
        clearUI.SetActive(false);
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
        StageBuilder.Instance.DestroyStage();
    }

    public void ShowPauseUI()
    {
        pauseUI.SetActive(true);
        optionUI.SetActive(false);
        stopUI.SetActive(false);
    }

    public void ShowOptionUI()
    {
        pauseUI.SetActive(false);
        optionUI.SetActive(true);
        stopUI.SetActive(false);
    }

    public void HidePauseUI()
    {
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
        stopUI.SetActive(true);
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

