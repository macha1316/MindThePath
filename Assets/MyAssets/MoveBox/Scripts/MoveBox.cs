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

    public void SetTargetPos(Vector3 pos)
    {
        targetPos = pos;
    }

    public void TryPush(Vector3 direction)
    {
        if (isMoving) return;

        targetPos += direction * StageBuilder.BLOCK_SIZE;

        if (!StageBuilder.Instance.IsValidGridPosition(targetPos)) return;
        if (!StageBuilder.Instance.IsMatchingCellType(targetPos, 'N')) return;

        isMoving = true;

        Vector3Int targetGrid = new Vector3Int(
                    Mathf.RoundToInt(targetPos.x / StageBuilder.BLOCK_SIZE),
                    Mathf.RoundToInt(targetPos.y / StageBuilder.HEIGHT_OFFSET),
                    Mathf.RoundToInt(targetPos.z / StageBuilder.BLOCK_SIZE)
                );
        GameManager.Instance.reservedPositions[targetGrid] = this;
        transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            isMoving = false;
            GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
        });
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
                Vector3Int targetGrid = new Vector3Int(
                    Mathf.RoundToInt(targetPos.x / StageBuilder.BLOCK_SIZE),
                    Mathf.RoundToInt(targetPos.y / StageBuilder.HEIGHT_OFFSET),
                    Mathf.RoundToInt(targetPos.z / StageBuilder.BLOCK_SIZE)
                );
                GameManager.Instance.reservedPositions[targetGrid] = this;
                transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
                {
                    isMoving = false;
                    GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
                });
            }
        }
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdateGridAtPosition(targetPos, 'M');
    }
}
