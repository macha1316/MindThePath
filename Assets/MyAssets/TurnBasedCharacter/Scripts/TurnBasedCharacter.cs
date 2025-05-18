using UnityEngine;
using DG.Tweening;

public class TurnbsedCharacter : MonoBehaviour, ITurnBased
{
    // === 基本ステータス ===
    private float moveDuration = 1f;
    private bool isMoving = false;
    protected bool isComplete = false;
    private Animator animator;
    protected Vector3 nextPos;

    // === 初期化 ===
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // === ターン開始時の処理 ===
    public void OnTurn()
    {
        if (GameManager.Instance.Is2DMode)
        {
            Vector3 forwardPos = transform.position + transform.forward * 2.0f;

            if (!StageBuilder.Instance.IsValidGridPosition(forwardPos))
            {
                transform.forward = -transform.forward;
                Vector3 flippedPos = transform.position + transform.forward * 2.0f;

                if (!StageBuilder.Instance.IsValidGridPosition(flippedPos))
                {
                    animator.SetTrigger("idle");
                    return;
                }

                Vector3 topPos1 = StageBuilder.Instance.GetTopCellPosition(flippedPos);

                if (StageBuilder.Instance.IsAnyMatchingCellType(topPos1, 'B', 'M'))
                {
                    animator.SetTrigger("walk");
                    nextPos = topPos1 + Vector3.up * StageBuilder.HEIGHT_OFFSET;
                    MoveForward();
                    return;
                }
                animator.SetTrigger("idle");
                return;
            }

            Vector3 topPos = StageBuilder.Instance.GetTopCellPosition(forwardPos);
            if (TryHandleImmediateFlipFor2D(topPos)) return;

            if (StageBuilder.Instance.IsMatchingCellType(topPos, 'B') ||
                StageBuilder.Instance.IsMatchingCellType(topPos, 'M'))
            {
                animator.SetTrigger("walk");
                nextPos = topPos + Vector3.up * StageBuilder.HEIGHT_OFFSET;
                MoveForward();
                return;
            }

            animator.SetTrigger("idle");
            return;
        }
        else
        {
            if (ShouldSkipTurn()) return;

            animator.SetTrigger("walk");
            nextPos = transform.position + transform.forward * 2.0f;

            // === 即時方向転換処理（前方に進めない時） ===
            if (TryHandleImmediateFlip()) return;

            // === MoveBoxを前に押す処理（条件付き） ===
            if (TryHandleMoveBox()) return;

            // === 通常移動処理 ===
            MoveForward();
        }

    }

    // === ターンをスキップすべきかの判定 ===
    private bool ShouldSkipTurn()
    {
        if (isMoving || isComplete)
        {
            animator.SetTrigger("idle");
            return true;
        }
        return false;
    }

    // === 即時方向転換処理（前方に進めない時） ===
    private bool TryHandleImmediateFlip()
    {
        // 無効な座標 or ブロックなら折り返し
        if (!StageBuilder.Instance.IsValidGridPosition(nextPos) ||
            StageBuilder.Instance.IsAnyMatchingCellType(nextPos, 'B', 'P'))
        {
            return !TryFlipDirection(ref nextPos);
        }

        // === N段の空間が続いていたら引き返す ===
        if (StageBuilder.Instance.IsMatchingCellType(nextPos, 'N'))
        {
            Vector3 belowNext = nextPos + Vector3.down * StageBuilder.BLOCK_SIZE;
            if (StageBuilder.Instance.IsMatchingCellType(belowNext, 'N'))
            {
                return !TryFlipDirection(ref nextPos);
            }
        }

        return false;
    }

    private bool TryHandleImmediateFlipFor2D(Vector3 topPos)
    {
        if (StageBuilder.Instance.IsAnyMatchingCellType(topPos, 'P', 'K'))
        {
            transform.forward = -transform.forward;
            Vector3 flippedPos = transform.position + transform.forward * 2.0f;

            if (!StageBuilder.Instance.IsValidGridPosition(flippedPos)) return true;

            Vector3 newTopPos = StageBuilder.Instance.GetTopCellPosition(flippedPos);
            if (StageBuilder.Instance.IsAnyMatchingCellType(newTopPos, 'B', 'M'))
            {
                animator.SetTrigger("walk");
                nextPos = newTopPos + Vector3.up * StageBuilder.HEIGHT_OFFSET;
                MoveForward();
                return true;
            }

            animator.SetTrigger("idle");
            return true;
        }
        return false;
    }

