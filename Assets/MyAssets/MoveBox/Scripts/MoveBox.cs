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
            isMoving = false;
        });
    }

    public void OnTurn()
    {
        // Do nothing by default
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdateGridAtPosition(targetPos, 'M');
    }
}
