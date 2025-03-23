using UnityEngine;

public class DraggableGimmic : MonoBehaviour
{
    private BoxCollider boxCollider;
    private Vector3 offset;
    private bool isDragging = false;

    public char cellType = 'G';

    private void OnMouseDown()
    {
        isDragging = true;
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        offset = transform.position - mouseWorldPos;

        boxCollider = GetComponent<BoxCollider>();
        boxCollider.enabled = false;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 targetPos = mouseWorldPos + offset;
        transform.position = SnapToGrid(targetPos);
    }

    private void OnMouseUp()
    {
        isDragging = false;
        boxCollider.enabled = true;

        // グリッド情報を更新
        if (StageBuilder.Instance != null)
        {
            StageBuilder.Instance.UpdateGridAtPosition(transform.position, cellType);
        }
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