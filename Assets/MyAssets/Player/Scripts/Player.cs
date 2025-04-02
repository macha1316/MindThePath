using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ITurnBased
{
    private float moveDuration = 1f;
    private bool isMoving = false;
    private bool isComplete = false;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        // とりまここ
        animator.SetTrigger("walk");
    }

    public void OnTurn()
    {
        if (isMoving || isComplete) return;

        Vector3 nextPos = transform.position + transform.forward * 2.0f;

        // 範囲外 or ブロック → 反転
        if (!IsValidPosition(nextPos) || IsMatchingCellType(nextPos, 'B'))
        {
            transform.forward = -transform.forward;
            nextPos = transform.position + transform.forward * 2.0f;
        }

        Vector3 oneDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        Vector3 twoDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET * 2;

        if (IsValidPosition(oneDown) && !IsMatchingCellType(oneDown, 'B'))
        {
            if (!IsValidPosition(twoDown) || IsMatchingCellType(twoDown, 'B'))
            {
                // 1段だけ下なら進む
                nextPos = oneDown;
            }
            else
            {
                // 2段以上空いてるので反転
                transform.forward = -transform.forward;
                nextPos = transform.position + transform.forward * 2.0f;
            }
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
                    isComplete = true;
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
        if (isComplete) return;
        StageBuilder.Instance.UpdatePlayerPosition(this);
    }

    public bool GetIsComplete() => isComplete;
}
