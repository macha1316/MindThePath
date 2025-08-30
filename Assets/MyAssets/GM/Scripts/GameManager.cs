using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private List<Player> players;
    private List<MoveBox> boxes;
    public bool IsStart { get; set; } = false;
    public bool Is2DMode { get; set; } = false;
    public bool IsGameClear { get; set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        players = new List<Player>();
        boxes = new List<MoveBox>();
        // Ensure UndoManager exists in scene
        if (FindObjectOfType<UndoManager>() == null)
        {
            var go = new GameObject("UndoManager");
            go.AddComponent<UndoManager>();
            DontDestroyOnLoad(go);
        }
    }

    public void GetPlayer(Player newP)
    {
        players.Add(newP);
    }

    public void GetMoveBox(MoveBox newM)
    {
        boxes.Add(newM);
    }

    public void SetGameStop()
    {
        players = new List<Player>();
        boxes = new List<MoveBox>();
        IsStart = false;
    }

    // スマホ対応（カメラの向きに応じてプレイヤーの移動方向を変える）
    public int currentIndex = 0;
    public void MoveRight()
    {
        switch (CameraController.Instance.CurrentIndex)
        {
            case 0: players[0].Direction = Vector3.right; break;
            case 1: players[0].Direction = Vector3.forward; break;
            case 2: players[0].Direction = Vector3.left; break;
            case 3: players[0].Direction = Vector3.back; break;
        }
    }
    public void MoveLeft()
    {
        switch (CameraController.Instance.CurrentIndex)
        {
            case 0: players[0].Direction = Vector3.left; break;
            case 1: players[0].Direction = Vector3.back; break;
            case 2: players[0].Direction = Vector3.right; break;
            case 3: players[0].Direction = Vector3.forward; break;
        }
    }
    public void MoveUp()
    {
        switch (CameraController.Instance.CurrentIndex)
        {
            case 0: players[0].Direction = Vector3.forward; break;
            case 1: players[0].Direction = Vector3.left; break;
            case 2: players[0].Direction = Vector3.back; break;
            case 3: players[0].Direction = Vector3.right; break;
        }
    }
    public void MoveDown()
    {
        switch (CameraController.Instance.CurrentIndex)
        {
            case 0: players[0].Direction = Vector3.back; break;
            case 1: players[0].Direction = Vector3.right; break;
            case 2: players[0].Direction = Vector3.forward; break;
            case 3: players[0].Direction = Vector3.left; break;
        }
    }
}
