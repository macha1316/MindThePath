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
    }

    public void OnTurn()
    {
        if (isMoving || isComplete)
        {
            animator.SetTrigger("idle");
            return;
        }
        else
        {
            animator.SetTrigger("walk");
        }

        Vector3 nextPos = transform.position + transform.forward * 2.0f;

        // 範囲外 or ブロック → すぐ折り返し
        if (!IsValidPosition(nextPos) || IsMatchingCellType(nextPos, 'B'))
        {
            transform.forward = -transform.forward;
            nextPos = transform.position + transform.forward * 2.0f;
        }

        // 1段下の位置を計算
        Vector3 oneDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        Vector3 twoDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET * 2;

        // 下に下がれる場合の処理
        if (IsValidPosition(oneDown) && !IsMatchingCellType(oneDown, 'B'))
        {
            if (!IsValidPosition(twoDown) || IsMatchingCellType(twoDown, 'B'))
            {
                // 1段だけ下なら進む
                animator.SetTrigger("jump");
                isMoving = true;

                transform.DOJump(oneDown, 2f, 1, moveDuration)
                    .SetEase(Ease.OutQuad)
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
                        }
                    });
                return;
            }
            // 2段以上空いてるので反転
            transform.forward = -transform.forward;
            nextPos = transform.position + transform.forward * 2.0f;
        }

        isMoving = true;

        // リファクタいる
        Vector3 frontPos = transform.position + transform.forward * 2.0f;
        int col = Mathf.RoundToInt(frontPos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(frontPos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(frontPos.z / StageBuilder.BLOCK_SIZE);

        char[,,] grid = StageBuilder.Instance.GetGridData();
        if (StageBuilder.Instance.IsValidGridPosition(frontPos) && grid[col, height, row] == 'M')
        {
            // foreach回す必要ある？
            foreach (var box in FindObjectsOfType<MoveBox>())
            {
                Vector3 boxPos = box.transform.position;
                int bCol = Mathf.RoundToInt(boxPos.x / StageBuilder.BLOCK_SIZE);
                int bHeight = Mathf.RoundToInt(boxPos.y / StageBuilder.HEIGHT_OFFSET);
                int bRow = Mathf.RoundToInt(boxPos.z / StageBuilder.BLOCK_SIZE);

                if (bCol == col && bHeight == height && bRow == row)
                {
                    // onTurnでする
                    box.TryPush(transform.forward);
                    break;
                }
            }
        }

        // 現在位置に基づいての行動
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
