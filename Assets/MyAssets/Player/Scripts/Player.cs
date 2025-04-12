using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ITurnBased
{
    private float moveDuration = 1f;
    private bool isMoving = false;
    private bool isComplete = false;
    private Animator animator;
    private Vector3 nextPos;

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

        nextPos = transform.position + transform.forward * 2.0f;

        // 範囲外 or ブロック → すぐ折り返し
        if (!StageBuilder.Instance.IsValidGridPosition(nextPos) ||
            IsMatchingCellType(nextPos, 'B') ||
            IsMatchingCellType(nextPos, 'P'))
        {
            if (!TryFlipDirection(ref nextPos))
            {
                return;
            }
        }

        if (HandleMoveBoxIfPresent(ref nextPos)) return;

        // 1段下の位置を計算
        Vector3 oneDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        Vector3 twoDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET * 2;

        // 下に下がれる場合の処理
        if (StageBuilder.Instance.IsValidGridPosition(oneDown) &&
            !IsMatchingCellType(oneDown, 'B') &&
            !IsMatchingCellType(oneDown, 'M'))
        {
            if (!StageBuilder.Instance.IsValidGridPosition(twoDown) ||
                IsMatchingCellType(twoDown, 'B') ||
                IsMatchingCellType(twoDown, 'M'))
            {
                // 1段だけ下なら進む
                animator.SetTrigger("jump");
                isMoving = true;
                nextPos = oneDown;

                transform.DOJump(nextPos, 2f, 1, moveDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        isMoving = false;
                        UpdateForwardFromDynamic();
                        CheckGoal();
                    });
                return;
            }
            // 2段以上空いてるので反転
            TryFlipDirection(ref nextPos);
        }

        isMoving = true;

        // 現在位置に基づいての行動
        transform.DOMove(nextPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                isMoving = false;
                UpdateForwardFromDynamic();
                CheckGoal();
            });
    }

    // 共通関数にすると思う(StageBuilder?)
    private bool TryFlipDirection(ref Vector3 nextPos)
    {
        transform.forward = -transform.forward;
        nextPos = transform.position + transform.forward * 2.0f;
        if (StageBuilder.Instance.IsValidGridPosition(nextPos))
        {
            if (IsMatchingCellType(nextPos, 'B') || IsMatchingCellType(nextPos, 'P'))
            {
                return false;
            }
            return true;
        }
        return false;
    }

    // 共通関数にすると思う(StageBuilder?)
    private void UpdateForwardFromDynamic()
    {
        if (IsMatchingDynamicCellType(transform.position, 'U')) transform.forward = Vector3.forward;
        if (IsMatchingDynamicCellType(transform.position, 'D')) transform.forward = Vector3.back;
        if (IsMatchingDynamicCellType(transform.position, 'R')) transform.forward = Vector3.right;
        if (IsMatchingDynamicCellType(transform.position, 'L')) transform.forward = Vector3.left;
    }

    private void CheckGoal()
    {
        if (IsMatchingCellType(transform.position, 'G')) isComplete = true;
    }

    private bool HandleMoveBoxIfPresent(ref Vector3 nextPos)
    {
        Vector3 frontPos = transform.position + transform.forward * 2.0f;
        int col = Mathf.RoundToInt(frontPos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(frontPos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(frontPos.z / StageBuilder.BLOCK_SIZE);

        if (!StageBuilder.Instance.IsValidGridPosition(frontPos)) return false;

        char[,,] grid = StageBuilder.Instance.GetGridData();
        if (grid[col, height, row] != 'M') return false;

        foreach (var box in FindObjectsOfType<MoveBox>())
        {
            Vector3 boxPos = box.transform.position;
            int bCol = Mathf.RoundToInt(boxPos.x / StageBuilder.BLOCK_SIZE);
            int bHeight = Mathf.RoundToInt(boxPos.y / StageBuilder.HEIGHT_OFFSET);
            int bRow = Mathf.RoundToInt(boxPos.z / StageBuilder.BLOCK_SIZE);

            if (bCol == col && bHeight == height && bRow == row)
            {
                Vector3 boxNextPos = boxPos + transform.forward * StageBuilder.BLOCK_SIZE;
                if (!StageBuilder.Instance.IsValidGridPosition(boxNextPos) || !IsMatchingCellType(boxNextPos, 'N'))
                {
                    if (!TryFlipDirection(ref nextPos)) return true;

                    isMoving = true;
                    transform.DOMove(nextPos, moveDuration)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            isMoving = false;
                            UpdateForwardFromDynamic();
                            CheckGoal();
                        });
                    return true;
                }

                box.TryPush(transform.forward);
                break;
            }
        }
        return false;
    }

    // 共通関数にすると思う(StageBuilder?)
    private bool IsMatchingCellType(Vector3 pos, char cellType)
    {
        int col = Mathf.RoundToInt(pos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / StageBuilder.BLOCK_SIZE);

        return StageBuilder.Instance.GetGridData()[col, height, row] == cellType;
    }

    // 共通関数にすると思う(StageBuilder?)
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

        if (!StageBuilder.Instance.IsValidGridPosition(nextPos) ||
            IsMatchingCellType(nextPos, 'B') ||
            IsMatchingCellType(nextPos, 'P'))
        {
            return;
        }

        // 自分の現在位置をグリッドとして取得して N にする
        int col = Mathf.RoundToInt(transform.position.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(transform.position.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(transform.position.z / StageBuilder.BLOCK_SIZE);

        StageBuilder.Instance.GetGridData()[col, height, row] = 'N';

        StageBuilder.Instance.UpdateGridAtPosition(nextPos, 'P');
    }

    public bool GetIsComplete() => isComplete;
}
