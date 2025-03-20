using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform parentBeforeDrag;

    public GameObject gimmickPrefab; // ギミックのプレハブ

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        eventData.pointerDrag = gameObject; // これを追加
        parentBeforeDrag = transform.parent;
        transform.SetParent(transform.root); // UIの最前面へ
        canvasGroup.blocksRaycasts = false; // ドロップを許可
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(parentBeforeDrag); // 元の位置に戻す
        transform.localPosition = Vector3.zero;
        canvasGroup.blocksRaycasts = true;
    }
}