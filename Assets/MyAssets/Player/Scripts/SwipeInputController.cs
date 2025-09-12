using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

// Attach to a persistent object in game scenes (e.g., an input manager on Canvas or a singleton root)
public class SwipeInputController : MonoBehaviour
{
    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 60f; // pixels
    [SerializeField] private float maxTapDuration = 0.20f; // seconds
    [SerializeField] private float doubleTapMaxDelay = 0.30f; // seconds
    [SerializeField] private float doubleTapMaxDistance = 40f; // pixels
    [Tooltip("If true, swipes/double-taps are detected even over UI/Canvas")]
    [SerializeField] private bool allowOverUI = true;
    [Tooltip("Prevent Undo when the tap happens over UI elements")]
    [SerializeField] private bool suppressUndoOverUI = true;

    private Vector2 startPos;
    private float startTime;
    private int activePointerId = -1;
    private bool tracking;

    private float lastTapTime = -10f;
    private Vector2 lastTapPos;


    private CameraController Cam => CameraController.Instance;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameClear) return;
        if (StageBuilder.Instance != null && StageBuilder.Instance.IsGenerating) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#endif
        HandleTouch();
    }

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!allowOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            tracking = true;
            activePointerId = -1; // mouse
            startPos = Input.mousePosition;
            startTime = Time.unscaledTime;
        }
        else if (Input.GetMouseButtonUp(0) && tracking)
        {
            Vector2 endPos = Input.mousePosition;
            float dt = Time.unscaledTime - startTime;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            ProcessPointerEnd(endPos, dt, overUI);
            tracking = false;
        }
    }

    private void HandleTouch()
    {
        if (Input.touchCount <= 0) return;
        // Use the first touch that isn't over UI
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                if (!allowOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) continue;
                if (tracking) continue; // already tracking one
                tracking = true;
                activePointerId = t.fingerId;
                startPos = t.position;
                startTime = Time.unscaledTime;
            }
            else if (tracking && t.fingerId == activePointerId && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
            {
                Vector2 endPos = t.position;
                float dt = Time.unscaledTime - startTime;
                bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId);
                ProcessPointerEnd(endPos, dt, overUI);
                tracking = false;
                activePointerId = -1;
            }
        }
    }

    private void ProcessPointerEnd(Vector2 endPos, float duration, bool overUI)
    {
        float dist = Vector2.Distance(endPos, startPos);
        // Double-tap detection (quick tap, short move)
        if (duration <= maxTapDuration && dist <= doubleTapMaxDistance)
        {
            // Ignore taps over UI if requested
            if (suppressUndoOverUI && overUI)
            {
                // clear any pending tap so UI taps don't chain with gameplay taps
                lastTapTime = -10f;
                return;
            }

            if (Time.unscaledTime - lastTapTime <= doubleTapMaxDelay && Vector2.Distance(endPos, lastTapPos) <= doubleTapMaxDistance)
            {
                TryUndo();
                lastTapTime = -10f; // consume
                return;
            }
            else
            {
                lastTapTime = Time.unscaledTime;
                lastTapPos = endPos;
                return; // single tap ignored
            }
        }

        // Swipe detection
        if (dist < minSwipeDistance) return;
        Vector2 delta = endPos - startPos;
        Vector3 baseDir;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            baseDir = (delta.x > 0f) ? Vector3.right : Vector3.left;
        }
        else
        {
            baseDir = (delta.y > 0f) ? Vector3.forward : Vector3.back;
        }
        Vector3 worldDir = MapByCameraIndex(baseDir);
        ApplyMove(worldDir);
    }

    private void ApplyMove(Vector3 worldDir)
    {
        if (worldDir == Vector3.zero) return;
        // Avoid input during tweening if any player is animating
        var players = FindObjectsOfType<Player>();

        foreach (var p in players)
        {
            p.Direction = worldDir;
        }
    }

    private void TryUndo()
    {
        // Avoid undo during movement tweens
        foreach (var p in FindObjectsOfType<Player>())
        {
            if (DOTween.IsTweening(p.transform)) return;
        }
        UndoManager.Instance?.UndoForCurrentInput();
    }

    // Same mapping logic as Player.MapByCameraIndex
    private Vector3 MapByCameraIndex(Vector3 baseDir)
    {
        int idx = 0;
        if (Cam != null) idx = Cam.CurrentIndex;

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
}
