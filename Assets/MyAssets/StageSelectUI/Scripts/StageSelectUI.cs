using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StageSelectUI : MonoBehaviour
{
    private const string ClearedStageKey = "ClearedStage";
    private enum GimmickType
    {
        Up,
        Wall,
        MoveBox,
        Kyle
    }
    [SerializeField] GameObject stageSelectUI;
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject stageUI;
    [SerializeField] GameObject clearUI;
    [SerializeField] GameObject pauseUI;
    [SerializeField] GameObject optionUI;
    [SerializeField] GameObject stopUI;
    [SerializeField] TextMeshProUGUI speedText;

    [SerializeField] GameObject[] gimmickUIList;

    private Dictionary<int, List<GimmickType>> stageGimmickMap = new Dictionary<int, List<GimmickType>>
    {
        { 0, new List<GimmickType> { GimmickType.Up } },
        { 1, new List<GimmickType> { GimmickType.Up, GimmickType.Up } },
        { 2, new List<GimmickType> { GimmickType.Wall, GimmickType.Up} },
        { 3, new List<GimmickType> { GimmickType.Wall, GimmickType.Up, GimmickType.Up} },
        { 4, new List<GimmickType> { GimmickType.Wall} },
        { 5, new List<GimmickType> { GimmickType.Up, GimmickType.Kyle} },
        { 6, new List<GimmickType> { GimmickType.Up,GimmickType.Up, GimmickType.Kyle, GimmickType.Wall} }
    };

    public static StageSelectUI Instance;

    private List<GameObject> activeGimmickUIs = new List<GameObject>();

    [SerializeField] GameObject[] stageSelectButtons;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        startUI.SetActive(false);
        stageUI.SetActive(false);
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
        ClearGimmickUIs();

        if (stageGimmickMap.ContainsKey(stageNumber))
        {
            foreach (var gimmick in stageGimmickMap[stageNumber])
            {
                int index = (int)gimmick;
                if (index >= 0 && index < gimmickUIList.Length && gimmickUIList[index] != null)
                {
                    GameObject newUI = Instantiate(gimmickUIList[index], stageUI.transform);
                    newUI.GetComponent<CanvasGroup>().alpha = 1;
                    newUI.GetComponent<CanvasGroup>().blocksRaycasts = true;
                    activeGimmickUIs.Add(newUI);
                }
            }
        }
    }

    public GameObject CreateGimmickUI(char cellType)
    {
        int index = cellType switch
        {
            'U' => (int)GimmickType.Up,
            'B' => (int)GimmickType.Wall,
            'M' => (int)GimmickType.MoveBox,
            'K' => (int)GimmickType.Kyle,
            _ => -1
        };

        if (index >= 0 && index < gimmickUIList.Length && gimmickUIList[index] != null)
        {
            GameObject newUI = Instantiate(gimmickUIList[index], stageUI.transform);
            newUI.GetComponent<CanvasGroup>().alpha = 1;
            newUI.GetComponent<CanvasGroup>().blocksRaycasts = true;
            activeGimmickUIs.Add(newUI);
            return newUI;
        }
        return null;
    }

    private void ClearGimmickUIs()
    {
        foreach (var ui in activeGimmickUIs)
        {
            if (ui != null)
            {
                Destroy(ui);
            }
        }
        activeGimmickUIs.Clear();
    }

    private void SelectStageUI()
    {
        startUI.SetActive(true);
        stageUI.SetActive(true);
        stageSelectUI.SetActive(false);
        clearUI.SetActive(false);
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
    }

    public void SetClearUI()
    {
        startUI.SetActive(false);
        stageUI.SetActive(false);
        stageSelectUI.SetActive(false);
        pauseUI.SetActive(false);
        optionUI.SetActive(false);
        clearUI.SetActive(true);
    }

    public void GameStartUI()
    {
        stageUI.SetActive(false);
    }

    public void SetGameSpeedText(string txt)
    {
        speedText.text = txt;
    }

    public void SetStageSelectUI()
    {
        startUI.SetActive(false);
        stageUI.SetActive(false);
        stageSelectUI.SetActive(true);
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

