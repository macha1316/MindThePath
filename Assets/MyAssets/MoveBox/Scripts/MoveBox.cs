using UnityEngine;
using DG.Tweening;

public class MoveBox : MonoBehaviour, ITurnBased
{
    private bool isMoving = false;
    private float moveDuration = 1f;
    public Vector3 TargetPos { get; set; }

    void Start()
    {
        TargetPos = transform.position;
    }

    public void SetTargetPos(Vector3 pos)
    {
        TargetPos = pos;
    }

    public void TryPush(Vector3 direction)
    {
        if (isMoving) return;

        TargetPos += direction * StageBuilder.BLOCK_SIZE;

        if (!StageBuilder.Instance.IsValidGridPosition(TargetPos)) return;
        if (!StageBuilder.Instance.IsMatchingCellType(TargetPos, 'N')) return;

        isMoving = true;

        Vector3Int targetGrid = new Vector3Int(
                    Mathf.RoundToInt(TargetPos.x / StageBuilder.BLOCK_SIZE),
                    Mathf.RoundToInt(TargetPos.y / StageBuilder.HEIGHT_OFFSET),
                    Mathf.RoundToInt(TargetPos.z / StageBuilder.BLOCK_SIZE)
                );
        GameManager.Instance.reservedPositions[targetGrid] = this;
        transform.DOMove(TargetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            isMoving = false;
            GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
        });
    }

    public void OnTurn()
    {
        if (isMoving) return;
        // 自らの下を見てNなら毎ターン下がるようにする
        Vector3 oneDown = TargetPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        if (StageBuilder.Instance.IsValidGridPosition(oneDown))
        {
            if (!StageBuilder.Instance.IsMatchingCellType(oneDown, 'B') && !StageBuilder.Instance.IsMatchingCellType(oneDown, 'M'))
            {
                TargetPos = oneDown;
                isMoving = true;
                Vector3Int targetGrid = new Vector3Int(
                    Mathf.RoundToInt(TargetPos.x / StageBuilder.BLOCK_SIZE),
                    Mathf.RoundToInt(TargetPos.y / StageBuilder.HEIGHT_OFFSET),
                    Mathf.RoundToInt(TargetPos.z / StageBuilder.BLOCK_SIZE)
                );
                GameManager.Instance.reservedPositions[targetGrid] = this;
                transform.DOMove(TargetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
                {
                    isMoving = false;
                    GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
                });
            }
        }
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdateGridAtPosition(TargetPos, 'M');
    }
}
