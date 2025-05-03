public interface ITurnBased
{
    void OnTurn();        // ターンごとの動作（移動やアクション）
    void UpdateGridData(); // gridDataの更新
}