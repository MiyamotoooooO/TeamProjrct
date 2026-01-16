using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("現在持っているアイテム")]
    public List<string> currentItems = new List<string>();

    [Header("参照")]
    public SaveManager saveManager; // セーブマネージャーへの参照

    // アイテムを拾う処理
    public void PickUpItem(GameObject itemObj)
    {
        string itemName = itemObj.name;

        // まだ持っていなければリストに追加
        if (!currentItems.Contains(itemName))
        {
            currentItems.Add(itemName);
            Debug.Log(itemName + " をインベントリに追加しました");
        }

        // 見た目を消す
        itemObj.SetActive(false);
    }

    // アイテムを持っているか確認する処理（鍵などで使う）
    public bool HasItem(string itemName)
    {
        return currentItems.Contains(itemName);
    }

    // SaveManagerから呼ばれる：セーブするデータを渡す
    public List<string> GetItemDataForSave()
    {
        return currentItems;
    }

    // SaveManagerから呼ばれる：ロードしたデータを反映する
    public void LoadItemData(List<string> loadedItems)
    {
        currentItems = loadedItems;

        // シーン上の全アイテムを確認し、すでに持っているものは消す
        ReflectInventoryToScene();
    }

    // 持っているアイテムをシーン上で非表示にする処理
    // （ロードした時に、すでに取ったアイテムが復活しないようにする）
    private void ReflectInventoryToScene()
    {

        foreach (string itemName in currentItems)
        {
            GameObject obj = GameObject.Find(itemName);
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}