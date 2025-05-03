using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform cameraRig;      // ステージ中心を回転させるターゲット
    public Transform mainCamera;     // 実際のカメラ
    public float rotateSpeed = 0.2f;
    public float zoomSpeed = 0.5f;
    public float minDistance = 3f;
    public float maxDistance = 20f;
    private float currentDistance;

    void Start()
    {
        currentDistance = Vector3.Distance(cameraRig.position, mainCamera.position);
    }

    void Update()
    {
        if (InputStateManager.IsDragging == true)
        {
            return;
        }

        if (Input.touchSupported && Input.touchCount > 0)
        {
            HandleTouch();
        }
        else
        {
            HandleMouse();
        }
    }

    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                cameraRig.Rotate(0, delta.x * rotateSpeed, 0, Space.World);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prevPos0 = t0.position - t0.deltaPosition;
            Vector2 prevPos1 = t1.position - t1.deltaPosition;
            float prevDist = Vector2.Distance(prevPos0, prevPos1);
            float currentDist = Vector2.Distance(t0.position, t1.position);

            float delta = currentDist - prevDist; // 指が広がれば正、狭まれば負
            currentDistance -= delta * zoomSpeed * 0.02f; // 少し調整
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            Vector3 direction = (mainCamera.position - cameraRig.position).normalized;
            mainCamera.position = cameraRig.position + direction * currentDistance;
        }
    }

    void HandleMouse()
    {
        if (Input.GetMouseButton(0))
        {
            float deltaX = Input.GetAxis("Mouse X");
            cameraRig.Rotate(0, deltaX * rotateSpeed * 5f, 0, Space.World);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed * 20f;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            Vector3 direction = (mainCamera.position - cameraRig.position).normalized;
            mainCamera.position = cameraRig.position + direction * currentDistance;
        }
    }
}