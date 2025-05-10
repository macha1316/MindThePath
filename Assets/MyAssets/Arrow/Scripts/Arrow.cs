using Unity.VisualScripting;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    private Camera targetCamera;
    private Transform topParent;
    private Transform parent;
    private DraggableGimmic draggableGimmic;

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // 2つ上の親を取得
        if (transform.parent != null)
        {
            parent = transform.parent;
        }
        if (transform.parent != null && transform.parent.parent != null)
        {
            topParent = transform.parent.parent;
        }
        else
        {
            topParent = transform;
        }
    }

    void Update()
    {
        if (targetCamera != null)
        {
            Vector3 direction = targetCamera.transform.position - transform.position;
            direction.y = 0f;
            transform.forward = direction.normalized;
        }
        if (GameManager.Instance.GetIsStart())
        {
            parent.gameObject.SetActive(false);
        }
    }

    void OnMouseDown()
    {
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform)
            {
                if (draggableGimmic == null)
                {
                    draggableGimmic = topParent.GetComponent<DraggableGimmic>();
                }
                RotationParentGimic();
            }
        }
    }

    public void RotationParentGimic()
    {
        if (topParent != null)
        {
            topParent.Rotate(0, 90f, 0, Space.World);
            AudioManager.Instance.PlayRotateSound();

            // ギミックタイプの変更: R → D → L → U → R...
            switch (draggableGimmic.cellType)
            {
                case 'R':
                    draggableGimmic.SetCellTypeFromArrow('D');
                    break;
                case 'D':
                    draggableGimmic.SetCellTypeFromArrow('L');
                    break;
                case 'L':
                    draggableGimmic.SetCellTypeFromArrow('U');
                    break;
                case 'U':
                    draggableGimmic.SetCellTypeFromArrow('R');
                    break;
            }
        }
    }
}
