using UnityEngine;
using DG.Tweening;

public class ConfettiManager : MonoBehaviour
{
    public static ConfettiManager Instance;

    [Header("Prefab")]
    [SerializeField] private GameObject confettiPrefab; // 任意（設定時はPrefabをInstantiate）

    [Header("Default Confetti Settings")]
    [SerializeField] private int defaultCount = 42;
    [SerializeField, Min(0.05f)] private float defaultSpeed = 1.4f; // 基本インパルス倍率（大きいほど速く遠く）
    [SerializeField, Min(0.05f)] private float defaultSpread = 1.8f; // 到達距離の基準
    [SerializeField, Range(0f, 180f)] private float defaultArcDeg = 25f; // コーン角（小さめでクラッカー感）
    [SerializeField] private Vector2 defaultSizeRange = new Vector2(0.14f, 0.28f);

    [Header("Physics")]
    [SerializeField] private bool usePhysics = true;
    [SerializeField, Min(0)] private float baseImpulse = 6.0f;
    [SerializeField, Min(0)] private float spiralFactor = 3.5f; // 接線方向の成分（巻く感じ）
    [SerializeField, Min(0)] private float torqueImpulse = 2.0f;
    [SerializeField] private PhysicsMaterial confettiMaterial; // 任意（未設定ならランタイム生成）
    [SerializeField] private Vector2 lifeTimeRange = new Vector2(1.8f, 3.2f);
    [SerializeField] private Vector3 origin = new Vector3(10, 15, 8);
    [SerializeField] private Vector3 origin2 = new Vector3(10, 15, 15);
    [SerializeField] private Vector3 mainDirection = new Vector3(0, 0, 0);


    [Header("Launch Pitch")]
    [SerializeField] private bool useFixedLaunchPitch = true; // 水平から一定角度で打ち上げ
    [SerializeField, Range(-180f, 85f)] private float launchPitchDeg = 45f; // 0=水平, 90=真上

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // デフォルト値での簡易呼び出し
    public void SpawnBurst()
    {
        SpawnBurst(origin, mainDirection, defaultCount, defaultSpeed, defaultSpread, defaultArcDeg, defaultSizeRange);
        SpawnBurst(origin2, mainDirection, defaultCount, defaultSpeed, defaultSpread, defaultArcDeg, defaultSizeRange);

    }

    public void SpawnBurst(
        Vector3 origin,
        Vector3 mainDirection,
        int count,
        float speed,
        float spread,
        float arcDeg,
        Vector2 sizeRange)
    {
        if (count <= 0) return;
        if (sizeRange.x > sizeRange.y) (sizeRange.x, sizeRange.y) = (sizeRange.y, sizeRange.x);

        Vector3 forward = mainDirection.sqrMagnitude > 0.0001f ? mainDirection.normalized : Vector3.forward;

        // 水平から固定の仰角で発射（クラッカーらしさ）
        if (useFixedLaunchPitch)
        {
            Vector3 yaw = new Vector3(forward.x, 0f, forward.z);
            if (yaw.sqrMagnitude < 1e-4f) yaw = Vector3.forward; // 垂直に近いときのフォールバック
            yaw.Normalize();
            float rad = Mathf.Deg2Rad * Mathf.Clamp(launchPitchDeg, 0f, 85f);
            forward = yaw * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad);
            forward.Normalize();
        }

        // 物理マテリアルの準備
        PhysicsMaterial pm = confettiMaterial;
        if (pm == null)
        {
            pm = new PhysicsMaterial("ConfettiPM")
            {
                bounciness = 0.2f,
                bounceCombine = PhysicsMaterialCombine.Multiply,
                dynamicFriction = 0.4f,
                staticFriction = 0.5f,
                frictionCombine = PhysicsMaterialCombine.Average
            };
        }

        for (int i = 0; i < count; i++)
        {
            GameObject go;
            go = Instantiate(confettiPrefab, origin, Quaternion.identity);

            go.name = "ConfettiCube";
            go.transform.localScale = Vector3.one * Random.Range(sizeRange.x, sizeRange.y);

            // カラー
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var m = rend.material;
                Color c = Color.HSVToRGB(Random.value, 0.7f, 1f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                else if (m.HasProperty("_Color")) m.color = c;
            }

            // Collider（既定でBoxCollider付き）設定
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                col.sharedMaterial = pm;
            }

            if (usePhysics)
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null) rb = go.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.mass = 0.02f;
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.05f;

                // コーン内方向 + 接線方向成分（巻く）
                Vector3 dir = RandomDirectionInCone(forward, arcDeg);
                // 接線ベクトル（forwardに直交）
                Vector3 ortho = Vector3.Cross(dir, Vector3.up);
                if (ortho.sqrMagnitude < 1e-4f) ortho = Vector3.Cross(dir, Vector3.right);
                ortho.Normalize();
                Vector3 tangential = Vector3.Cross(ortho, dir).normalized; // dirに直交しつつ回り込む方向

                float impulse = baseImpulse * Mathf.Max(0.1f, speed) * Random.Range(0.8f, 1.2f);
                Vector3 velocity = dir * impulse + tangential * spiralFactor;
                rb.AddForce(velocity, ForceMode.Impulse);
                rb.AddTorque(Random.onUnitSphere * torqueImpulse, ForceMode.Impulse);

                // ライフタイム後に消滅（縮小演出付き）
                float life = Random.Range(lifeTimeRange.x, lifeTimeRange.y);
                DOVirtual.DelayedCall(Mathf.Max(0.1f, life - 0.2f), () =>
                {
                    if (go == null) return;
                    go.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => { if (go != null) Destroy(go); });
                });
            }
            else
            {
                // 旧DOJump方式（非物理）
                Vector3 dir = RandomDirectionInCone(forward, arcDeg);
                float dist = Random.Range(spread * 0.6f, spread * 1.2f) * Mathf.Max(0.1f, speed);
                Vector3 dst = origin + dir * dist;
                float duration = Random.Range(0.35f, 0.6f) / Mathf.Max(0.1f, speed);
                float jumpH = Random.Range(0.4f, 0.9f) * Mathf.Clamp(speed, 0.4f, 2.5f);
                go.transform.DOJump(dst, jumpH, 1, duration)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        if (go == null) return;
                        go.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => { if (go != null) Destroy(go); });
                    });
            }
        }
    }

    private Vector3 RandomDirectionInCone(Vector3 forward, float angleDeg)
    {
        forward = forward.normalized;
        if (forward == Vector3.zero) forward = Vector3.up;
        float theta = Random.Range(0f, Mathf.Deg2Rad * Mathf.Clamp(angleDeg, 0f, 180f));
        float phi = Random.Range(0f, Mathf.PI * 2f);
        Vector3 ortho1 = Vector3.Cross(forward, Vector3.up);
        if (ortho1.sqrMagnitude < 1e-4f) ortho1 = Vector3.Cross(forward, Vector3.right);
        ortho1.Normalize();
        Vector3 ortho2 = Vector3.Cross(forward, ortho1).normalized;
        Vector3 dir = Mathf.Cos(theta) * forward + Mathf.Sin(theta) * (Mathf.Cos(phi) * ortho1 + Mathf.Sin(phi) * ortho2);
        return dir.normalized;
    }
}
