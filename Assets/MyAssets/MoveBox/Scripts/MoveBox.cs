using UnityEngine;
using DG.Tweening;
using System;

public class MoveBox : MonoBehaviour, ITurnBased
{
    private bool isMoving = false;
    private float moveDuration = 1f;
    private Vector3 targetPos;

    public void TryPush(Vector3 direction)
    {
        if (isMoving) return;

        targetPos = transform.position + direction * StageBuilder.BLOCK_SIZE;

        if (!StageBuilder.Instance.IsValidGridPosition(targetPos)) return;

        char[,,] grid = StageBuilder.Instance.GetGridData();
        int col = Mathf.RoundToInt(targetPos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(targetPos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(targetPos.z / StageBuilder.BLOCK_SIZE);

        if (grid[col, height, row] != 'N') return;

        isMoving = true;

        transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            FallIfNeeded();
        });
    }

    public void OnTurn()
    {
        // Do nothing by default
    }

    public void UpdateGridData()
    {
        // いきなりtargetPosに合わせるの問題になる？かも
        StageBuilder.Instance.UpdateGridAtPosition(targetPos, 'M');
    }

    private void FallIfNeeded()
    {
        Vector3 currentPos = targetPos;
        Vector3 down = Vector3.down * StageBuilder.HEIGHT_OFFSET;

        while (true)
        {
            Vector3 belowPos = currentPos + down;
            if (!StageBuilder.Instance.IsValidGridPosition(belowPos)) break;

            int col = Mathf.RoundToInt(belowPos.x / StageBuilder.BLOCK_SIZE);
            int height = Mathf.RoundToInt(belowPos.y / StageBuilder.HEIGHT_OFFSET);
            int row = Mathf.RoundToInt(belowPos.z / StageBuilder.BLOCK_SIZE);

            char[,,] grid = StageBuilder.Instance.GetGridData();
            if (grid[col, height, row] != 'N') break;

            currentPos = belowPos;
        }

        if (currentPos != targetPos)
        {
            StageBuilder.Instance.UpdateGridAtPosition(targetPos, 'N');
            targetPos = currentPos;

            // 何マス落ちるかを計算
            int fallBlocks = Mathf.RoundToInt((transform.position.y - targetPos.y) / StageBuilder.HEIGHT_OFFSET);
            float fallTime = fallBlocks * 1f;

            transform.DOMove(targetPos, fallTime).SetEase(Ease.Linear).OnComplete(() =>
            {
                isMoving = false;
            });
        }
        else
        {
            isMoving = false;
        }
    }
}
