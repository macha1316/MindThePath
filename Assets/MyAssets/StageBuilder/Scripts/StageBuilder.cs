using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

    // UIPrefab
    [SerializeField] string[] textAssets;

    // Stage情報をロード & UIをStage情報に合わせて出す
    public void CreateStage(int stageNumber)
    {
        LoadStage(textAssets[stageNumber]);
        StageSelectUI.Instance.SelectStageUI(stageNumber);
    }

    private char[,,] gridData;
    private char[,,] dynamicTiles;

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

        for (int height = 0; height < heightCount; height++)
        {
            string[] layer = layers[height];

            for (int row = 0; row < rowCount; row++)
            {
                string[] cells = layer[row].Split(',');

                for (int col = 0; col < colCount; col++)
                {
                    Vector3 position = new Vector3(col * BLOCK_SIZE, height * HEIGHT_OFFSET, (rowCount - 1 - row) * BLOCK_SIZE);
                    char cellType = cells[col][0]; // 文字列から1文字を取得
                    SpawnBlock(cellType, position);

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

    void SpawnBlock(char cellType, Vector3 position)
    {
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
            GameObject obj = Instantiate(prefab, position, Quaternion.identity);
            if (cellType == 'P')
            {
                obj.AddComponent<Player>();
            }
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

        gridData[col, height, row] = 'N';
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
}

