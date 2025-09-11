using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance;
    // Prevent double-recording when two players react to the same input frame
    private int lastRecordedInputFrame = -1;
    // Prevent multiple undo executions within the same input frame
    private int lastUndoneInputFrame = -1;

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

    private class PlayerState
    {
        public int id;
        public Vector3 pos;
        public Vector3 forward;
    }

    private class GameState
    {
        public List<PlayerState> players;
        public bool isGameClear;
        public List<MoveBoxState> boxes;
        public char[,,] gridSnapshot;
    }

    private readonly Stack<GameState> history = new Stack<GameState>();

    public void Clear()
    {
        history.Clear();
        lastRecordedInputFrame = -1;
        lastUndoneInputFrame = -1;
    }

    public void Record()
    {
        var players = FindObjectsOfType<Player>()
            .OrderBy(p => p.GetInstanceID())
            .ToList();
        if (players.Count == 0) return;

        var gs = new GameState
        {
            players = players.Select(p => new PlayerState
            {
                id = p.GetInstanceID(),
                pos = p.transform.position,
                forward = p.transform.forward
            }).ToList(),
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

    // Record only once per input frame (use with KeyDown/UI input)
    public void RecordForCurrentInput()
    {
        int f = Time.frameCount;
        if (lastRecordedInputFrame == f) return;
        Record();
        lastRecordedInputFrame = f;
    }

    // Undo only once per input frame (use with KeyDown/UI input)
    public void UndoForCurrentInput()
    {
        int f = Time.frameCount;
        if (lastUndoneInputFrame == f) return;
        Undo();
        lastUndoneInputFrame = f;
    }

    public void Undo()
    {
        if (history.Count == 0) return;
        AudioManager.Instance?.PlayUndoSound();
        var gs = history.Pop();

        // Restore grid snapshot first (includes 'F' tiles)
        StageBuilder.Instance.SetGridData(gs.gridSnapshot);
        StageBuilder.Instance.RebuildFragilesFromGrid();

        // Restore players (by instance id ordering)
        var playersNow = FindObjectsOfType<Player>().OrderBy(p => p.GetInstanceID()).ToList();
        foreach (var p in playersNow)
        {
            var s = gs.players.FirstOrDefault(x => x.id == p.GetInstanceID());
            if (s != null)
            {
                p.TeleportTo(s.pos);
                p.transform.forward = s.forward;
            }
        }

        // Refresh goal visibility and ON/OFF once (players only at this point)
        StageBuilder.Instance.RefreshSwitchAndOnOff();
        StageBuilder.Instance.RefreshGoalVisibilityForPlayers();

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

        // Final refresh: ON/OFF must reflect both players and boxes after full restore
        StageBuilder.Instance.RefreshSwitchAndOnOff();
    }
}
