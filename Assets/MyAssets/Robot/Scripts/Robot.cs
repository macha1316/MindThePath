public class Robot : TurnbsedCharacter
{
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
