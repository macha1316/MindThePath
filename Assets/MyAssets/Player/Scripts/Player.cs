using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ITurnBased
{
    private float moveDuration = 1f;
    private bool isMoving = false;

    public void OnTurn()
    {
        if (isMoving) return;

        Vector3 nextPos = transform.position + transform.forward * 2.0f;

        // 進行方向が範囲外、ブロックがある、または方向変更ギミックがあるなら進行方向を変更
        if (!IsValidPosition(nextPos) || IsBlockedPosition(nextPos))
        {
            transform.forward = -transform.forward;
            nextPos = transform.position + transform.forward * 2.0f;
        }
        else if (IsDirectionChangeTile(nextPos))
        {
            transform.forward = Vector3.right; // 方向変更ギミックに対応
            nextPos = transform.position + transform.forward * 2.0f;
        }

        isMoving = true;

        transform.DOMove(nextPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => isMoving = false);
    }

    private bool IsValidPosition(Vector3 pos)
    {
        int col = Mathf.RoundToInt(pos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / StageBuilder.BLOCK_SIZE);

        return col >= 0 && col < StageBuilder.Instance.GetGridData().GetLength(0) &&
               height >= 0 && height < StageBuilder.Instance.GetGridData().GetLength(1) &&
               row >= 0 && row < StageBuilder.Instance.GetGridData().GetLength(2);
    }

    private bool IsBlockedPosition(Vector3 pos)
    {
        int col = Mathf.RoundToInt(pos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / StageBuilder.BLOCK_SIZE);

        return StageBuilder.Instance.GetGridData()[col, height, row] == 'B';
    }

    private bool IsDirectionChangeTile(Vector3 pos)
    {
        int col = Mathf.RoundToInt(pos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / StageBuilder.BLOCK_SIZE);

        return StageBuilder.Instance.GetDynamicGridData()[col, height, row] == 'Y';
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdatePlayerPosition(this);
    }
}
