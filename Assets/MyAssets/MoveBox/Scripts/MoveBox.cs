using UnityEngine;

public class MoveBox : MonoBehaviour, ITurnBased
{
    public Vector3 TargetPos { get; set; }

    void Start()
    {
        TargetPos = transform.position;
    }

    public void OnTurn() { }

    public void UpdateGridData()
    {
        // グリッドは常に実位置を信頼して更新（Undo後の不整合を防ぐ）
        StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'M');
    }

    public void TeleportIfOnPortal()
    {
        // 足元(1段下)がテレポート('A')なら、相方の'A'へ瞬時移動
        Vector3 support = TargetPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        if (!StageBuilder.Instance.IsValidGridPosition(support)) return;
        if (!StageBuilder.Instance.IsMatchingCellType(support, 'A')) return;

        var fromCell = StageBuilder.Instance.GridFromPosition(support);
        if (StageBuilder.Instance.TryFindOtherCell('A', fromCell, out var otherCell))
        {
            Vector3 dest = StageBuilder.Instance.WorldFromGrid(otherCell) + Vector3.up * StageBuilder.HEIGHT_OFFSET;

            // 目的地が占有されていないことを確認（簡易チェック）
            if (!StageBuilder.Instance.IsValidGridPosition(dest)) return;
            char occ = StageBuilder.Instance.GetGridCharType(dest);
            if (occ != 'N') return;

            // グリッド更新
            StageBuilder.Instance.UpdateGridAtPosition(TargetPos, 'N');
            StageBuilder.Instance.UpdateGridAtPosition(dest, 'M');

            // 瞬間移動
            TargetPos = dest;
            transform.position = dest;
        }
    }
}
