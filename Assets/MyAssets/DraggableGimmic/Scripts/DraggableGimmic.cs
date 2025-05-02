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

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("UI")) return;

            Vector3 offsetPos = hit.point;
            Vector3 normal = hit.normal;
            float halfBlock = StageBuilder.BLOCK_SIZE * 0.5f;

            if (normal == Vector3.right)
                offsetPos.x += halfBlock;
            else if (normal == Vector3.left)
                offsetPos.x -= halfBlock;
            else if (normal == Vector3.forward)
                offsetPos.z += halfBlock;
            else if (normal == Vector3.back)
                offsetPos.z -= halfBlock;

            Vector3 snappedPos = SnapToGrid(offsetPos);

            if (!StageBuilder.Instance.IsValidGridPosition(snappedPos))
            {
                transform.position = snappedPos;
                return;
            }

            int col = Mathf.RoundToInt(snappedPos.x / StageBuilder.BLOCK_SIZE);
            int height = Mathf.RoundToInt(snappedPos.y / StageBuilder.BLOCK_SIZE);
            int row = Mathf.RoundToInt(snappedPos.z / StageBuilder.BLOCK_SIZE);

            transform.position = snappedPos;

            Vector3 currentGridPos = new Vector3(col, height, row);
            if (currentGridPos != lastGridPosition)
            {
                AudioManager.Instance.PlayDragSound();
                lastGridPosition = currentGridPos;
            }
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

    public void SetCellTypeFromArrow(char type)
    {
        cellType = type;
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
    }
}