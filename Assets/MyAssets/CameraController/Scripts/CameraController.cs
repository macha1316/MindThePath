using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform mainCamera;     // 実際のカメラ
    // private bool is2DView = false;

    void Start()
    {
    }

    public void SwitchView()
    {
        GameManager.Instance.Is2DMode = !GameManager.Instance.Is2DMode;
        if (GameManager.Instance.Is2DMode)
        {

            SwitchTo2DView();
            StageBuilder.Instance.SwitchTo2DView();
        }
        else
        {
            SwitchTo3DView();
            StageBuilder.Instance.SwitchTo3DView();
        }
    }

    public void SwitchTo2DView()
    {
        mainCamera.position = new Vector3(8f, 17f, 8f);
        mainCamera.rotation = Quaternion.Euler(90f, 0f, 0f);
        Camera cam = mainCamera.GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 10f;
        }
    }

    public void SwitchTo3DView()
    {
        mainCamera.position = new Vector3(8f, 17f, -8f);
        mainCamera.rotation = Quaternion.Euler(45f, 0f, 0f);
        Camera cam = mainCamera.GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = false;
        }
    }
}