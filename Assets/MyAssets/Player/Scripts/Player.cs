using UnityEngine;
using DG.Tweening;
using System.Collections;

public class Player : MonoBehaviour, ITurnBased
{
    public float moveSpeed = 5f;
    [Header("Jump Move Settings")]
    [SerializeField] private float jumpPowerFactor = 0.6f; // HEIGHT_OFFSETに対する倍率
    [SerializeField] private int jumpCount = 1;             // 1マスにつき何回ジャンプするか
    [SerializeField] private float squashAmount = 0.08f;    // ぴょん時のつぶれ量（スケール）
    [SerializeField] private float squashDuration = 0.06f;  // つぶれ時間
    private bool isMoving = false;
    private Vector3 targetPosition;
    public Vector3 Direction { get; set; } = Vector3.zero;
    private Vector3 lastSupportPos; // 直前に立っていた床（1段下）の座標
    private bool isGoalCelebrating = false;

    void Start()
    {
        targetPosition = transform.position;
        StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'P');
    }

    void Update()
    {
        // Undo (keyboard): Z — only when not moving
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (!isMoving)
            {
                UndoManager.Instance?.Undo();
                AudioManager.Instance?.PlayUndoSound();
            }
            return;
        }

        if (isMoving)
        {
            // 既に移動中は新しい入力を受け付けない（アニメ状態は維持）
            return;
        }

        // WASDをカメラの向きに追従させる
        if (Input.GetKeyDown(KeyCode.W)) Direction = MapByCameraIndex(Vector3.forward);
        else if (Input.GetKeyDown(KeyCode.S)) Direction = MapByCameraIndex(Vector3.back);
        else if (Input.GetKeyDown(KeyCode.A)) Direction = MapByCameraIndex(Vector3.left);
        else if (Input.GetKeyDown(KeyCode.D)) Direction = MapByCameraIndex(Vector3.right);

        if (Direction != Vector3.zero)
        {
            Vector3 next = transform.position + Direction * StageBuilder.HEIGHT_OFFSET;

            bool canMove = false;

            if (GameManager.Instance.Is2DMode)
            {
                if (StageBuilder.Instance.IsValidGridPosition(next))
                {
                    Vector3 topPos = StageBuilder.Instance.GetTopCellPosition(next);
                    if (StageBuilder.Instance.IsAnyMatchingCellType(topPos, 'B', 'M', 'F', 'A'))
                    {
                        next = topPos + Vector3.up * StageBuilder.HEIGHT_OFFSET;
                        canMove = true;
                    }
                }
            }
            else
            {
                if (!StageBuilder.Instance.IsValidGridPosition(next)) return;
                Vector3 nextDown = next + Vector3.down * StageBuilder.HEIGHT_OFFSET;

                if (StageBuilder.Instance.TryGetMoveBoxAtPosition(next, out var boxAhead))
                {
                    Vector3 afterNext = next + Direction * StageBuilder.HEIGHT_OFFSET;
                    if (!StageBuilder.Instance.IsValidGridPosition(afterNext)) return;
                    // 押し先に箱が既にある場合は押せない
                    if (StageBuilder.Instance.TryGetMoveBoxAtPosition(afterNext, out _)) return;
                    // Teleport('A')を空きとして許可（押し先がテレポートでも可）
                    bool afterNextIsEmpty = StageBuilder.Instance.IsMatchingCellType(afterNext, 'N') ||
                                             StageBuilder.Instance.IsMatchingCellType(afterNext, 'A');
                    if (!afterNextIsEmpty) return;
                    if (StageBuilder.Instance.IsMatchingCellType(nextDown, 'O')) return;

                    // 新規条件: 対象の箱の上にさらに箱がある場合は押せない
                    Vector3 aboveBox = next + Vector3.up * StageBuilder.HEIGHT_OFFSET;
                    if (StageBuilder.Instance.TryGetMoveBoxAtPosition(aboveBox, out _)) return;

                    // 押し出し先の直下に一つもブロックが無い場合は押せない
                    if (!StageBuilder.Instance.HasAnySupportBelow(afterNext)) return;

                    // 既存挙動に合わせて、Goal('G')は空気として扱い更に下まで落下
                    Vector3 dropPos = StageBuilder.Instance.FindDropPosition(afterNext, goalIsAir: true);

                    StageBuilder.Instance.UpdateGridAtPosition(next, 'N');
                    StageBuilder.Instance.UpdateGridAtPosition(dropPos, 'M');

                    boxAhead.transform.DOMove(dropPos, 1f / moveSpeed)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            boxAhead.transform.position = dropPos;
                            boxAhead.TargetPos = dropPos;
                            boxAhead.TeleportIfOnPortal();
                        });
                    boxAhead.TargetPos = dropPos;

                    canMove = true;
                }
                else if (StageBuilder.Instance.IsValidGridPosition(next) &&
                    // F(Fragile) も“壁”として扱い、めり込みを防ぐ
                    !StageBuilder.Instance.IsAnyMatchingCellType(next, 'B', 'P', 'O', 'F') &&
                    !StageBuilder.Instance.IsAnyMatchingCellType(nextDown, 'P', 'N', 'O'))
                {
                    canMove = true;
                }
            }

            if (canMove)
            {
                // Snapshot current state for Undo
                UndoManager.Instance?.Record();

                // 今立っている床（1段下）を記録（移動後に消滅処理するため）
                lastSupportPos = transform.position + Vector3.down * StageBuilder.HEIGHT_OFFSET;
                StageBuilder.Instance.UpdateGridAtPosition(transform.position, 'N');
                targetPosition = next;
                isMoving = true;
                transform.forward = Direction;
                CheckGoal();

                StageBuilder.Instance.UpdateGridAtPosition(targetPosition, 'P');

                float jumpPower = StageBuilder.HEIGHT_OFFSET * jumpPowerFactor;
                var tween = transform.DOJump(targetPosition, jumpPower, Mathf.Max(1, jumpCount), 1f / moveSpeed)
                    .OnStart(() =>
                    {
                        // 軽いつぶれ感
                        if (squashAmount > 0f)
                        {
                            Vector3 s0 = transform.localScale;
                            Vector3 s1 = new Vector3(s0.x + squashAmount, s0.y - squashAmount * 1.5f, s0.z + squashAmount);
                            transform.DOScale(s1, squashDuration).SetLoops(2, LoopType.Yoyo);
                        }
                        AudioManager.Instance?.PlayMoveSound();
                    })
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        transform.position = targetPosition;
                        isMoving = false;
                        HandleFragileFloorDisappear();
                        HandleTeleport();
                    });
            }
            Direction = Vector3.zero;
        }
    }

    // カメラの現在インデックスに応じて、基準方向をワールド方向へマップ
    private Vector3 MapByCameraIndex(Vector3 baseDir)
    {
        int idx = 0;
        if (CameraController.Instance != null) idx = CameraController.Instance.CurrentIndex;

        // idx=0 がデフォルト（カメラ+X右, +Z前）
        // idxが90度回る毎に、WASDの基準方向を回転させる
        if (baseDir == Vector3.forward)
        {
            switch (idx)
            {
                case 0: return Vector3.forward;
                case 1: return Vector3.left;
                case 2: return Vector3.back;
                case 3: return Vector3.right;
            }
        }
        else if (baseDir == Vector3.back)
        {
            switch (idx)
            {
                case 0: return Vector3.back;
                case 1: return Vector3.right;
                case 2: return Vector3.forward;
                case 3: return Vector3.left;
            }
        }
        else if (baseDir == Vector3.left)
        {
            switch (idx)
            {
                case 0: return Vector3.left;
                case 1: return Vector3.back;
                case 2: return Vector3.right;
                case 3: return Vector3.forward;
            }
        }
        else if (baseDir == Vector3.right)
        {
            switch (idx)
            {
                case 0: return Vector3.right;
                case 1: return Vector3.forward;
                case 2: return Vector3.left;
                case 3: return Vector3.back;
            }
        }
        return baseDir;
    }

    private void CheckGoal()
    {
        if (isGoalCelebrating) return;
        if (StageBuilder.Instance.IsMatchingCellType(targetPosition, 'G'))
        {
            StartCoroutine(GoalCelebrateThenClear());
        }
    }

    private IEnumerator GoalCelebrateThenClear()
    {
        isGoalCelebrating = true;
        GameManager.Instance.IsGameClear = true;
        // 入力抑制
        isMoving = true;

        // 近傍のGoalオブジェクトを削除（GoalBlockを付与している前提）
        var goals = FindObjectsOfType<GoalBlock>();
        float bestDist = float.MaxValue; GoalBlock nearest = null;
        foreach (var g in goals)
        {
            float d = Vector3.SqrMagnitude(g.transform.position - transform.position);
            if (d < bestDist)
            {
                bestDist = d; nearest = g;
            }
        }
        if (nearest != null && bestDist < (StageBuilder.BLOCK_SIZE * StageBuilder.BLOCK_SIZE * 4f))
        {
            Destroy(nearest.gameObject);
        }
        AudioManager.Instance?.PlayClearSounds();
        ConfettiManager.Instance?.SpawnBurst();

        yield return new WaitForSeconds(0.5f);

        Vector3 pos = transform.position;
        float jumpH = StageBuilder.HEIGHT_OFFSET * 0.9f;
        // ちょい派手なジャンプ＋回転＋スカッシュ
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOJump(pos, jumpH, 1, 0.45f).SetEase(Ease.OutCubic));
        seq.Join(transform.DORotate(new Vector3(0f, 360f, 0f), 0.45f, RotateMode.WorldAxisAdd).SetEase(Ease.OutCubic));
        seq.Join(transform.DOPunchScale(new Vector3(0.25f, -0.25f, 0.25f), 0.4f, 2, 0.6f));

        yield return seq.WaitForCompletion();
        yield return new WaitForSeconds(1f);

        StageSelectUI.Instance.SetClearUI();
    }

    public void UpdateGridData()
    {
        StageBuilder.Instance.UpdateGridAtPosition(targetPosition, 'P');
    }

    // Teleport instantly (used by Undo)
    public void TeleportTo(Vector3 pos)
    {
        isMoving = false;
        targetPosition = pos;
        transform.position = pos;
    }

    private void HandleFragileFloorDisappear()
    {
        // 直前に乗っていた足場が消える床(F)なら、破壊してグリッドを'N'にする
        if (!StageBuilder.Instance.IsValidGridPosition(lastSupportPos)) return;
        if (!StageBuilder.Instance.IsMatchingCellType(lastSupportPos, 'F')) return;

        // グリッド更新
        StageBuilder.Instance.UpdateGridAtPosition(lastSupportPos, 'N');

        // 対応するFragileBlockオブジェクトを探して破棄
        var lastGrid = StageBuilder.Instance.GridFromPosition(lastSupportPos);
        foreach (var frag in FindObjectsOfType<FragileBlock>())
        {
            var g = StageBuilder.Instance.GridFromPosition(frag.transform.position);
            if (g == lastGrid)
            {
                // 演出して消す
                var t = frag.transform;
                t.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    if (frag != null) Destroy(frag.gameObject);
                });
                break;
            }
        }
    }

    private void HandleTeleport()
    {
        // 同じ座標にテレポート('A')がある場合、相方の'A'へ瞬間移動
        Vector3 here = targetPosition;
        if (!StageBuilder.Instance.IsValidGridPosition(here)) return;

        // グリッドの文字ではなくシーン上の TeleportBlock を参照して同座標判定
        var myCell = StageBuilder.Instance.GridFromPosition(here);
        TeleportBlock currentPortal = null;
        foreach (var p in FindObjectsOfType<TeleportBlock>())
        {
            var c = StageBuilder.Instance.GridFromPosition(p.transform.position);
            if (c.x == myCell.x && c.y == myCell.y && c.z == myCell.z)
            {
                currentPortal = p;
                break;
            }
        }
        if (currentPortal == null) return;

        var fromCell = StageBuilder.Instance.GridFromPosition(currentPortal.transform.position);
        if (StageBuilder.Instance.TryFindOtherCell('A', fromCell, out var otherCell))
        {
            // 目的地は相方の'A'と同じ座標（同じセル）
            Vector3 dest = StageBuilder.Instance.WorldFromGrid(otherCell);
            if (!StageBuilder.Instance.IsValidGridPosition(dest)) return;

            bool boxOnDest = StageBuilder.Instance.TryGetMoveBoxAtPosition(dest, out var boxAtDest);
            if (boxOnDest)
            {
                // 目的地に箱がいる場合は、押せるなら押してからテレポート
                Vector3 afterNext = dest + transform.forward * StageBuilder.HEIGHT_OFFSET;
                if (!StageBuilder.Instance.IsValidGridPosition(afterNext)) return; // 押せないのでTP中止
                if (StageBuilder.Instance.TryGetMoveBoxAtPosition(afterNext, out _)) return; // 押し先に箱
                if (!StageBuilder.Instance.IsMatchingCellType(afterNext, 'N')) return; // 押せない
                if (!StageBuilder.Instance.HasAnySupportBelow(afterNext)) return; // 支えがない

                // 新規条件: 押す対象の箱の上に箱が乗っている場合は押せない
                Vector3 aboveDestBox = dest + Vector3.up * StageBuilder.HEIGHT_OFFSET;
                if (StageBuilder.Instance.TryGetMoveBoxAtPosition(aboveDestBox, out _)) return;

                Vector3 dropPos = StageBuilder.Instance.FindDropPosition(afterNext, goalIsAir: true);

                // グリッド更新（箱を先に動かす）
                StageBuilder.Instance.UpdateGridAtPosition(dest, 'N');
                StageBuilder.Instance.UpdateGridAtPosition(dropPos, 'M');

                boxAtDest.transform.DOMove(dropPos, 1f / moveSpeed)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        boxAtDest.transform.position = dropPos;
                        boxAtDest.TargetPos = dropPos;
                        boxAtDest.TeleportIfOnPortal();
                    });
            }
            else
            {
                // 目的地は 'N' または 'A' を許容（'A'は空扱い）
                char occ = StageBuilder.Instance.GetGridCharType(dest);
                if (!(occ == 'N' || occ == 'A')) return;
            }

            // テレポート演出（縮小→小さく弧を描いて移動→拡大）
            Vector3 s0 = transform.localScale;
            float shrinkT = 0.08f;
            float expandT = 0.12f;
            float arcT = 0.20f;
            // サウンド
            AudioManager.Instance?.PlayTeleportSound();
            transform.DOScale(s0 * 0.2f, shrinkT).OnComplete(() =>
            {
                Vector3 start = transform.position;
                Vector3 dir = (dest - start);
                Vector3 flat = new Vector3(dir.x, 0f, dir.z);
                Vector3 side = Vector3.Cross(flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward, Vector3.up);
                float sideSign = Random.value < 0.5f ? -1f : 1f;
                float sideMag = StageBuilder.BLOCK_SIZE * 0.2f;
                Vector3 mid = Vector3.Lerp(start, dest, 0.5f)
                              + Vector3.up * (StageBuilder.HEIGHT_OFFSET * 0.5f)
                              + side * sideSign * sideMag;

                var seq = DG.Tweening.DOTween.Sequence();
                seq.Append(transform.DOMove(mid, arcT * 0.5f).SetEase(Ease.OutSine));
                seq.Append(transform.DOMove(dest, arcT * 0.5f).SetEase(Ease.InSine));
                seq.OnComplete(() =>
                {
                    // グリッド更新: 現在位置を空に、目的地をプレイヤーに
                    StageBuilder.Instance.UpdateGridAtPosition(targetPosition, 'N');
                    StageBuilder.Instance.UpdateGridAtPosition(dest, 'P');

                    // 最終位置・拡大
                    transform.position = dest;
                    targetPosition = dest;
                    transform.DOScale(s0, expandT).SetEase(Ease.OutBack);
                });
            });
        }
    }
}
