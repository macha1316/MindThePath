using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform parentBeforeDrag;
    private bool hasSpawnedObject = false;
    private GameObject gimmickInstance;

    [SerializeField] GameObject gimmickPrefab; // ギミックのプレハブ
    [SerializeField] RectTransform spawnBoundary; // ドラッグして脱出したい境界パネル
    [SerializeField] char cellType;

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
            gimmickInstance.GetComponent<BoxCollider>().enabled = false;
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
        float snappedX = Mathf.Round(worldPos.x / StageBuilder.BLOCK_SIZE) * StageBuilder.BLOCK_SIZE;
        float snappedY = Mathf.Round(worldPos.y / StageBuilder.BLOCK_SIZE) * StageBuilder.BLOCK_SIZE;
        float snappedZ = Mathf.Round(worldPos.z / StageBuilder.BLOCK_SIZE) * StageBuilder.BLOCK_SIZE;
        return new Vector3(snappedX, snappedY, snappedZ);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // オブジェクトを置いたあとのUI復元処理を削除
        // UIを戻さずに非表示のままにしておくことで再度表示しない

        if (gimmickInstance != null)
        {
            StageBuilder.Instance.UpdateGridAtPosition(gimmickInstance.transform.position, cellType);

            gimmickInstance.GetComponent<BoxCollider>().enabled = true;
            gimmickInstance.AddComponent<DraggableGimmic>();
            gimmickInstance.GetComponent<DraggableGimmic>().cellType = cellType;
        }
    }
}