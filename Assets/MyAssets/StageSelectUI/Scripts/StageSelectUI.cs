using System.Collections.Generic;
using UnityEngine;

public enum GimmickType
{
    Up,
    Down,
    Right,
    Left,
    Wall,
    MoveBox
}

public class StageSelectUI : MonoBehaviour
{
    [SerializeField] GameObject stageSelectUI;
    [SerializeField] GameObject startUI;
    [SerializeField] GameObject stageUI;
    [SerializeField] GameObject clearUI;

    public GameObject[] gimmickUIList;

    private Dictionary<GimmickType, GameObject> gimmickUIPrefabMap = new Dictionary<GimmickType, GameObject>();
    private Dictionary<int, List<GimmickType>> stageGimmickMap = new Dictionary<int, List<GimmickType>>
    {
        { 0, new List<GimmickType> { GimmickType.Left } },
        { 1, new List<GimmickType> { GimmickType.Left, GimmickType.Down } },
        { 2, new List<GimmickType> { GimmickType.Wall, GimmickType.Up} },
        { 3, new List<GimmickType> { GimmickType.Wall, GimmickType.Up, GimmickType.Right} },
        { 4, new List<GimmickType> { GimmickType.Wall, GimmickType.MoveBox, GimmickType.Up} }
    };

    public static StageSelectUI Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        startUI.SetActive(false);
        stageUI.SetActive(false);
        clearUI.SetActive(false);
        stageSelectUI.SetActive(true);
    }

    void Start()
    {
        gimmickUIPrefabMap[GimmickType.Up] = gimmickUIList[0];
        gimmickUIPrefabMap[GimmickType.Down] = gimmickUIList[1];
        gimmickUIPrefabMap[GimmickType.Right] = gimmickUIList[2];
        gimmickUIPrefabMap[GimmickType.Left] = gimmickUIList[3];
        gimmickUIPrefabMap[GimmickType.Wall] = gimmickUIList[4];
        gimmickUIPrefabMap[GimmickType.MoveBox] = gimmickUIList[5];
    }

    public void SelectStageUI(int stageNumber)
    {
        SelectStageUI();
        SetGimmickUIParents();

        // ステージごとのstageUIを変更する
        foreach (var ui in gimmickUIPrefabMap.Values)
        {
            ui.SetActive(false);
        }

        if (stageGimmickMap.ContainsKey(stageNumber))
        {
            foreach (var gimmick in stageGimmickMap[stageNumber])
            {
                if (gimmickUIPrefabMap.ContainsKey(gimmick))
                {
                    gimmickUIPrefabMap[gimmick].SetActive(true);
                }
            }
        }
    }

    private void SelectStageUI()
    {
        startUI.SetActive(true);
        stageUI.SetActive(true);
        stageSelectUI.SetActive(false);
        clearUI.SetActive(false);
    }

    public void SetClearUI()
    {
        startUI.SetActive(false);
        stageUI.SetActive(false);
        stageSelectUI.SetActive(false);
        clearUI.SetActive(true);
    }

    private void SetGimmickUIParents()
    {
        foreach (var ui in gimmickUIList)
        {
            if (ui != null)
            {
                ui.GetComponent<CanvasGroup>().alpha = 1;
                ui.GetComponent<CanvasGroup>().blocksRaycasts = true;
                ui.transform.SetParent(stageUI.transform, false);
            }
        }
    }
}
