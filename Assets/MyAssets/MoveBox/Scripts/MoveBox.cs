using UnityEngine;
using DG.Tweening;

public class MoveBox : MonoBehaviour, ITurnBased
{
    private bool isMoving = false;
    private float moveDuration = 1f;
    private Vector3 targetPos;

    void Start()
    {
        targetPos = transform.position;
    }

    public void TryPush(Vector3 direction)
    {
        if (isMoving) return;

        targetPos += direction * StageBuilder.BLOCK_SIZE;

        if (!StageBuilder.Instance.IsValidGridPosition(targetPos)) return;
        if (!StageBuilder.Instance.IsMatchingCellType(targetPos, 'N')) return;

        isMoving = true;
        transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            isMoving = false;
        });
        return;
    }

    public void OnTurn()
    {
        if (isMoving) return;
        // 自らの下を見てNなら毎ターン下がるようにする
        Vector3 oneDown = targetPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        if (StageBuilder.Instance.IsValidGridPosition(oneDown))
        {
            if (!StageBuilder.Instance.IsMatchingCellType(oneDown, 'B') && !StageBuilder.Instance.IsMatchingCellType(oneDown, 'M'))
            {
                targetPos = oneDown;
                isMoving = true;
                transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
                {
                    isMoving = false;
                });
            }
        }
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdateGridAtPosition(targetPos, 'M');
    }
}
