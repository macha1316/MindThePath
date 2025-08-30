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
    
    // 押し出し先の直下以降に1つでもブロックがあるか
    private bool HasSupportBelow(Vector3 worldPos)
    {
        Vector3 check = worldPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        while (StageBuilder.Instance.IsValidGridPosition(check))
        {
            if (StageBuilder.Instance.GetGridCharType(check) != 'N')
            {
                return true; // 何かしらのブロックがある
            }
            check += Vector3.down * StageBuilder.HEIGHT_OFFSET;
        }
        return false; // 何もなかった
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        targetPosition = transform.position;
        StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'P');
    }

    void Update()
    {
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
                    if (StageBuilder.Instance.IsAnyMatchingCellType(topPos, 'B', 'M', 'F'))
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
                    if (!HasSupportBelow(afterNext)) return;

                    Vector3 dropPos = afterNext;
                    while (StageBuilder.Instance.IsValidGridPosition(dropPos + Vector3.down * StageBuilder.HEIGHT_OFFSET) &&
                           !StageBuilder.Instance.IsAnyMatchingCellType(dropPos + Vector3.down * StageBuilder.HEIGHT_OFFSET, 'B', 'M', 'P', 'O', 'F'))
                    {
                        dropPos += Vector3.down * StageBuilder.HEIGHT_OFFSET;
                    }

                    StageBuilder.Instance.UpdateGridAtPosition(next, 'N');
                    StageBuilder.Instance.UpdateGridAtPosition(dropPos, 'M');

                    foreach (var box in FindObjectsOfType<MoveBox>())
                    {
                        if (Vector3.Distance(box.transform.position, next) < 1f)
                        {
                            box.transform.DOMove(dropPos, 1f / moveSpeed).SetEase(Ease.Linear);
                            box.TargetPos = dropPos;
                            break;
                        }
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
}
