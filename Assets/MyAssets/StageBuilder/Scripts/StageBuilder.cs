using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StageBuilder : MonoBehaviour
{
    // デバッグ用
    public string csvFileName = "Stages/Stage1";
    public GameObject startPrefab;
    public GameObject blockPrefab;
    public GameObject goalPrefab;
    public GameObject nonePrefab;
    public GameObject playerPrefab;
    public float blockSize = 2.0f;
    public float heightOffset = 2.0f;

    public char[,,] gridData;

    public static StageBuilder Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        LoadStage(csvFileName);
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

        for (int height = 0; height < heightCount; height++)
        {
            string[] layer = layers[height];

            for (int row = 0; row < rowCount; row++)
            {
                string[] cells = layer[row].Split(',');

                for (int col = 0; col < colCount; col++)
                {
                    Vector3 position = new Vector3(col * blockSize, height * heightOffset, (rowCount - 1 - row) * blockSize);
                    char cellType = cells[col][0]; // 文字列から1文字を取得
                    SpawnBlock(cellType, position);

                    // ★ Z 軸 (row) の計算を修正 (反転させる)
                    int correctedRow = rowCount - 1 - row;

                    gridData[col, height, correctedRow] = cellType;
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

        // デバッグでここにしている
        StartCoroutine(TurnManager.Instance.TurnLoop());
    }

    void SpawnBlock(char cellType, Vector3 position)
    {
        GameObject prefab = null;
        switch (cellType)
        {
            case 'S':
                prefab = startPrefab;
                break;
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
        int newCol = Mathf.RoundToInt(player.transform.position.x / blockSize);
        int newHeight = Mathf.RoundToInt(player.transform.position.y / heightOffset);
        int newRow = Mathf.RoundToInt(player.transform.position.z / blockSize);

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
                    // ブロック（B）、ゴール（G）、スタート（S）を保持し、それ以外をリセット
                    if (gridData[c, h, r] != 'B' && gridData[c, h, r] != 'G' && gridData[c, h, r] != 'S' && gridData[c, h, r] != 'P')
                    {
                        gridData[c, h, r] = 'N'; // 空白にリセット
                    }
                }
            }
        }
    }
}

