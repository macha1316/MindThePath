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
        if (ShouldSkipTurn()) return;

        animator.SetTrigger("walk");
        nextPos = transform.position + transform.forward * 2.0f;

        if (TryHandleImmediateFlip()) return;
        if (TryHandleMoveBox()) return;
        if (TryHandleJumpDown()) return;

        Vector3Int targetGrid = new Vector3Int(
            Mathf.RoundToInt(nextPos.x / StageBuilder.BLOCK_SIZE),
            Mathf.RoundToInt(nextPos.y / StageBuilder.HEIGHT_OFFSET),
            Mathf.RoundToInt(nextPos.z / StageBuilder.BLOCK_SIZE)
        );

        if (GameManager.Instance.reservedPositions.ContainsKey(targetGrid)) return;
        GameManager.Instance.reservedPositions[targetGrid] = this;

        MoveForward();
    }

    private bool ShouldSkipTurn()
    {
        if (isMoving || isComplete)
        {
            animator.SetTrigger("idle");
            return true;
        }
        return false;
    }

    private bool TryHandleImmediateFlip()
    {
        Vector3Int targetGrid = new Vector3Int(
            Mathf.RoundToInt(nextPos.x / StageBuilder.BLOCK_SIZE),
            Mathf.RoundToInt(nextPos.y / StageBuilder.HEIGHT_OFFSET),
            Mathf.RoundToInt(nextPos.z / StageBuilder.BLOCK_SIZE)
        );

        if (GameManager.Instance.reservedPositions.TryGetValue(targetGrid, out var otherPlayer))
        {
            if (otherPlayer != this)
            {
                return !TryFlipDirection(ref nextPos); // 即折り返す
            }
        }

        if (!StageBuilder.Instance.IsValidGridPosition(nextPos) ||
            StageBuilder.Instance.IsMatchingCellType(nextPos, 'B'))
        {
            return !TryFlipDirection(ref nextPos);
        }

        return false;
    }

    private bool TryHandleJumpDown()
    {
        Vector3 oneDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        Vector3 twoDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET * 2;

        if (StageBuilder.Instance.IsValidGridPosition(oneDown) &&
            !StageBuilder.Instance.IsMatchingCellType(oneDown, 'B') &&
            !StageBuilder.Instance.IsMatchingCellType(oneDown, 'M'))
        {
            if (!StageBuilder.Instance.IsValidGridPosition(twoDown) ||
                StageBuilder.Instance.IsMatchingCellType(twoDown, 'B') ||
                StageBuilder.Instance.IsMatchingCellType(twoDown, 'M'))
            {
                animator.SetTrigger("jump");
                isMoving = true;
                nextPos = oneDown;

                CheckGoal();
                transform.DOJump(nextPos, 2f, 1, moveDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        isMoving = false;
                        UpdateForwardFromDynamic();
                        GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
                    });
                return true;
            }

            TryFlipDirection(ref nextPos);
        }
        return false;
    }

    private void MoveForward()
    {
        isMoving = true;
        CheckGoal();
        transform.DOMove(nextPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                isMoving = false;
                UpdateForwardFromDynamic();
                GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
            });
    }

    private bool TryFlipDirection(ref Vector3 nextPos)
    {
        transform.forward = -transform.forward;
        nextPos = transform.position + transform.forward * 2.0f;
        if (StageBuilder.Instance.IsValidGridPosition(nextPos))
        {
            if (StageBuilder.Instance.IsMatchingCellType(nextPos, 'B'))
            {
                return false;
            }
            return true;
        }
        return false;
    }

    private void UpdateForwardFromDynamic()
    {
        if (IsMatchingDynamicCellType(nextPos, 'U')) transform.forward = Vector3.forward;
        if (IsMatchingDynamicCellType(nextPos, 'D')) transform.forward = Vector3.back;
        if (IsMatchingDynamicCellType(nextPos, 'R')) transform.forward = Vector3.right;
        if (IsMatchingDynamicCellType(nextPos, 'L')) transform.forward = Vector3.left;
    }

    private void CheckGoal()
    {
        if (StageBuilder.Instance.IsMatchingCellType(nextPos, 'G'))
        {
            // 臨時処理, ちゃんとGridで判定したい
            StageSelectUI.Instance.SaveClearedStage(StageBuilder.Instance.stageNumber + 1);
            DOVirtual.DelayedCall(Time.timeScale == 1f ? 1f : 0.5f, () => isComplete = true);
        }
    }

    private bool TryHandleMoveBox()
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
                if (!StageBuilder.Instance.IsValidGridPosition(boxNextPos) || !StageBuilder.Instance.IsMatchingCellType(boxNextPos, 'N'))
                {
                    if (!TryFlipDirection(ref nextPos)) return true;

                    isMoving = true;
                    CheckGoal();
                    transform.DOMove(nextPos, moveDuration)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            isMoving = false;
                            UpdateForwardFromDynamic();
                            GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
                        });
                    return true;
                }

                box.TryPush(transform.forward);
                break;
            }
        }
        return false;
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

        if (!StageBuilder.Instance.IsValidGridPosition(nextPos) ||
            StageBuilder.Instance.IsMatchingCellType(nextPos, 'B') ||
            StageBuilder.Instance.IsMatchingCellType(nextPos, 'P'))
        {
            return;
        }

        StageBuilder.Instance.UpdateGridAtPosition(nextPos, 'P');
    }

    public bool GetIsComplete() => isComplete;
}
