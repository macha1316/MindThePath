using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour
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
            Vector3 next = transform.position + direction * 2.0f;
            if (StageBuilder.Instance.IsValidGridPosition(next) &&
                !StageBuilder.Instance.IsAnyMatchingCellType(next, 'B', 'P', 'K'))
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
}
