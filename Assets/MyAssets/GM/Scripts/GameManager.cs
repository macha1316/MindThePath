using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private List<Player> players;
    private List<MoveBox> boxes;
    private bool isStart = false;
    private int stageNumber;
    public Dictionary<Vector3Int, Player> reservedPositions = new();

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

    public bool IsComplete()
    {
        foreach (var player in players)
        {
            if (!player.GetIsComplete()) return false;
        }
        return true;
    }

    public bool GetIsStart() => isStart;

    public void SetIsStart()
    {
        isStart = true;
    }
    public void SetGameStop()
    {
        players = new List<Player>();
        boxes = new List<MoveBox>();
        isStart = false;
    }

    public int GetStageNumber() => stageNumber;
    public int SetStageNumber(int num)
    {
        stageNumber = stageNumber += num;
        return stageNumber;
    }
}
