using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    [Header("現在の時間（0.0 〜 24.0）")]
    public float currentHour;

    [Header("保持しているアイテムリスト")]
    public List<string> collectedItems = new List<string>();
}
