using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleRotateManager : MonoBehaviour
{
    [Header("回転オブジェクト数")]
    public int objectCount = 3;

    private int[] currentDirections;
    private int[] correctDirections;

    [Header("正解時に出す鍵")]
    public GameObject keyPrefab;
    public Transform spawnPoint;

    private bool keySpawned = false;

    private void Start()
    {
        currentDirections = new int[objectCount];
        correctDirections = new int[objectCount];
    }

    public void SetCorrectDirection(int id, int dir)
    {
        correctDirections[id] = dir;
    }

    public void UpdateDirection(int id, int dir)
    {
        currentDirections[id] = dir;

        // すでに鍵を出している
        if (keySpawned)
            return;

        // 全部一致しているかチェック
        for (int i = 0; i < objectCount; i++)
        {
            if (currentDirections[i] != correctDirections[i])
                return;
        }

        // 全問正解
        Instantiate(keyPrefab, spawnPoint.position, Quaternion.identity);
        keySpawned = true;
    }
}
