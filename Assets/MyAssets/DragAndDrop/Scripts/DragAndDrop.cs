using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform parentBeforeDrag;
    private bool hasSpawnedObject = false;
    private GameObject gimmickInstance;

    public GameObject gimmickPrefab; // ギミックのプレハブ
    public RectTransform spawnBoundary; // ドラッグして脱出したい境界パネル
    private float blockSize = 2f; // グリッドサイズに合わせて変更

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        eventData.pointerDrag = gameObject;
        parentBeforeDrag = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;
        hasSpawnedObject = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = Input.mousePosition;

        if (!hasSpawnedObject && !RectTransformUtility.RectangleContainsScreenPoint(spawnBoundary, Input.mousePosition))
        {
            hasSpawnedObject = true;
            canvasGroup.alpha = 0;

            Vector3 worldPos = GetWorldPosition();
            gimmickInstance = Instantiate(gimmickPrefab, SnapToGrid(worldPos), Quaternion.identity);
        }

        if (hasSpawnedObject && gimmickInstance != null)
        {
            Vector3 worldPos = GetWorldPosition();
            gimmickInstance.transform.position = SnapToGrid(worldPos);
        }
    }

    private Vector3 GetWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return hit.point; // 地面やグリッドに当たった場所
        }

        // 失敗時は fallback
        return Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
    }

    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        float snappedX = Mathf.Round(worldPos.x / blockSize) * blockSize;
        float snappedY = Mathf.Round(worldPos.y / blockSize) * blockSize;
        float snappedZ = Mathf.Round(worldPos.z / blockSize) * blockSize;
        return new Vector3(snappedX, snappedY, snappedZ);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(parentBeforeDrag);
        transform.localPosition = Vector3.zero;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        StageBuilder.Instance.UpdateGridAtPosition(gimmickInstance.transform.position, 'Y');

        if (hasSpawnedObject)
        {
            return;
        }
    }
}