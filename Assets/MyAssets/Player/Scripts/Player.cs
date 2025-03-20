using UnityEngine;

public class Player : MonoBehaviour, ITurnBased
{
    public void OnTurn()
    {
        Vector3 nextPos = transform.position + transform.forward * 2.0f;
        transform.position = nextPos;
    }

    public void UpdateGridData()
    {
        // gridData にプレイヤーの位置を反映
        StageBuilder.Instance.UpdatePlayerPosition(this);
    }
}