    // === 通常移動処理 ===
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
            });
    }

    // === 方向転換（反転）処理 ===
    public virtual bool TryFlipDirection(ref Vector3 nextPos)
    {
        transform.forward = -transform.forward;
        nextPos = transform.position + transform.forward * 2.0f;
        if (StageBuilder.Instance.IsValidGridPosition(nextPos))
        {
            if (StageBuilder.Instance.IsAnyMatchingCellType(nextPos, 'B', 'P'))
            {
                return false;
            }
            return true;
        }
        return false;
    }

    public bool TryFlipDirectionFor2D(ref Vector3 nextPos)
    {
        transform.forward = -transform.forward;
        Vector3 flippedPos = transform.position + transform.forward * 2.0f;

        if (!StageBuilder.Instance.IsValidGridPosition(flippedPos)) return true;

        Vector3 topPos = StageBuilder.Instance.GetTopCellPosition(flippedPos);

        if (StageBuilder.Instance.IsAnyMatchingCellType(topPos, 'K', 'P'))
        {
            nextPos = topPos + Vector3.up * StageBuilder.HEIGHT_OFFSET;
            return true;
        }
        return false;
    }

    // === 動的セルの向きに応じて進行方向を更新 ===
    private void UpdateForwardFromDynamic()
    {
        if (IsMatchingDynamicCellType(nextPos, 'U')) transform.forward = Vector3.forward;
        if (IsMatchingDynamicCellType(nextPos, 'D')) transform.forward = Vector3.back;
        if (IsMatchingDynamicCellType(nextPos, 'R')) transform.forward = Vector3.right;
        if (IsMatchingDynamicCellType(nextPos, 'L')) transform.forward = Vector3.left;
    }

    // === ゴール到達時の処理 ===
    public virtual void CheckGoal()
    {
        if (StageBuilder.Instance.IsMatchingCellType(nextPos, 'G'))
        {
            StageSelectUI.Instance.SaveClearedStage(StageBuilder.Instance.stageNumber + 1);
            DOVirtual.DelayedCall(Time.timeScale == 1f ? 1f : 0.5f, () => isComplete = true);
        }
    }

    // === MoveBoxを前に押す処理（条件付き） ===
    private bool TryHandleMoveBox()
    {
        // プレイヤーの正面マスを取得
        Vector3 frontPos = transform.position + transform.forward * 2.0f;

        // グリッド外 or 無効な位置なら何もしない
        if (!StageBuilder.Instance.IsValidGridPosition(frontPos)) return false;

        // そこにMoveBox('M')がなければ終了
        if (StageBuilder.Instance.GetGridCharType(frontPos) != 'M') return false;

        // 全てのMoveBoxを走査して、該当するBoxを見つける
        foreach (var box in FindObjectsOfType<MoveBox>())
        {
            // Grid座標が一致しないBoxはスキップ
            if (StageBuilder.Instance.GridFromPosition(box.transform.position) != StageBuilder.Instance.GridFromPosition(frontPos)) continue;

            // 押し出し先・上下の座標を計算
            Vector3 boxNextPos = box.transform.position + transform.forward * StageBuilder.BLOCK_SIZE;
            Vector3 nextDownPos = nextPos - Vector3.up * StageBuilder.BLOCK_SIZE;
            Vector3 nextUpPos = nextPos + Vector3.up * StageBuilder.BLOCK_SIZE;

            // 移動先が無効 または 通行不可のセル または 障害物が上下にある場合は方向転換
            if (!StageBuilder.Instance.IsValidGridPosition(boxNextPos) ||
                !StageBuilder.Instance.IsMatchingCellType(boxNextPos, 'N') ||
                StageBuilder.Instance.IsMatchingCellType(nextDownPos, 'N') ||
                StageBuilder.Instance.IsAnyMatchingCellType(nextUpPos, 'P', 'K', 'M'))
            {
                // 折り返して止まる
                if (!TryFlipDirection(ref nextPos)) return true;

                isMoving = true;
                CheckGoal();
                transform.DOMove(nextPos, moveDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        isMoving = false;
                        UpdateForwardFromDynamic();
                    });
                return true;
            }
            // Boxを前に押す（Box側が動く）
            box.TryPush(transform.forward);
            break;
        }

        // Box押しで特別な処理はなし
        return false;
    }

    // === 動的グリッドとの一致判定 ===
    private bool IsMatchingDynamicCellType(Vector3 pos, char cellType)
    {
        return StageBuilder.Instance.GetDynamicGridCharType(pos) == cellType;
    }

    // === グリッドデータを更新する処理（基本はP） ===
    public virtual void UpdateGridData()
    {
        if (isComplete) return;

        if (!StageBuilder.Instance.IsValidGridPosition(nextPos) ||
            StageBuilder.Instance.IsAnyMatchingCellType(nextPos, 'B', 'P', 'K'))
        {
            StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'P');
            return;
        }

        StageBuilder.Instance.UpdateGridAtPosition(nextPos, 'P');
    }
}
