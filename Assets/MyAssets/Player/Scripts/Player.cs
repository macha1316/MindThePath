using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ITurnBased
{
    public float moveDuration = 1f;
    private bool isMoving = false;

    public void OnTurn()
    {
        if (isMoving) return;

        Vector3 nextPos = transform.position + transform.forward * 2.0f;
        isMoving = true;

        transform.DOMove(nextPos, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => isMoving = false);

        // 一応デバッグ用で残す
        // transform.position = nextPos;
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdatePlayerPosition(this);
    }
}
