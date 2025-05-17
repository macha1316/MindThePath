using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ITurnBased
{
    public float moveSpeed = 5f;
    private bool isMoving = false;
    private Vector3 targetPosition;

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

        Vector3 direction = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.W)) direction = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S)) direction = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.A)) direction = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D)) direction = Vector3.right;

        if (direction != Vector3.zero)
        {
            Vector3 next = transform.position + direction * StageBuilder.HEIGHT_OFFSET;

            bool canMove = false;

            if (GameManager.Instance.Is2DMode)
            {
                Vector3 topPos = StageBuilder.Instance.GetTopCellPosition(next);
                if (StageBuilder.Instance.IsAnyMatchingCellType(topPos, 'B', 'M'))
                {
                    next = topPos + Vector3.up * StageBuilder.HEIGHT_OFFSET;
                    canMove = true;
                }
            }
            else
            {
                Vector3 nextDown = next + Vector3.down * StageBuilder.HEIGHT_OFFSET;
                if (StageBuilder.Instance.IsMatchingCellType(next, 'M'))
                {
                    Vector3 afterNext = next + direction * StageBuilder.HEIGHT_OFFSET;

                    Vector3 dropPos = afterNext;
                    while (StageBuilder.Instance.IsValidGridPosition(dropPos + Vector3.down * StageBuilder.HEIGHT_OFFSET) &&
                           !StageBuilder.Instance.IsAnyMatchingCellType(dropPos + Vector3.down * StageBuilder.HEIGHT_OFFSET, 'B', 'M', 'P', 'K'))
                    {
                        dropPos += Vector3.down * StageBuilder.HEIGHT_OFFSET;
                    }

                    StageBuilder.Instance.UpdateGridAtPosition(next, 'N');
                    StageBuilder.Instance.UpdateGridAtPosition(dropPos, 'M');

                    foreach (var box in FindObjectsOfType<MoveBox>())
                    {
                        if (Vector3.Distance(box.transform.position, next) < 0.1f)
                        {
                            box.transform.DOMove(dropPos, 1f / moveSpeed).SetEase(Ease.Linear);
                            box.TargetPos = dropPos;
                            break;
                        }
                    }

                    canMove = true;
                    return;
                }
                else if (StageBuilder.Instance.IsValidGridPosition(next) &&
                    !StageBuilder.Instance.IsAnyMatchingCellType(next, 'B', 'P', 'K') &&
                    !StageBuilder.Instance.IsAnyMatchingCellType(nextDown, 'P', 'K', 'N'))
                {
                    canMove = true;
                }
            }

            if (canMove)
            {
                StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'N');
                targetPosition = next;
                isMoving = true;
                transform.forward = direction;

                transform.DOMove(targetPosition, 1f / moveSpeed)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        transform.position = targetPosition;
                        isMoving = false;
                        StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'P');
                    });
            }
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
