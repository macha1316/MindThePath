public class Robot : TurnbsedCharacter
{
    // === ゴール到達時の処理 ===
    public override void CheckGoal()
    {
        // ゴールに到達したら、前進せずに反転する
        if (StageBuilder.Instance.IsMatchingCellType(nextPos, 'G'))
        {
            TryFlipDirection(ref nextPos);
        }
    }

    public override void UpdateGridData()
    {
        if (isComplete) return;

        if (!StageBuilder.Instance.IsValidGridPosition(nextPos) ||
            StageBuilder.Instance.IsMatchingCellType(nextPos, 'B') ||
            StageBuilder.Instance.IsMatchingCellType(nextPos, 'P') ||
            StageBuilder.Instance.IsMatchingCellType(nextPos, 'K'))
        {
            StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'K'); return;
        }

        StageBuilder.Instance.UpdateGridAtPosition(nextPos, 'K');
    }
}
