using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private List<Player> players;
    private bool isStart = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        players = new List<Player>();
    }

    public void GetPlayer(Player newP)
    {
        players.Add(newP);
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
        isStart = false;
    }
}
