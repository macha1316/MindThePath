using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Lightweight animated UI background: floating soft squares drifting upward.
// Attach under a panel (RectTransform). The script spawns Image children and
// animates them in Update for minimal overhead. Designed for mobile.
public class UIFloatingBackground : MonoBehaviour
{
    [Header("Particles")]
    [SerializeField, Min(1)] private int count = 36;
    [SerializeField] private Vector2 sizeRange = new Vector2(8f, 26f);
    [SerializeField] private Vector2 speedYRange = new Vector2(12f, 36f); // px/sec
    [SerializeField] private Vector2 driftXRange = new Vector2(-10f, 10f); // px/sec
    [SerializeField] private Vector2 rotSpeedRange = new Vector2(-15f, 15f); // deg/sec
    [SerializeField] private Vector2 alphaRange = new Vector2(0.1f, 0.25f);
    [SerializeField] private float spawnPadding = 40f; // beyond rect to wrap
    [SerializeField] private bool pastelHSV = true; // use HSV palette

    [Header("Colors (optional)")]
    [SerializeField] private Color[] customPalette; // set to override HSV

    private RectTransform root;
    private readonly List<Particle> particles = new List<Particle>();
    private Sprite defaultSprite;
    private bool spawned;

    private struct Particle
    {
        public RectTransform rect;
        public float speedY;
        public float driftX;
        public float rotSpeed;
    }

    // Allow runtime override before activation
    public void SetCount(int c)
    {
        count = Mathf.Max(1, c);
    }

    private void Awake()
    {
        root = GetComponent<RectTransform>();
        if (root == null)
        {
            var rt = gameObject.AddComponent<RectTransform>();
            root = rt;
        }

        // Try to grab a built-in UI sprite so Images render without custom assets
        defaultSprite = null;
    }

    private void OnEnable()
    {
        if (!spawned)
        {
            SpawnAll();
            spawned = true;
        }
    }

    private void OnDisable()
    {
        // no-op, keep children for reuse when re-enabled
    }

    private void OnDestroy()
    {
        particles.Clear();
    }

    private void SpawnAll()
    {
        // Ensure this background sits behind other UI in the same panel
        transform.SetAsFirstSibling();

        var rect = root.rect;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("BG_Particle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Random size
            float size = Random.Range(sizeRange.x, sizeRange.y);
            rt.sizeDelta = new Vector2(size, size);

            // Random initial position within (and slightly beyond) bounds
            float x = Random.Range(-spawnPadding, rect.width + spawnPadding);
            float y = Random.Range(-spawnPadding, rect.height + spawnPadding);
            rt.anchoredPosition = new Vector2(x, y);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.sprite = defaultSprite; // safe fallback
            img.type = Image.Type.Sliced;
            img.color = NextColor();

            float speedY = Random.Range(speedYRange.x, speedYRange.y);
            float driftX = Random.Range(driftXRange.x, driftXRange.y);
            float rotSpd = Random.Range(rotSpeedRange.x, rotSpeedRange.y);

            particles.Add(new Particle
            {
                rect = rt,
                speedY = speedY,
                driftX = driftX,
                rotSpeed = rotSpd
            });
        }
    }

    private void Update()
    {
        if (particles.Count == 0) return;
        var rect = root.rect;
        float w = rect.width;
        float h = rect.height;
        float dt = Time.unscaledDeltaTime; // keep moving even if game paused

        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            var pos = p.rect.anchoredPosition;
            pos.x += p.driftX * dt;
            pos.y += p.speedY * dt;

            // Wrap horizontally
            if (pos.x < -spawnPadding) pos.x = w + spawnPadding;
            else if (pos.x > w + spawnPadding) pos.x = -spawnPadding;

            // Wrap vertically (bottom to top)
            if (pos.y > h + spawnPadding)
            {
                pos.y = -spawnPadding;
                // Occasionally vary appearance on wrap
                float size = Random.Range(sizeRange.x, sizeRange.y);
                p.rect.sizeDelta = new Vector2(size, size);
                p.driftX = Random.Range(driftXRange.x, driftXRange.y);
                p.speedY = Random.Range(speedYRange.x, speedYRange.y);
                SetImageColor(p.rect, NextColor());
            }

            p.rect.anchoredPosition = pos;

            if (Mathf.Abs(p.rotSpeed) > 0.01f)
            {
                p.rect.Rotate(0f, 0f, p.rotSpeed * dt, Space.Self);
            }

            particles[i] = p;
        }
    }

    private void SetImageColor(RectTransform rect, Color c)
    {
        var img = rect.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private Color NextColor()
    {
        if (customPalette != null && customPalette.Length > 0)
        {
            var baseCol = customPalette[Random.Range(0, customPalette.Length)];
            float a = Random.Range(alphaRange.x, alphaRange.y);
            baseCol.a = a;
            return baseCol;
        }

        if (pastelHSV)
        {
            float h = Random.value;
            float s = Random.Range(0.3f, 0.65f);
            float v = Random.Range(0.8f, 1f);
            Color c = Color.HSVToRGB(h, s, v);
            c.a = Random.Range(alphaRange.x, alphaRange.y);
            return c;
        }

        // Fallback: soft white
        return new Color(1f, 1f, 1f, Random.Range(alphaRange.x, alphaRange.y));
    }
}
