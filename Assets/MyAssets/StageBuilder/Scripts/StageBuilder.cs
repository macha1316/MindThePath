using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using System.Collections;

public class StageBuilder : MonoBehaviour
{
    private List<GameObject> canvasObjects = new List<GameObject>();
    private List<GameObject> modelObjects = new List<GameObject>();

    public const float BLOCK_SIZE = 2.0f;
    public const float HEIGHT_OFFSET = 2.0f;

    // デバッグ用
    [SerializeField] string csvFileName = "Stages/Stage1";
    [SerializeField] GameObject blockPrefab;
    [SerializeField] GameObject fragileBlockPrefab; // 'F' 一度乗ると消える床
    [SerializeField] GameObject goalPrefab;
    [SerializeField] GameObject nonePrefab;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject moveBoxPrefab;
    [SerializeField] GameObject lavaPrefab;
    [SerializeField] GameObject teleportPrefab; // 'A' テレポート床
    private char[,,] gridData;
    public int stageNumber = 0;

    // UIPrefab
    [SerializeField] string[] textAssets;
    public GameObject stageRoot; // ステージ親

    // コルーチンによるブロック生成管理用
    private int remainingBlocksToSpawn = 0;
    public bool IsGenerating { get; private set; } = false;

    // 生成アニメーション設定（ぼこぼこ降ってくる感じ）
    [Header("Spawn Animation")]
    [SerializeField] private Vector2 spawnDropHeightRange = new Vector2(5f, 9f); // 下降開始の高さ範囲
    [SerializeField] private Vector2 spawnDropDurationRange = new Vector2(0.25f, 0.6f); // 落下時間の範囲
    [SerializeField] private float spawnFadeDuration = 0.35f; // フェード時間
    [SerializeField] private Ease spawnDropEase = Ease.OutCubic; // 落下イージング
    [SerializeField] private float spawnDelayRandom = 0.35f; // ランダムな遅延
    [SerializeField] private float spawnNoiseScale = 0.15f; // ノイズスケール
    [SerializeField] private float spawnNoiseAmplitude = 0.35f; // ノイズ遅延の強さ
    [SerializeField] private float spawnRotJitter = 6f; // 落下中の傾き（度）
    [SerializeField] private float spawnLandingSquash = 0.1f; // 着地時のつぶれ量
    [SerializeField] private float spawnLandingSquashDuration = 0.08f; // つぶれ時間

    // マテリアルの一時的な透明化制御用
    private struct MaterialRenderState
    {
        public bool valid;
        public bool hasMode; public float mode;
        public bool hasSurface; public float surface;
        public bool hasBlend; public float blend;
        public bool hasSrcBlend; public int srcBlend;
        public bool hasDstBlend; public int dstBlend;
        public bool hasZWrite; public int zWrite;
        public bool keywordAlphaTest;
        public bool keywordAlphaBlend;
        public bool keywordAlphaPremul;
        public int renderQueue;
    }

