using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Cinemachine.CinemachineVirtualCamera[] virtualCameras;
    public Cinemachine.CinemachineVirtualCamera virtualCamera2D;
    public Cinemachine.CinemachineBrain cinemachineBrain;
    public int CurrentIndex { get; set; } = 0;
    public Transform mainCamera;

    public static CameraController Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
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
        cinemachineBrain.m_DefaultBlend.m_Time = 0f;
        virtualCamera2D.Priority = 11;
        virtualCamera2D.m_Lens.OrthographicSize = 12f;
        StageSelectUI.Instance.HideCameraRotateUI();

        switch (CurrentIndex)
        {
            case 0:
                virtualCamera2D.transform.rotation = Quaternion.Euler(90, 0, 0);
                break;
            case 1:
                virtualCamera2D.transform.rotation = Quaternion.Euler(90, -90, 0);
                break;
            case 2:
                virtualCamera2D.transform.rotation = Quaternion.Euler(90, 180, 0);
                break;
            case 3:
                virtualCamera2D.transform.rotation = Quaternion.Euler(90, 90, 0);
                break;
        }

        Camera cam = mainCamera.GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 10f;
        }
    }

    public void SwitchTo3DView()
    {
        cinemachineBrain.m_DefaultBlend.m_Time = 0.5f;
        virtualCamera2D.Priority = 0;
        StageSelectUI.Instance.ShowCameraRotateUI();

        Camera cam = mainCamera.GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = false;
        }
    }

    public void RotateRight()
    {
        if (virtualCameras == null || virtualCameras.Length == 0) return;

        virtualCameras[CurrentIndex].Priority = 0;
        CurrentIndex = (CurrentIndex + 1) % virtualCameras.Length;
        virtualCameras[CurrentIndex].Priority = 10;
    }

    public void RotateLeft()
    {
        if (virtualCameras == null || virtualCameras.Length == 0) return;

        virtualCameras[CurrentIndex].Priority = 0;
        CurrentIndex = (CurrentIndex - 1 + virtualCameras.Length) % virtualCameras.Length;
        virtualCameras[CurrentIndex].Priority = 10;
    }
}