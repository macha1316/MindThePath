using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
        // Reset clear state when (re)starting a stage
        IsGameClear = false;
    }

    // スマホ対応（カメラの向きに応じてプレイヤーの移動方向を変える）
    public int currentIndex = 0;
    public void MoveRight()
    {
        UndoManager.Instance?.RecordForCurrentInput();
        var ps = FindObjectsOfType<Player>();
        foreach (var p in ps) { if (DOTween.IsTweening(p.transform)) return; }
        Vector3 dir = MapByCameraIndex(Vector3.right);
        foreach (var p in ps) p.Direction = dir;
    }
    public void MoveLeft()
    {
        UndoManager.Instance?.RecordForCurrentInput();
        var ps = FindObjectsOfType<Player>();
        foreach (var p in ps) { if (DOTween.IsTweening(p.transform)) return; }
        Vector3 dir = MapByCameraIndex(Vector3.left);
        foreach (var p in ps) p.Direction = dir;
    }
    public void MoveUp()
    {
        UndoManager.Instance?.RecordForCurrentInput();
        var ps = FindObjectsOfType<Player>();
        foreach (var p in ps) { if (DOTween.IsTweening(p.transform)) return; }
        Vector3 dir = MapByCameraIndex(Vector3.forward);
        foreach (var p in ps) p.Direction = dir;
    }
    public void MoveDown()
    {
        UndoManager.Instance?.RecordForCurrentInput();
        var ps = FindObjectsOfType<Player>();
        foreach (var p in ps) { if (DOTween.IsTweening(p.transform)) return; }
        Vector3 dir = MapByCameraIndex(Vector3.back);
        foreach (var p in ps) p.Direction = dir;
    }

    // Camera-relative mapping consistent with Player/SwipeInputController
    private Vector3 MapByCameraIndex(Vector3 baseDir)
    {
        int idx = 0;
        if (CameraController.Instance != null) idx = CameraController.Instance.CurrentIndex;
        if (baseDir == Vector3.forward)
        {
            switch (idx)
            {
                case 0: return Vector3.forward;
                case 1: return Vector3.left;
                case 2: return Vector3.back;
                case 3: return Vector3.right;
            }
        }
        else if (baseDir == Vector3.back)
        {
            switch (idx)
            {
                case 0: return Vector3.back;
                case 1: return Vector3.right;
                case 2: return Vector3.forward;
                case 3: return Vector3.left;
            }
        }
        else if (baseDir == Vector3.left)
        {
            switch (idx)
            {
                case 0: return Vector3.left;
                case 1: return Vector3.back;
                case 2: return Vector3.right;
                case 3: return Vector3.forward;
            }
        }
        else if (baseDir == Vector3.right)
        {
            switch (idx)
            {
                case 0: return Vector3.right;
                case 1: return Vector3.forward;
                case 2: return Vector3.left;
                case 3: return Vector3.back;
            }
        }
        return baseDir;
    }
}
