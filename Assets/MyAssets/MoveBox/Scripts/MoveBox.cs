using UnityEngine;
using DG.Tweening;

public class MoveBox : MonoBehaviour, ITurnBased
{
    public Vector3 TargetPos { get; set; }

    void Start()
    {
        TargetPos = transform.position;
    }


    public void UpdateGridData()
    {
        // グリッドは常に実位置を信頼して更新（Undo後の不整合を防ぐ）
        StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'M');
    }

    public void TeleportIfOnPortal()
    {
        // 同じ座標にテレポート('A')がある場合、相方の'A'へ瞬時移動
        Vector3 here = TargetPos;
        if (!StageBuilder.Instance.IsValidGridPosition(here)) return;

        var myCell = StageBuilder.Instance.GridFromPosition(here);
        TeleportBlock currentPortal = null;
        foreach (var p in GameObject.FindObjectsOfType<TeleportBlock>())
        {
            var c = StageBuilder.Instance.GridFromPosition(p.transform.position);
            if (c.x == myCell.x && c.y == myCell.y && c.z == myCell.z)
            {
                currentPortal = p;
                break;
            }
        }
        if (currentPortal == null) return;

        var fromCell = StageBuilder.Instance.GridFromPosition(currentPortal.transform.position);
        if (StageBuilder.Instance.TryFindOtherCell('A', fromCell, out var otherCell))
        {
            Vector3 dest = StageBuilder.Instance.WorldFromGrid(otherCell);

            // 目的地が占有されていないことを確認（'A'とH(OFF)は空扱い）
            if (!StageBuilder.Instance.IsValidGridPosition(dest)) return;
            char occ = StageBuilder.Instance.GetGridCharType(dest);
            if (!((occ == 'N' || occ == 'A') || StageBuilder.Instance.IsOnOffBlockEmptyAt(dest))) return;

            // 縮小→小さく弧を描いて移動→拡大の演出
            Vector3 s0 = transform.localScale;
            float shrinkT = 0.08f;
            float expandT = 0.12f;
            float arcT = 0.20f;
            // サウンド
            AudioManager.Instance?.PlayTeleportSound();
            transform.DOScale(s0 * 0.2f, shrinkT).OnComplete(() =>
            {
                Vector3 start = transform.position;
                Vector3 dir = (dest - start);
                Vector3 flat = new Vector3(dir.x, 0f, dir.z);
                Vector3 side = Vector3.Cross(flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward, Vector3.up);
                float sideSign = Random.value < 0.5f ? -1f : 1f;
                float sideMag = StageBuilder.BLOCK_SIZE * 0.2f;
                Vector3 mid = Vector3.Lerp(start, dest, 0.5f)
                              + Vector3.up * (StageBuilder.HEIGHT_OFFSET * 0.5f)
                              + side * sideSign * sideMag;

                var seq = DG.Tweening.DOTween.Sequence();
                seq.Append(transform.DOMove(mid, arcT * 0.5f).SetEase(Ease.OutSine));
                seq.Append(transform.DOMove(dest, arcT * 0.5f).SetEase(Ease.InSine));
                seq.OnComplete(() =>
                {
                    // グリッド更新
                    StageBuilder.Instance.UpdateGridAtPosition(TargetPos, 'N');
                    StageBuilder.Instance.UpdateGridAtPosition(dest, 'M');

                    // テレポート完了
                    TargetPos = dest;
                    transform.position = dest;
                    transform.DOScale(s0, expandT).SetEase(Ease.OutBack);
                    StageBuilder.Instance.RefreshSwitchAndOnOff();
                });
            });
        }
    }
}
