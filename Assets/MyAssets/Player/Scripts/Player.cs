using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ITurnBased
{
    public float moveSpeed = 5f;
    private Animator animator;
    private bool isMoving = false;
    private Vector3 targetPosition;
    public Vector3 Direction { get; set; } = Vector3.zero;
    private Vector3 lastSupportPos; // 直前に立っていた床（1段下）の座標

    void Start()
    {
        animator = GetComponent<Animator>();
        targetPosition = transform.position;
        StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'P');
    }

    void Update()
    {
        // Undo (keyboard): Z — only when not moving
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (!isMoving)
            {
                UndoManager.Instance?.Undo();
            }
            return;
        }

        if (isMoving)
        {
            if (animator != null) animator.SetTrigger("Idle");
            return;
        }

        if (Input.GetKeyDown(KeyCode.W)) Direction = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S)) Direction = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.A)) Direction = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D)) Direction = Vector3.right;

        if (Direction != Vector3.zero)
        {
            Vector3 next = transform.position + Direction * StageBuilder.HEIGHT_OFFSET;

            bool canMove = false;

            if (GameManager.Instance.Is2DMode)
            {
                if (StageBuilder.Instance.IsValidGridPosition(next))
                {
                    Vector3 topPos = StageBuilder.Instance.GetTopCellPosition(next);
                    if (StageBuilder.Instance.IsAnyMatchingCellType(topPos, 'B', 'M', 'F', 'A'))
                    {
                        next = topPos + Vector3.up * StageBuilder.HEIGHT_OFFSET;
                        canMove = true;
                    }
                }
            }
            else
            {
                if (!StageBuilder.Instance.IsValidGridPosition(next)) return;
                Vector3 nextDown = next + Vector3.down * StageBuilder.HEIGHT_OFFSET;

                if (StageBuilder.Instance.IsMatchingCellType(next, 'M'))
                {
                    Vector3 afterNext = next + Direction * StageBuilder.HEIGHT_OFFSET;
                    if (!StageBuilder.Instance.IsValidGridPosition(afterNext)) return;
                    if (!StageBuilder.Instance.IsMatchingCellType(afterNext, 'N')) return;
                    if (StageBuilder.Instance.IsMatchingCellType(nextDown, 'O')) return;

                    // 押し出し先の直下に一つもブロックが無い場合は押せない
                    if (!StageBuilder.Instance.HasAnySupportBelow(afterNext)) return;

                    // 既存挙動に合わせて、Goal('G')は空気として扱い更に下まで落下
                    Vector3 dropPos = StageBuilder.Instance.FindDropPosition(afterNext, goalIsAir: true);

                    StageBuilder.Instance.UpdateGridAtPosition(next, 'N');
                    StageBuilder.Instance.UpdateGridAtPosition(dropPos, 'M');

                    if (StageBuilder.Instance.TryGetMoveBoxAtPosition(next, out var box))
                    {
                        box.transform.DOMove(dropPos, 1f / moveSpeed)
                            .SetEase(Ease.Linear)
                            .OnComplete(() =>
                            {
                                box.transform.position = dropPos;
                                box.TeleportIfOnPortal();
                            });
                        box.TargetPos = dropPos;
                    }

                    canMove = true;
                }
                else if (StageBuilder.Instance.IsValidGridPosition(next) &&
                    !StageBuilder.Instance.IsAnyMatchingCellType(next, 'B', 'P', 'O') &&
                    !StageBuilder.Instance.IsAnyMatchingCellType(nextDown, 'P', 'N', 'O'))
                {
                    canMove = true;
                }
            }

            if (canMove)
            {
                // Snapshot current state for Undo
                UndoManager.Instance?.Record();

                // 今立っている床（1段下）を記録（移動後に消滅処理するため）
                lastSupportPos = transform.position + Vector3.down * StageBuilder.HEIGHT_OFFSET;
                StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'N');
                targetPosition = next;
                isMoving = true;
                transform.forward = Direction;
                CheckGoal();

                if (animator != null) animator.SetTrigger("Walk");

                StageBuilder.Instance.UpdateGridAtPosition(targetPosition, 'P');
                transform.DOMove(targetPosition, 1f / moveSpeed)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        transform.position = targetPosition;
                        isMoving = false;
                        HandleFragileFloorDisappear();
                        HandleTeleport();
                    });
            }
            Direction = Vector3.zero;
        }
    }

    private void CheckGoal()
    {
        if (StageBuilder.Instance.IsMatchingCellType(targetPosition, 'G'))
        {
            GameManager.Instance.IsGameClear = true;
            StageSelectUI.Instance.SetClearUI();
        }
    }

    public void OnTurn()
    {
        // Implement turn-based logic here if needed
    }
    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdateGridAtPosition(targetPosition, 'P');
    }

    // Teleport instantly (used by Undo)
    public void TeleportTo(Vector3 pos)
    {
        isMoving = false;
        targetPosition = pos;
        transform.position = pos;
    }

    private void HandleFragileFloorDisappear()
    {
        // 直前に乗っていた足場が消える床(F)なら、破壊してグリッドを'N'にする
        if (!StageBuilder.Instance.IsValidGridPosition(lastSupportPos)) return;
        if (!StageBuilder.Instance.IsMatchingCellType(lastSupportPos, 'F')) return;

        // グリッド更新
        StageBuilder.Instance.UpdateGridAtPosition(lastSupportPos, 'N');

        // 対応するFragileBlockオブジェクトを探して破棄
        var lastGrid = StageBuilder.Instance.GridFromPosition(lastSupportPos);
        foreach (var frag in GameObject.FindObjectsOfType<FragileBlock>())
        {
            var g = StageBuilder.Instance.GridFromPosition(frag.transform.position);
            if (g == lastGrid)
            {
                // 演出して消す
                var t = frag.transform;
                t.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    if (frag != null) Destroy(frag.gameObject);
                });
                break;
            }
        }
    }

    private void HandleTeleport()
    {
        // プレイヤーの足元(1段下)がテレポート('A')なら、相方の'A'へ瞬間移動
        Vector3 support = targetPosition + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        if (!StageBuilder.Instance.IsValidGridPosition(support)) return;
        if (!StageBuilder.Instance.IsMatchingCellType(support, 'A')) return;

        var fromCell = StageBuilder.Instance.GridFromPosition(support);
        if (StageBuilder.Instance.TryFindOtherCell('A', fromCell, out var otherCell))
        {
            Vector3 dest = StageBuilder.Instance.WorldFromGrid(otherCell) + Vector3.up * StageBuilder.HEIGHT_OFFSET;
            if (!StageBuilder.Instance.IsValidGridPosition(dest)) return;

            char occ = StageBuilder.Instance.GetGridCharType(dest);
            if (occ == 'M')
            {
                // 目的地に箱がいる場合は、押せるなら押してからテレポート
                Vector3 afterNext = dest + transform.forward * StageBuilder.HEIGHT_OFFSET;
                if (!StageBuilder.Instance.IsValidGridPosition(afterNext)) return; // 押せないのでTP中止
                if (!StageBuilder.Instance.IsMatchingCellType(afterNext, 'N')) return; // 押せない
                if (!StageBuilder.Instance.HasAnySupportBelow(afterNext)) return; // 支えがない

                Vector3 dropPos = StageBuilder.Instance.FindDropPosition(afterNext, goalIsAir: true);

                // グリッド更新（箱を先に動かす）
                StageBuilder.Instance.UpdateGridAtPosition(dest, 'N');
                StageBuilder.Instance.UpdateGridAtPosition(dropPos, 'M');

                if (StageBuilder.Instance.TryGetMoveBoxAtPosition(dest, out var boxAtDest))
                {
                    boxAtDest.transform.DOMove(dropPos, 1f / moveSpeed)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            boxAtDest.transform.position = dropPos;
                            boxAtDest.TargetPos = dropPos;
                            boxAtDest.TeleportIfOnPortal();
                        });
                }
            }
            else if (occ != 'N')
            {
                // 目的地が空でない場合はテレポートしない
                return;
            }

            // グリッド更新: 現在位置を空に、目的地をプレイヤーに
            StageBuilder.Instance.UpdateGridAtPosition(targetPosition, 'N');
            StageBuilder.Instance.UpdateGridAtPosition(dest, 'P');

            // 瞬間移動
            transform.position = dest;
            targetPosition = dest;
        }
    }
}
