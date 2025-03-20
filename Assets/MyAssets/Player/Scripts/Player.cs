using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ITurnBased
{
    public float moveDuration = 1f;
    private bool isMoving = false;

    public void OnTurn()
    {
        if (isMoving) return;

        Vector3 nextPos = transform.position + transform.forward * 2.0f;

        // 進行方向が範囲外なら進行方向を反転
        if (!IsValidPosition(nextPos))
        {
            transform.forward = -transform.forward;
            nextPos = transform.position + transform.forward * 2.0f;
        }

        isMoving = true;

        transform.DOMove(nextPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => isMoving = false);
    }

    private bool IsValidPosition(Vector3 pos)
    {
        int col = Mathf.RoundToInt(pos.x / StageBuilder.Instance.blockSize);
        int height = Mathf.RoundToInt(pos.y / StageBuilder.Instance.heightOffset);
        int row = Mathf.RoundToInt(pos.z / StageBuilder.Instance.blockSize);

        return col >= 0 && col < StageBuilder.Instance.gridData.GetLength(0) &&
               height >= 0 && height < StageBuilder.Instance.gridData.GetLength(1) &&
               row >= 0 && row < StageBuilder.Instance.gridData.GetLength(2);
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdatePlayerPosition(this);
    }
}
