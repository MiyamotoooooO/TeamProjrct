using System.Collections.Generic;
using UnityEngine;
using static InventoryManager;

[System.Serializable]
public class SaveData
{
    [Header("保持しているアイテムリスト")]
    public List<string> collectedItems = new List<string>();

    [Header("Playerの位置")]
    public Vector3 playerPosition;

    [Header("Playerの向き")]
    public Quaternion playerRotation;

    [Header("どのシーンにいたか")]
    public string sceneName;

    [Header("終わったイベントのIDリスト")]
    public List<string> completedEventIDs = new List<string>();
}