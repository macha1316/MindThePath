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

        // 範囲外 or ブロック → 反転
        if (!IsValidPosition(nextPos) || IsMatchingCellType(nextPos, 'B'))
        {
            transform.forward = -transform.forward;
            nextPos = transform.position + transform.forward * 2.0f;
        }

        isMoving = true;

        transform.DOMove(nextPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                isMoving = false;

                // Dynamic
                if (IsMatchingDynamicCellType(transform.position, 'U'))
                {
                    transform.forward = Vector3.forward;
                }
                if (IsMatchingDynamicCellType(transform.position, 'D'))
                {
                    transform.forward = Vector3.back;
                }
                if (IsMatchingDynamicCellType(transform.position, 'R'))
                {
                    transform.forward = Vector3.right;
                }
                if (IsMatchingDynamicCellType(transform.position, 'L'))
                {
                    transform.forward = Vector3.left;
                }
                // 通常
                if (IsMatchingCellType(transform.position, 'G'))
                {
                    Debug.Log("GGGGGGGoal");
                }
            });
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

    private bool IsMatchingCellType(Vector3 pos, char cellType)
    {
        int col = Mathf.RoundToInt(pos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / StageBuilder.BLOCK_SIZE);

        return StageBuilder.Instance.GetGridData()[col, height, row] == cellType;
    }

    private bool IsMatchingDynamicCellType(Vector3 pos, char cellType)
    {
        int col = Mathf.RoundToInt(pos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / StageBuilder.BLOCK_SIZE);

        return StageBuilder.Instance.GetDynamicGridData()[col, height, row] == cellType;
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdatePlayerPosition(this);
    }
}
