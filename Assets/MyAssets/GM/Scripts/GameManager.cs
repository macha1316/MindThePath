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

    // スマホ対応
    public void MoveRight()
    {
        players[0].Direction = Vector3.right;
    }
    public void MoveLeft()
    {
        players[0].Direction = Vector3.left;
    }
    public void MoveUp()
    {
        players[0].Direction = Vector3.forward;
    }
    public void MoveDown()
    {
        players[0].Direction = Vector3.back;
    }
}
