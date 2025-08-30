using System;
using System.Collections.Generic;
using UnityEngine;
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
    private char[,,] gridData;
    private char[,,] dynamicTiles;
    public int stageNumber = 0;

    // UIPrefab
    [SerializeField] string[] textAssets;
    public GameObject stageRoot; // ステージ親

    // コルーチンによるブロック生成管理用
    private int remainingBlocksToSpawn = 0;
    public bool IsGenerating { get; private set; } = false;

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
    }

    // Stage情報をロード & UIをStage情報に合わせて出す
    public void CreateStage(int stageNumberProp)
    {
        stageNumber = stageNumberProp;
        StageSelectUI.Instance.SelectStageUI(stageNumber);
        GameManager.Instance.SetGameStop();
        UndoManager.Instance?.Clear();
        gridData = null;
        dynamicTiles = null;
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
        dynamicTiles = new char[colCount, totalHeight, rowCount];

        int delayCounter = 0;
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
                        StartCoroutine(SpawnBlockWithDelay(cellTypeString, position, delayCounter * 0.01f));
                        delayCounter++;
                    }
                    else
                    {
                        StartCoroutine(SpawnBlockWithDelay(cellTypeString, position, 0f));
                    }

                    int correctedRow = rowCount - 1 - row;

                    gridData[col, height, correctedRow] = cellType;
                    dynamicTiles[col, height, correctedRow] = cellType;
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
                TurnManager.Instance.StartMove();
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
            GameObject obj = Instantiate(prefab, position, Quaternion.identity, stageRoot != null ? stageRoot.transform : null);
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            Transform marker = obj.transform.Find("Marker");
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }

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
            if (cellType == 'F')
            {
                obj.AddComponent<FragileBlock>();
            }
            Vector3 dir = Vector3.forward;
            switch (dirChar)
            {
                case '^': dir = Vector3.forward; break;
                case 'v': dir = Vector3.back; break;
                case '<': dir = Vector3.left; break;
                case '>': dir = Vector3.right; break;
            }
            obj.transform.forward = dir;

            foreach (Transform child in obj.transform)
            {
                Canvas canvas = child.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvasObjects.Add(child.gameObject);
                    child.gameObject.SetActive(false); // start hidden
                }
                else
                {
                    modelObjects.Add(child.gameObject);
                    child.gameObject.SetActive(true); // default visible
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
                    // ブロック（B）、ゴール（G）、溶岩（O）、消える床（F）を保持し、それ以外をリセット
                    if (gridData[c, h, r] != 'B' && gridData[c, h, r] != 'G' && gridData[c, h, r] != 'O' && gridData[c, h, r] != 'F')
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

    public char GetDynamicGridCharType(Vector3 pos)
    {
        int col = Mathf.RoundToInt(pos.x / BLOCK_SIZE);
        int height = Mathf.RoundToInt(pos.y / HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(pos.z / BLOCK_SIZE);

        return dynamicTiles[col, height, row];
    }

    // setメソッド
    public void UpdateGridAtPosition(Vector3 worldPosition, char type)
    {
        int col = Mathf.RoundToInt(worldPosition.x / BLOCK_SIZE);
        int height = Mathf.RoundToInt(worldPosition.y / HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(worldPosition.z / BLOCK_SIZE);

        gridData[col, height, row] = type;
    }

    // setメソッド
    public void UpdateDynamicTileAtPosition(Vector3 worldPosition, char type)
    {
        int col = Mathf.RoundToInt(worldPosition.x / BLOCK_SIZE);
        int height = Mathf.RoundToInt(worldPosition.y / HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(worldPosition.z / BLOCK_SIZE);

        dynamicTiles[col, height, row] = type;
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
        dynamicTiles = null;
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
