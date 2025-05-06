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
        if (ShouldSkipTurn()) return;

        animator.SetTrigger("walk");
        nextPos = transform.position + transform.forward * 2.0f;

        // === 即時方向転換処理（前方に進めない時） ===
        if (TryHandleImmediateFlip()) return;

        // === 一段下へジャンプする処理 ===
        if (TryHandleJumpDown()) return;
        // === MoveBoxを前に押す処理（条件付き） ===
        if (TryHandleMoveBox()) return;

        Vector3Int targetGrid = StageBuilder.Instance.GridFromPosition(nextPos);
        if (GameManager.Instance.reservedPositions.ContainsKey(targetGrid)) return;

        // === 通常移動処理 ===
        MoveForward();
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
        Vector3Int targetGrid = StageBuilder.Instance.GridFromPosition(nextPos);
        if (GameManager.Instance.reservedPositions.TryGetValue(targetGrid, out var otherPlayer))
        {
            // 他プレイヤーと衝突する場合は折り返す
            if (otherPlayer != this)
            {
                return !TryFlipDirection(ref nextPos);
            }
        }

        // 無効な座標 or ブロックなら折り返し
        if (!StageBuilder.Instance.IsValidGridPosition(nextPos) ||
            StageBuilder.Instance.IsMatchingCellType(nextPos, 'B'))
        {
            return !TryFlipDirection(ref nextPos);
        }

        return false;
    }

    // === 一段下へジャンプする処理 ===
    private bool TryHandleJumpDown()
    {
        Vector3 oneDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        Vector3 twoDown = nextPos + Vector3.down * StageBuilder.HEIGHT_OFFSET * 2;

        if (StageBuilder.Instance.IsValidGridPosition(oneDown) &&
            !StageBuilder.Instance.IsMatchingCellType(oneDown, 'B') &&
            !StageBuilder.Instance.IsMatchingCellType(oneDown, 'M') &&
            !StageBuilder.Instance.IsMatchingCellType(nextPos, 'M'))
        {
            if (!StageBuilder.Instance.IsValidGridPosition(twoDown) ||
                StageBuilder.Instance.IsAnyMatchingCellType(twoDown, 'B', 'M'))
            {
                // 一段だけ空いている → ジャンプ
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

            // 二段とも空いている → ジャンプ不可、折り返す
            TryFlipDirection(ref nextPos);
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
                GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
            });
    }

    // === 方向転換（反転）処理 ===
    public virtual bool TryFlipDirection(ref Vector3 nextPos)
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
                        GameManager.Instance.reservedPositions.Remove(StageBuilder.Instance.GridFromPosition(transform.position));
                    });
                return true;
            }

            // MoveBoxの先がすでに予約されていたら引き返す
            Vector3Int targetGrid = StageBuilder.Instance.GridFromPosition(boxNextPos);
            if (GameManager.Instance.reservedPositions.ContainsKey(targetGrid))
            {
                // MoveBoxの押し出し先がすでに他のキャラに予約されている場合、方向転換して終了
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

    // === ゴール完了状態の取得 ===
    public bool GetIsComplete() => isComplete;
}
