using UnityEngine;
using DG.Tweening;

public class MoveBox : MonoBehaviour, ITurnBased
{
    private bool isMoving = false;
    private float moveDuration = 1f;
    private Vector3 targetPos;

    void Start()
    {
        targetPos = transform.position;
    }

    public void TryPush(Vector3 direction)
    {
        if (isMoving) return;

        targetPos += direction * StageBuilder.BLOCK_SIZE;

        if (!StageBuilder.Instance.IsValidGridPosition(targetPos)) return;

        char[,,] grid = StageBuilder.Instance.GetGridData();
        int col = Mathf.RoundToInt(targetPos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(targetPos.y / StageBuilder.HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(targetPos.z / StageBuilder.BLOCK_SIZE);

        if (grid[col, height, row] != 'N') return;

        isMoving = true;
        transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
        {
            isMoving = false;
        });
        return;
    }

    public void OnTurn()
    {
        if (isMoving) return;
        // 自らの下を見てNなら毎ターン下がるようにする
        Vector3 oneDown = targetPos + Vector3.down * StageBuilder.HEIGHT_OFFSET;
        if (StageBuilder.Instance.IsValidGridPosition(oneDown))
        {
            int oneDownCol = Mathf.RoundToInt(oneDown.x / StageBuilder.BLOCK_SIZE);
            int oneDownHeight = Mathf.RoundToInt(oneDown.y / StageBuilder.HEIGHT_OFFSET);
            int oneDownRow = Mathf.RoundToInt(oneDown.z / StageBuilder.BLOCK_SIZE);

            char[,,] grid = StageBuilder.Instance.GetGridData();

            if (grid[oneDownCol, oneDownHeight, oneDownRow] == 'N')
            {
                targetPos = oneDown;
                isMoving = true;
                transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear).OnComplete(() =>
                {
                    isMoving = false;
                });
            }
        }
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdateGridAtPosition(targetPos, 'M');
    }
}
