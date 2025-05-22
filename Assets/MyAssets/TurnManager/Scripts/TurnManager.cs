using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    private List<ITurnBased> turnObjects = new List<ITurnBased>();
    private bool isFirstComplete = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ExecuteTurn()
    {
        if (isFirstComplete) return;
        foreach (var obj in turnObjects)
        {
            obj.OnTurn();
        }

        UpdateGridData();

        // Goal判定
        if (GameManager.Instance.IsGameClear)
        {
            isFirstComplete = true;
        }
    }

    private void UpdateGridData()
    {
        if (!GameManager.Instance.IsStart) return;
        // gridDataをリセット（全てのオブジェクトの位置を反映）
        StageBuilder.Instance.ResetGridData();

        foreach (var obj in turnObjects)
        {
            obj.UpdateGridData();
        }
    }

    // 1秒ごとにターンを実行
    IEnumerator TurnLoop()
    {
        while (GameManager.Instance.IsStart)
        {
            ExecuteTurn();
            yield return new WaitForSeconds(1f);
        }
    }

    public void StartMove()
    {
        if (StageBuilder.Instance.IsGenerating) return;
        if (GameManager.Instance.IsStart) return;
        isFirstComplete = false;
        turnObjects = new List<ITurnBased>();
        turnObjects.AddRange(FindObjectsOfType<MonoBehaviour>().OfType<ITurnBased>());
        turnObjects = turnObjects
            .OrderByDescending(obj => (obj as MonoBehaviour).GetComponent<MoveBox>() != null)
            .ToList();
        GameManager.Instance.IsStart = true;
        GameManager.Instance.IsGameClear = false;
        AudioManager.Instance.GameStartSound();
        StartCoroutine(TurnLoop());
    }
}
