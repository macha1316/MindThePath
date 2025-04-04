using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ITurnBased
{
    void OnTurn();        // ターンごとの動作（移動やアクション）
    void UpdateGridData(); // gridDataの更新
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    private List<ITurnBased> turnObjects = new List<ITurnBased>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ExecuteTurn()
    {
        foreach (var obj in turnObjects)
        {
            obj.OnTurn();
        }

        UpdateGridData();

        // Goal判定
        if (GameManager.Instance.IsComplete())
        {
            StageSelectUI.Instance.SetClearUI();
        }
    }

    private void UpdateGridData()
    {
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
        // 一旦ここ
        turnObjects.AddRange(FindObjectsOfType<MonoBehaviour>().OfType<ITurnBased>());
        while (GameManager.Instance.GetIsStart())
        {
            ExecuteTurn();
            yield return new WaitForSeconds(1f);
        }
    }

    public void StartMove()
    {
        turnObjects = new List<ITurnBased>();
        GameManager.Instance.SetIsStart();
        StartCoroutine(TurnLoop());
    }
}
