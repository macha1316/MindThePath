using UnityEngine;

public class DraggableGimmic : MonoBehaviour
{
    private BoxCollider boxCollider;
    private Vector3 offset;
    private bool isDragging = false;
    private Vector3 lastGridPosition;
    public char cellType = 'N';

    void Start()
    {
        lastGridPosition = Vector3.zero;
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance.GetIsStart()) return;
        isDragging = true;
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        offset = transform.position - mouseWorldPos;

        // 現在位置のgridDataをNに更新
        if (StageBuilder.Instance != null)
        {
            Vector3 currentPos = SnapToGrid(transform.position);
            int col = Mathf.RoundToInt(currentPos.x / StageBuilder.BLOCK_SIZE);
            int height = Mathf.RoundToInt(currentPos.y / StageBuilder.BLOCK_SIZE);
            int row = Mathf.RoundToInt(currentPos.z / StageBuilder.BLOCK_SIZE);

            StageBuilder.Instance.GetGridData()[col, height, row] = 'N';
            StageBuilder.Instance.GetDynamicGridData()[col, height, row] = 'N';
        }

        boxCollider = GetComponent<BoxCollider>();
        boxCollider.enabled = false;
        InputStateManager.IsDragging = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 targetPos = mouseWorldPos + offset;
        Vector3 snappedPos = SnapToGrid(targetPos);

        if (!StageBuilder.Instance.IsValidGridPosition(snappedPos))
        {
            transform.position = snappedPos;
            return;
        }

        int col = Mathf.RoundToInt(snappedPos.x / StageBuilder.BLOCK_SIZE);
        int height = Mathf.RoundToInt(snappedPos.y / StageBuilder.BLOCK_SIZE);
        int row = Mathf.RoundToInt(snappedPos.z / StageBuilder.BLOCK_SIZE);

        // Nが出るまで上へ再帰的に確認
        while (height < StageBuilder.Instance.GetGridData().GetLength(1) &&
               StageBuilder.Instance.GetGridData()[col, height, row] != 'N')
        {
            height += 1;
        }
        snappedPos.y = height * StageBuilder.BLOCK_SIZE;

        transform.position = snappedPos;

        Vector3 currentGridPos = new Vector3(col, height, row);
        if (currentGridPos != lastGridPosition)
        {
            AudioManager.Instance.PlayDragSound();
            lastGridPosition = currentGridPos;
        }
    }

    private void OnMouseUp()
    {
        if (GameManager.Instance.GetIsStart()) return;
        isDragging = false;
        boxCollider.enabled = true;

        // Grid
        if (cellType == 'B' || cellType == 'M' || cellType == 'P')
        {
            StageBuilder.Instance.UpdateGridAtPosition(transform.position, cellType);
        }
        // Dynamic
        else
        {
            StageBuilder.Instance.UpdateDynamicTileAtPosition(transform.position, cellType);
        }

        InputStateManager.IsDragging = false;
        AudioManager.Instance.PlayDropSound();

    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return hit.point;
        }
        return Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
    }

    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        float size = StageBuilder.BLOCK_SIZE;
        return new Vector3(
            Mathf.Round(worldPos.x / size) * size,
            Mathf.Round(worldPos.y / size) * size,
            Mathf.Round(worldPos.z / size) * size
        );
    }
}