    private MaterialRenderState MakeTransparent(Material mat)
    {
        var s = new MaterialRenderState { valid = true };
        s.renderQueue = mat.renderQueue;
        s.keywordAlphaTest = mat.IsKeywordEnabled("_ALPHATEST_ON");
        s.keywordAlphaBlend = mat.IsKeywordEnabled("_ALPHABLEND_ON");
        s.keywordAlphaPremul = mat.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");

        if (mat.HasProperty("_Mode")) { s.hasMode = true; s.mode = mat.GetFloat("_Mode"); mat.SetFloat("_Mode", 2f); } // Standard: Fade
        if (mat.HasProperty("_Surface")) { s.hasSurface = true; s.surface = mat.GetFloat("_Surface"); mat.SetFloat("_Surface", 1f); } // URP: Transparent
        if (mat.HasProperty("_Blend")) { s.hasBlend = true; s.blend = mat.GetFloat("_Blend"); mat.SetFloat("_Blend", 0f); } // URP: Alpha blend
        if (mat.HasProperty("_SrcBlend")) { s.hasSrcBlend = true; s.srcBlend = mat.GetInt("_SrcBlend"); mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); }
        if (mat.HasProperty("_DstBlend")) { s.hasDstBlend = true; s.dstBlend = mat.GetInt("_DstBlend"); mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); }
        if (mat.HasProperty("_ZWrite")) { s.hasZWrite = true; s.zWrite = mat.GetInt("_ZWrite"); mat.SetInt("_ZWrite", 0); }

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000; // Transparent queue
        return s;
    }

    private void RestoreRenderState(Material mat, MaterialRenderState s)
    {
        if (!s.valid) return;
        if (s.hasMode) mat.SetFloat("_Mode", s.mode);
        if (s.hasSurface) mat.SetFloat("_Surface", s.surface);
        if (s.hasBlend) mat.SetFloat("_Blend", s.blend);
        if (s.hasSrcBlend) mat.SetInt("_SrcBlend", s.srcBlend);
        if (s.hasDstBlend) mat.SetInt("_DstBlend", s.dstBlend);
        if (s.hasZWrite) mat.SetInt("_ZWrite", s.zWrite);

        if (s.keywordAlphaTest) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");
        if (s.keywordAlphaBlend) mat.EnableKeyword("_ALPHABLEND_ON"); else mat.DisableKeyword("_ALPHABLEND_ON");
        if (s.keywordAlphaPremul) mat.EnableKeyword("_ALPHAPREMULTIPLY_ON"); else mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = s.renderQueue;
    }

    // セル種別の定義（マジックナンバー回避）
    public static class Cell
    {
        public const char Empty = 'N';
        public const char Block = 'B';
        public const char Goal = 'G';
        public const char Player = 'P';
        public const char MoveBox = 'M';
        public const char Fragile = 'F';
        public const char Lava = 'O';
        public const char Teleport = 'A';
    }

    // Stage情報をロード & UIをStage情報に合わせて出す
    public void CreateStage(int stageNumberProp)
    {
        stageNumber = stageNumberProp;
        StageSelectUI.Instance.SelectStageUI(stageNumber);
        GameManager.Instance.SetGameStop();
        UndoManager.Instance?.Clear();
        gridData = null;
        IsGenerating = true;
        canvasObjects.Clear();
        modelObjects.Clear();
        AudioManager.Instance.SelectStageSound();
        LoadStage(textAssets[stageNumber]);

        bool isFirstPlay = !PlayerPrefs.HasKey(PlayerPrefsManager.FirstPlayKey);
        if (isFirstPlay)
        {
            PlayerPrefs.SetInt(PlayerPrefsManager.FirstPlayKey, 1);
            PlayerPrefs.Save();
            StageSelectUI.Instance.ShowTutorialUI();
        }
    }

    public void ReCreateStage()
    {
        if (IsGenerating) return;
        if (stageRoot != null)
        {
            foreach (Transform child in stageRoot.transform)
            {
                Destroy(child.gameObject);
            }
        }
        CameraController.Instance.SwitchTo3DView();
        CameraController.Instance.CurrentIndex = 1;
        CameraController.Instance.RotateLeft();
        GameManager.Instance.Is2DMode = false;
        CreateStage(stageNumber);
    }

    public static StageBuilder Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void LoadStage(string filePath)
    {
        TextAsset csvFile = Resources.Load<TextAsset>(filePath);
        if (csvFile == null)
        {
            Debug.LogError($"CSVファイルが見つかりません: {filePath}");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        List<string[]> layers = new List<string[]>();
        List<string> currentLayer = new List<string>();

        // 空行ごとにレイヤーを区切る
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentLayer.Count > 0)
                {
                    layers.Add(currentLayer.ToArray());
                    currentLayer.Clear();
                }
            }
            else
            {
                currentLayer.Add(line);
            }
        }
        if (currentLayer.Count > 0)
        {
            layers.Add(currentLayer.ToArray());
        }

        GenerateStage(layers);
    }

    void GenerateStage(List<string[]> layers)
    {
        int heightCount = layers.Count;
        int rowCount = layers[0].Length;
        int colCount = layers[0][0].Split(',').Length;

        int totalHeight = heightCount + 3;
        // gridData を初期化
        gridData = new char[colCount, totalHeight, rowCount];

        int delayCounter = 0; // 既存の直列感を弱めるため、下の遅延はノイズ＋ジッタ主体
        remainingBlocksToSpawn = 0;

        for (int height = 0; height < heightCount; height++)
        {
            string[] layer = layers[height];

            for (int row = 0; row < rowCount; row++)
            {
                string[] cells = layer[row].Split(',');

                for (int col = 0; col < colCount; col++)
                {
                    string cellTypeString = cells[col];
                    char cellType = cellTypeString[0];
                    Vector3 position = new Vector3(col * BLOCK_SIZE, height * HEIGHT_OFFSET, (rowCount - 1 - row) * BLOCK_SIZE);

                    if (cellTypeString[0] != 'N')
                    {
                        remainingBlocksToSpawn++;
                        float n = Mathf.PerlinNoise(position.x * spawnNoiseScale, position.z * spawnNoiseScale); // 0..1
                        float noiseDelay = n * spawnNoiseAmplitude; // 0..amp
                        float jitter = UnityEngine.Random.Range(0f, spawnDelayRandom);
                        float delay = noiseDelay + jitter;
                        StartCoroutine(SpawnBlockWithDelay(cellTypeString, position, delay));
                        delayCounter++;
                    }
                    else
                    {
                        StartCoroutine(SpawnBlockWithDelay(cellTypeString, position, 0f));
                    }

                    int correctedRow = rowCount - 1 - row;

                    gridData[col, height, correctedRow] = cellType;
                }
            }
        }

        for (int h = heightCount; h < heightCount + 3; h++)
        {
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    gridData[c, h, r] = 'N';
                }
            }
        }
        // ステージ生成完了イベントの発火はコルーチン完了時に行う
    }

    IEnumerator SpawnBlockWithDelay(string cellTypeString, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnBlock(cellTypeString, position);

        if (cellTypeString[0] != 'N')
        {
            remainingBlocksToSpawn--;
            if (remainingBlocksToSpawn == 0)
            {
                IsGenerating = false;
            }
        }
    }

    void SpawnBlock(string cellTypeString, Vector3 position)
    {
        char cellType = cellTypeString[0];
        char dirChar = cellTypeString.Length > 1 ? cellTypeString[1] : ' ';
        GameObject prefab = null;
        switch (cellType)
        {
            case 'B':
                prefab = blockPrefab;
                break;
            case 'G':
                prefab = goalPrefab;
                break;
            case 'N':
                prefab = nonePrefab;
                break;
            case 'A':
                prefab = teleportPrefab != null ? teleportPrefab : blockPrefab;
                break;
            case 'F':
                prefab = fragileBlockPrefab != null ? fragileBlockPrefab : blockPrefab;
                break;
            case 'P':
                prefab = playerPrefab;
                break;
            case 'M':
                prefab = moveBoxPrefab;
                break;
            case 'O':
                prefab = lavaPrefab;
                break;
        }
        if (prefab != null)
        {
            // ルートは最終位置に生成し、見た目は子のみを上から降ろす＆フェード
            GameObject obj = Instantiate(prefab, position, Quaternion.identity, stageRoot != null ? stageRoot.transform : null);

            Transform marker = obj.transform.Find("Marker");
            if (marker != null) marker.gameObject.SetActive(false);

            if (cellType == 'P')
            {
                Player newP = obj.AddComponent<Player>();
                GameManager.Instance.GetPlayer(newP);
            }
            if (cellType == 'M')
            {
                MoveBox newM = obj.AddComponent<MoveBox>();
                GameManager.Instance.GetMoveBox(newM);
            }
            if (cellType == 'F') obj.AddComponent<FragileBlock>();

            Vector3 dir = Vector3.forward;
            switch (dirChar)
            {
                case '^': dir = Vector3.forward; break;
                case 'v': dir = Vector3.back; break;
                case '<': dir = Vector3.left; break;
                case '>': dir = Vector3.right; break;
            }
            obj.transform.forward = dir;

            // 子オブジェクト分類（Canvasは非表示、モデルは表示＆アニメ対象）
            var modelChildren = new List<Transform>();
            foreach (Transform child in obj.transform)
            {
                Canvas canvas = child.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvasObjects.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
                else
                {
                    modelObjects.Add(child.gameObject);
                    child.gameObject.SetActive(true);
                    modelChildren.Add(child);
                }
            }

            // 空セルは演出不要
            if (cellType == 'N') return;

            // ぼこぼこ感: 子ごとに高さ/時間/回転をランダム化
            foreach (var t in modelChildren)
            {
                float dropHeight = UnityEngine.Random.Range(spawnDropHeightRange.x, spawnDropHeightRange.y);
                float dropDuration = UnityEngine.Random.Range(spawnDropDurationRange.x, spawnDropDurationRange.y);

                Vector3 originalLocal = t.localPosition;
                Vector3 originalScale = t.localScale;
                Vector3 originalEuler = t.localEulerAngles;

                // 落下開始位置 + 回転ジッタ
                t.localPosition = originalLocal + Vector3.up * dropHeight;
                Vector3 jitterEuler = originalEuler + new Vector3(
                    UnityEngine.Random.Range(-spawnRotJitter, spawnRotJitter),
                    0f,
                    UnityEngine.Random.Range(-spawnRotJitter, spawnRotJitter)
                );
                t.localEulerAngles = jitterEuler;

                // 落下と姿勢復帰
                t.DOLocalMoveY(originalLocal.y, dropDuration).SetEase(spawnDropEase).OnComplete(() =>
                {
                    // 着地時のつぶれ（軽くバウンド感）
                    if (spawnLandingSquash > 0f)
                    {
                        Vector3 squash = new Vector3(spawnLandingSquash, -spawnLandingSquash * 1.5f, spawnLandingSquash);
                        t.DOScale(originalScale + squash, spawnLandingSquashDuration).SetLoops(2, LoopType.Yoyo);
                    }
                });
                t.DOLocalRotate(originalEuler, dropDuration).SetEase(Ease.OutQuad);
            }

            // フェードイン（マテリアルを一時Transparentにしてα0→1）
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    var state = MakeTransparent(mat);

                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color c0 = mat.GetColor("_BaseColor");
                        Color start = new Color(c0.r, c0.g, c0.b, 0f);
                        Color end = new Color(c0.r, c0.g, c0.b, 1f);
                        mat.SetColor("_BaseColor", start);
                        mat.DOColor(end, "_BaseColor", spawnFadeDuration).OnComplete(() => RestoreRenderState(mat, state));
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        Color c0 = mat.color;
                        Color start = new Color(c0.r, c0.g, c0.b, 0f);
                        Color end = new Color(c0.r, c0.g, c0.b, 1f);
                        mat.color = start;
                        mat.DOColor(end, spawnFadeDuration).OnComplete(() => RestoreRenderState(mat, state));
                    }
                    else
                    {
                        // フェード不可なら設定を戻す
                        RestoreRenderState(mat, state);
                    }
                }
            }
        }
    }

    // === Public spawn helpers (for Undo etc.) ===
    public void SpawnFragileAt(Vector3 worldPosition)
    {
        SpawnBlock("F", worldPosition);
    }

    // Toggle view modes
    public void SwitchTo2DView()
    {
        foreach (var obj in canvasObjects) obj.SetActive(true);
        foreach (var obj in modelObjects) obj.SetActive(false);
    }

    public void SwitchTo3DView()
    {
        foreach (var obj in canvasObjects) obj.SetActive(false);
        foreach (var obj in modelObjects) obj.SetActive(true);
    }

    public void ResetGridData()
    {
        for (int h = 0; h < gridData.GetLength(1); h++)
        {
            for (int r = 0; r < gridData.GetLength(2); r++)
            {
                for (int c = 0; c < gridData.GetLength(0); c++)
                {
                    // ブロック（B）、ゴール（G）、溶岩（O）、消える床（F）、テレポート（A）を保持し、それ以外をリセット
                    if (gridData[c, h, r] != 'B' && gridData[c, h, r] != 'G' && gridData[c, h, r] != 'O' && gridData[c, h, r] != 'F' && gridData[c, h, r] != 'A')
                    {
                        gridData[c, h, r] = 'N';
                    }
                }
            }
        }
    }

    // === Grid copy/restore (for Undo) ===
    public char[,,] GetGridDataCopy()
    {
        if (gridData == null) return null;
        int xLen = gridData.GetLength(0);
        int yLen = gridData.GetLength(1);
        int zLen = gridData.GetLength(2);
        var copy = new char[xLen, yLen, zLen];
        for (int x = 0; x < xLen; x++)
            for (int y = 0; y < yLen; y++)
                for (int z = 0; z < zLen; z++)
                    copy[x, y, z] = gridData[x, y, z];
        return copy;
    }

    public void SetGridData(char[,,] source)
    {
        if (source == null) return;
        if (gridData == null) return;
        if (source.GetLength(0) != gridData.GetLength(0) ||
            source.GetLength(1) != gridData.GetLength(1) ||
            source.GetLength(2) != gridData.GetLength(2))
        {
            Debug.LogWarning("Grid size mismatch on SetGridData; skipping.");
            return;
        }
        for (int x = 0; x < gridData.GetLength(0); x++)
            for (int y = 0; y < gridData.GetLength(1); y++)
                for (int z = 0; z < gridData.GetLength(2); z++)
                    gridData[x, y, z] = source[x, y, z];
    }

    public Vector3 WorldFromGrid(Vector3Int cell)
    {
        return new Vector3(cell.x * BLOCK_SIZE, cell.y * HEIGHT_OFFSET, cell.z * BLOCK_SIZE);
    }

    public void RebuildFragilesFromGrid()
    {
        // Map existing fragile objects by grid cell
        var existing = new Dictionary<Vector3Int, FragileBlock>();
        foreach (var frag in GameObject.FindObjectsOfType<FragileBlock>())
        {
            var g = GridFromPosition(frag.transform.position);
            existing[g] = frag;
        }

        // Destroy fragiles where grid no longer has 'F'
        foreach (var kv in existing)
        {
            var cell = kv.Key;
            if (gridData[cell.x, cell.y, cell.z] != 'F')
            {
                Destroy(kv.Value.gameObject);
            }
        }

        // Ensure fragiles exist where grid has 'F'
        for (int x = 0; x < gridData.GetLength(0); x++)
        {
            for (int y = 0; y < gridData.GetLength(1); y++)
            {
                for (int z = 0; z < gridData.GetLength(2); z++)
                {
                    if (gridData[x, y, z] == 'F')
                    {
                        var cell = new Vector3Int(x, y, z);
                        if (!existing.ContainsKey(cell))
                        {
                            SpawnFragileAt(WorldFromGrid(cell));
                        }
                    }
                }
            }
        }
    }

    public char GetGridCharType(Vector3 pos)
    {
        int col = Mathf.RoundToInt(pos.x / BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / BLOCK_SIZE);

        return gridData[col, height, row];
    }

    // setメソッド
    public void UpdateGridAtPosition(Vector3 worldPosition, char type)
    {
        int col = Mathf.RoundToInt(worldPosition.x / BLOCK_SIZE);
        int height = Mathf.RoundToInt(worldPosition.y / HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(worldPosition.z / BLOCK_SIZE);

        gridData[col, height, row] = type;
    }

    // grid範囲内かの判定
    public bool IsValidGridPosition(Vector3 worldPosition)
    {
        int col = Mathf.RoundToInt(worldPosition.x / BLOCK_SIZE);
        int height = Mathf.RoundToInt(worldPosition.y / HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(worldPosition.z / BLOCK_SIZE);

        return col >= 0 && col < gridData.GetLength(0) &&
               height >= 0 && height < gridData.GetLength(1) &&
               row >= 0 && row < gridData.GetLength(2);
    }

    // 個別でセルタイプを確認
    public bool IsMatchingCellType(Vector3 pos, char cellType)
    {
        int col = Mathf.RoundToInt(pos.x / BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / BLOCK_SIZE);

        return gridData[col, height, row] == cellType;
    }

    // 複数のセルタイプを確認 一つでもマッチすればtrue
    public bool IsAnyMatchingCellType(Vector3 pos, params char[] types)
    {
        foreach (char type in types)
        {
            if (IsMatchingCellType(pos, type)) return true;
        }
        return false;
    }

    // === 便利関数（重複ロジックの集約） ===
    // 指定位置直下から下方向に探索し、最初に見つかる "落下を止める床" の直上まで落とした座標を返す
    // goalIsAir=true の場合、'G' も空気として扱い、更に下まで落下させる（既存挙動互換）
    public Vector3 FindDropPosition(Vector3 startWorldPos, bool goalIsAir)
    {
        Vector3 pos = startWorldPos;
        while (IsValidGridPosition(pos + Vector3.down * HEIGHT_OFFSET))
        {
            Vector3 below = pos + Vector3.down * HEIGHT_OFFSET;
            char t = GetGridCharType(below);
            bool canFall = (t == Cell.Empty) || (goalIsAir && t == Cell.Goal);
            if (!canFall) break;
            pos = below;
        }
        return pos;
    }

    // 指定位置の直下に一つでも"空気でない"セルが存在するか（支えがあるか）
    public bool HasAnySupportBelow(Vector3 worldPos)
    {
        Vector3 check = worldPos + Vector3.down * HEIGHT_OFFSET;
        while (IsValidGridPosition(check))
        {
            if (GetGridCharType(check) != Cell.Empty)
            {
                return true;
            }
            check += Vector3.down * HEIGHT_OFFSET;
        }
        return false;
    }

    // 指定ワールド座標のグリッドセルに存在する MoveBox を取得
    public bool TryGetMoveBoxAtPosition(Vector3 worldPosition, out MoveBox found)
    {
        var targetCell = GridFromPosition(worldPosition);
        foreach (var box in GameObject.FindObjectsOfType<MoveBox>())
        {
            if (GridFromPosition(box.transform.position) == targetCell)
            {
                found = box;
                return true;
            }
        }
        found = null;
        return false;
    }

    // 指定セルの他方の同タイプセルを探す（例: 'A' テレポートの相方）
    public bool TryFindOtherCell(char type, Vector3Int fromCell, out Vector3Int otherCell)
    {
        otherCell = default;
        int xLen = gridData.GetLength(0);
        int yLen = gridData.GetLength(1);
        int zLen = gridData.GetLength(2);
        for (int x = 0; x < xLen; x++)
        {
            for (int y = 0; y < yLen; y++)
            {
                for (int z = 0; z < zLen; z++)
                {
                    if (gridData[x, y, z] == type)
                    {
                        if (x != fromCell.x || y != fromCell.y || z != fromCell.z)
                        {
                            otherCell = new Vector3Int(x, y, z);
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    // 複数のセルタイプを全て満たすか確認 全てマッチすればtrue
    public bool IsAllMatchingCellTypes(Vector3 pos, params char[] types)
    {
        foreach (char type in types)
        {
            if (!IsMatchingCellType(pos, type)) return false;
        }
        return true;
    }

    public Vector3Int GridFromPosition(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(pos.x / BLOCK_SIZE),
            Mathf.RoundToInt(pos.y / HEIGHT_OFFSET),
            Mathf.RoundToInt(pos.z / BLOCK_SIZE)
        );
    }

    public void BuildNextStage()
    {
        if (stageRoot != null)
        {
            foreach (Transform child in stageRoot.transform)
            {
                Destroy(child.gameObject);
            }
        }
        CreateStage(stageNumber + 1);
    }

    public void DestroyStage()
    {
        if (stageRoot != null)
        {
            foreach (Transform child in stageRoot.transform)
            {
                Destroy(child.gameObject);
            }
        }
        gridData = null;
    }

    public char GetTopCellTypeAt(Vector3 position)
    {
        int col = Mathf.RoundToInt(position.x / BLOCK_SIZE);
        int row = Mathf.RoundToInt(position.z / BLOCK_SIZE);

        for (int height = gridData.GetLength(1) - 1; height >= 0; height--)
        {
            char cell = gridData[col, height, row];
            if (cell != 'N')
            {
                return cell;
            }
        }
        return 'N';
    }

    public Vector3 GetTopCellPosition(Vector3 position)
    {
        int col = Mathf.RoundToInt(position.x / BLOCK_SIZE);
        int row = Mathf.RoundToInt(position.z / BLOCK_SIZE);

        for (int height = gridData.GetLength(1) - 1; height >= 0; height--)
        {
            char cell = gridData[col, height, row];
            if (cell != 'N')
            {
                return new Vector3(col * BLOCK_SIZE, height * HEIGHT_OFFSET, row * BLOCK_SIZE);
            }
        }

        // デフォルトで最下層を返す
        return new Vector3(col * BLOCK_SIZE, 0f, row * BLOCK_SIZE);
    }

    private void OnDrawGizmos()
    {
        if (gridData == null) return;

        for (int x = 0; x < gridData.GetLength(0); x++)
        {
            for (int y = 0; y < gridData.GetLength(1); y++)
            {
                for (int z = 0; z < gridData.GetLength(2); z++)
                {
                    char cell = gridData[x, y, z];
                    if (cell == 'N') continue;

                    Vector3 pos = new Vector3(x * BLOCK_SIZE, y * HEIGHT_OFFSET + 0.5f, z * BLOCK_SIZE);

                    if (cell == 'G')
                    {
                        Gizmos.color = Color.red;
                    }
                    else if (cell == 'P')
                    {
                        Gizmos.color = Color.green;
                    }
                    else if (cell == 'M')
                    {
                        Gizmos.color = Color.yellow;
                    }
                    else if (cell == 'F')
                    {
                        Gizmos.color = Color.magenta;
                    }
                    else if (cell == 'A')
                    {
                        Gizmos.color = Color.cyan;
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                    }

                    Gizmos.DrawWireCube(pos, Vector3.one * 1.0f);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.3f, cell.ToString());
#endif
                }
            }
        }
    }
}
