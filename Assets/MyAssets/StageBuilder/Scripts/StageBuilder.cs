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
        for (int height = 0; height < layers.Count; height++)
        {
            string[] layer = layers[height];
            int rowCount = layer.Length;

            for (int row = 0; row < rowCount; row++)
            {
                string[] cells = layer[row].Split(',');
                int colCount = cells.Length;

                for (int col = 0; col < colCount; col++)
                {
                    Vector3 position = new Vector3(col * blockSize, height * heightOffset, (rowCount - 1 - row) * blockSize);
                    SpawnBlock(cells[col], position);
                }
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