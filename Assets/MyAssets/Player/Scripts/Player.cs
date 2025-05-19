using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ITurnBased
{
    public float moveSpeed = 5f;
    private bool isMoving = false;
    private Vector3 targetPosition;
    public Vector3 Direction { get; set; } = Vector3.zero;

    void Start()
    {
        targetPosition = transform.position;
        StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'P');
    }

    void Update()
    {
        if (isMoving)
        {
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
                    if (StageBuilder.Instance.IsAnyMatchingCellType(topPos, 'B', 'M'))
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

                    Vector3 dropPos = afterNext;
                    while (StageBuilder.Instance.IsValidGridPosition(dropPos + Vector3.down * StageBuilder.HEIGHT_OFFSET) &&
                           !StageBuilder.Instance.IsAnyMatchingCellType(dropPos + Vector3.down * StageBuilder.HEIGHT_OFFSET, 'B', 'M', 'P', 'K', 'O'))
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
                    !StageBuilder.Instance.IsAnyMatchingCellType(next, 'B', 'P', 'K', 'O') &&
                    !StageBuilder.Instance.IsAnyMatchingCellType(nextDown, 'P', 'K', 'N', 'O'))
                {
                    canMove = true;
                }
            }

            if (canMove)
            {
                StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'N');
                targetPosition = next;
                isMoving = true;
                transform.forward = Direction;
                CheckGoal();

                StageBuilder.Instance.UpdateGridAtPosition(targetPosition, 'P');
                transform.DOMove(targetPosition, 1f / moveSpeed)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        transform.position = targetPosition;
                        isMoving = false;
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
}
