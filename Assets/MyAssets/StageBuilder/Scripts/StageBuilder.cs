using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class StageBuilder : MonoBehaviour
{
    // 定数
    public const float BLOCK_SIZE = 2.0f;
    public const float HEIGHT_OFFSET = 2.0f;

    // デバッグ用
    [SerializeField] string csvFileName = "Stages/Stage1";
    [SerializeField] GameObject blockPrefab;
    [SerializeField] GameObject goalPrefab;
    [SerializeField] GameObject nonePrefab;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject upPrefab;
    [SerializeField] GameObject downPrefab;
    [SerializeField] GameObject rightPrefab;
    [SerializeField] GameObject leftPrefab;
    private char[,,] gridData;
    private char[,,] dynamicTiles;

    // UIPrefab
    [SerializeField] string[] textAssets;
    public GameObject stageRoot; // ステージ親

    // Stage情報をロード & UIをStage情報に合わせて出す
    public void CreateStage(int stageNumber)
    {
        GameManager.Instance.SetStageNumber(stageNumber);
        StageSelectUI.Instance.SelectStageUI(stageNumber);
        GameManager.Instance.SetGameStop();
        gridData = new char[0, 0, 0];
        dynamicTiles = new char[0, 0, 0];
        LoadStage(textAssets[stageNumber]);
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

        // gridData を初期化
        gridData = new char[colCount, heightCount, rowCount];
        dynamicTiles = new char[colCount, heightCount, rowCount];

        int delayCounter = 0;

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

        // gridData の内容をログ出力
        Debug.Log("gridData 内容:");
        for (int h = 0; h < heightCount; h++)
        {
            for (int r = 0; r < rowCount; r++)
            {
                string rowData = "";
                for (int c = 0; c < colCount; c++)
                {
                    rowData += gridData[c, h, r] + " ";
                }
                // Debug.Log($"高さ {h}, 行 {r}: {rowData}");
            }
        }
    }

    IEnumerator SpawnBlockWithDelay(string cellTypeString, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnBlock(cellTypeString, position);
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
            case 'P':
                prefab = playerPrefab;
                break;
            case 'U':
                prefab = upPrefab;
                break;
            case 'D':
                prefab = downPrefab;
                break;
            case 'R':
                prefab = rightPrefab;
                break;
            case 'L':
                prefab = leftPrefab;
                break;
        }
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, position, Quaternion.identity, stageRoot != null ? stageRoot.transform : null);
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            if (cellType == 'P')
            {
                Player newP = obj.AddComponent<Player>();
                GameManager.Instance.GetPlayer(newP);
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
        }
    }

    public void UpdatePlayerPosition(Player player)
    {
        // 現在のプレイヤー位置を取得（ワールド座標 → グリッド座標に変換）
        int newCol = Mathf.RoundToInt(player.transform.position.x / BLOCK_SIZE);
        int newHeight = Mathf.RoundToInt(player.transform.position.y / HEIGHT_OFFSET);
        int newRow = Mathf.RoundToInt(player.transform.position.z / BLOCK_SIZE);

        // 以前のプレイヤー位置を削除
        for (int h = 0; h < gridData.GetLength(1); h++)
        {
            for (int r = 0; r < gridData.GetLength(2); r++)
            {
                for (int c = 0; c < gridData.GetLength(0); c++)
                {
                    if (gridData[c, h, r] == 'P')
                    {
                        gridData[c, h, r] = 'N'; // 以前のプレイヤー位置を空白に
                    }
                }
            }
        }

        // 新しい位置にプレイヤーを設定
        gridData[newCol, newHeight, newRow] = 'P';
    }

    public void ResetGridData()
    {
        for (int h = 0; h < gridData.GetLength(1); h++)
        {
            for (int r = 0; r < gridData.GetLength(2); r++)
            {
                for (int c = 0; c < gridData.GetLength(0); c++)
                {
                    // ブロック（B）、ゴール（G）、プレイヤー（P）を保持し、それ以外をリセット
                    if (gridData[c, h, r] != 'B' && gridData[c, h, r] != 'G' && gridData[c, h, r] != 'P')
                    {
                        gridData[c, h, r] = 'N';
                    }
                    // Dynamicの方のリセットいるかも
                }
            }
        }
    }

    // getメソッド
    public char[,,] GetGridData() => gridData;
    public char[,,] GetDynamicGridData() => dynamicTiles;

    // setメソッド
    public void UpdateGridAtPosition(Vector3 worldPosition, char type)
    {
        int col = Mathf.RoundToInt(worldPosition.x / BLOCK_SIZE);
        int height = Mathf.RoundToInt(worldPosition.y / HEIGHT_OFFSET);
        int row = Mathf.RoundToInt(worldPosition.z / BLOCK_SIZE);

        gridData[col, height, row] = type;
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

    public void BuildNextStage()
    {
        if (stageRoot != null)
        {
            foreach (Transform child in stageRoot.transform)
            {
                Destroy(child.gameObject);
            }
        }
        CreateStage(GameManager.Instance.SetStageNumber(1));
    }
}
