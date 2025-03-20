using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    public Vector3 gridPosition; // タイルのグリッド座標

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("kiteru");
        DragAndDrop item = eventData.pointerDrag.GetComponent<DragAndDrop>();

        if (item != null)
        {
            // ギミックをタイル上に配置
            GameObject gimmick = Instantiate(item.gimmickPrefab, transform.position, Quaternion.identity);

            // `gridData` と `dynamicTiles` を更新
            StageBuilder.Instance.SetGimmickAt(gridPosition, gimmick);
        }
    }
}