using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private class MoveBoxState
    {
        public int id;
        public Vector3 pos;
        public Vector3 targetPos;
    }

    private class GameState
    {
        public Vector3 playerPos;
        public Vector3 playerForward;
        public bool isGameClear;
        public List<MoveBoxState> boxes;
        public char[,,] gridSnapshot;
    }

    private readonly Stack<GameState> history = new Stack<GameState>();

    public void Clear()
    {
        history.Clear();
    }

    public void Record()
    {
        var player = FindObjectOfType<Player>();
        if (player == null) return;

        var gs = new GameState
        {
            playerPos = player.transform.position,
            playerForward = player.transform.forward,
            isGameClear = GameManager.Instance.IsGameClear,
            boxes = FindObjectsOfType<MoveBox>()
                .OrderBy(b => b.GetInstanceID())
                .Select(b => new MoveBoxState
                {
                    id = b.GetInstanceID(),
                    pos = b.transform.position,
                    targetPos = b.TargetPos
                }).ToList(),
            gridSnapshot = StageBuilder.Instance.GetGridDataCopy()
        };

        history.Push(gs);
    }

    public void Undo()
    {
        if (history.Count == 0) return;
        AudioManager.Instance?.PlayUndoSound();
        var gs = history.Pop();

        // Restore grid snapshot first (includes 'F' tiles)
        StageBuilder.Instance.SetGridData(gs.gridSnapshot);
        StageBuilder.Instance.RebuildFragilesFromGrid();

        // Restore player
        var player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.TeleportTo(gs.playerPos);
            player.transform.forward = gs.playerForward;
        }

        // Restore boxes (by instance id ordering)
        var boxes = FindObjectsOfType<MoveBox>().OrderBy(b => b.GetInstanceID()).ToList();
        foreach (var b in boxes)
        {
            var s = gs.boxes.FirstOrDefault(x => x.id == b.GetInstanceID());
            if (s != null)
            {
                b.transform.position = s.pos;
                b.TargetPos = s.targetPos;
            }
        }

        // Update flags
        GameManager.Instance.IsGameClear = gs.isGameClear;

        // Refresh grid: clear dynamic then let actors write their current positions
        StageBuilder.Instance.ResetGridData();
        foreach (var it in FindObjectsOfType<MonoBehaviour>().OfType<ITurnBased>())
        {
            it.UpdateGridData();
        }
    }
}

