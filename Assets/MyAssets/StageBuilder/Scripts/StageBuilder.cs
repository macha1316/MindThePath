using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StageBuilder : MonoBehaviour
{
    public string csvFileName = "Stages/Stage1"; // Resourcesフォルダ内のパス
    public GameObject startPrefab;
    public GameObject blockPrefab;
    public GameObject goalPrefab;
    public GameObject nonePrefab;
    public GameObject playerPrefab;
    private float blockSize = 2.0f; // 1マスのサイズ
    private float heightOffset = 2.0f; // 高さの段差

    // gridData を追加（ステージ全体のマス情報を保持する）
    private string[,,] gridData;

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
        gridData = new string[colCount, heightCount, rowCount];

        for (int height = 0; height < heightCount; height++)
        {
            string[] layer = layers[height];

            for (int row = 0; row < rowCount; row++)
            {
                string[] cells = layer[row].Split(',');

                for (int col = 0; col < colCount; col++)
                {
                    Vector3 position = new Vector3(col * blockSize, height * heightOffset, (rowCount - 1 - row) * blockSize);
                    SpawnBlock(cells[col], position);

                    gridData[col, height, row] = cells[col];

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
                Debug.Log($"高さ {h}, 行 {r}: {rowData}");
            }
        }
    }

    void SpawnBlock(string cellType, Vector3 position)
    {
        GameObject prefab = null;
        switch (cellType)
        {
            case "S":
                prefab = startPrefab;
                break;
            case "B":
                prefab = blockPrefab;
                break;
            case "G":
                prefab = goalPrefab;
                break;
            case "N":
                prefab = nonePrefab;
                break;
            case "P":
                prefab = playerPrefab;
                break;
        }

        if (prefab != null)
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
    }
}