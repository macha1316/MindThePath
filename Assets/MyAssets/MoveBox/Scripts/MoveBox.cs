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

        // Drop until hitting a non-air block (Goalは空気扱いしない)
        TargetPos = StageBuilder.Instance.FindDropPosition(TargetPos, goalIsAir: false);

        isMoving = true;

        Vector3Int targetGrid = new Vector3Int(
            Mathf.RoundToInt(TargetPos.x / StageBuilder.BLOCK_SIZE),
            Mathf.RoundToInt(TargetPos.y / StageBuilder.HEIGHT_OFFSET),
            Mathf.RoundToInt(TargetPos.z / StageBuilder.BLOCK_SIZE)
        );

        transform.DOMove(TargetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            isMoving = false;
        });
    }

    public void OnTurn() { }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdateGridAtPosition(TargetPos, 'M');
    }
}
