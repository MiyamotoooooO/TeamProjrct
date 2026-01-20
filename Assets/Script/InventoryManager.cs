using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("現在持っているアイテム")]
    public List<string> currentItems = new List<string>();

    [Header("参照")]
    public SaveManager saveManager; // セーブマネージャーへの参照

    [HeaderAttribute("アイテム名とプレハブの対応表")]
    public List<ItemPrefabPair> itemPrefabs = new List<ItemPrefabPair>();

    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public Sprite icon;
    }

    [Header("アイテムデータ一覧")]
    public List<ItemData> itemDataList = new List<ItemData>();

    public Sprite GetItemIcon(string itemName)
    {
        foreach (var data in itemDataList)
        {
            if (data.itemName == itemName)
                return data.icon;
        }
        return null;
    }

    [System.Serializable]
    public class ItemPrefabPair
    {
        public string itemName;
        public GameObject prefab;
    }

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

    // アイテムを削除する処理
    public void RemoveItem(string itemName)
    {
        if (currentItems.Contains(itemName))
        {
            currentItems.Remove(itemName);
            Debug.Log(itemName + "をインベントリから削除しました");
        }
    }

    // アイテムのドロップ処理
    public GameObject DropItem(string itemName, Vector3 position)
    {
        // インベントリに無ければ何もしない
        if (!currentItems.Contains(itemName))
            return null;

        // インベントリから削除
        currentItems.Remove(itemName);

        // 対応するプレハブを探す
        foreach (var pair in itemPrefabs)
        {
            if (pair.itemName == itemName)
            {
                // プレハブを生成して返す
                return Instantiate(pair.prefab, position, Quaternion.identity);
            }
        }

        Debug.LogWarning("対応するプレハブが見つかりません：" + itemName);
        return null;
    }

    // アイテム名からプレハブのレイヤーを取得する
    public int GetItemLayer(string itemName)
    {
        foreach (var pair in itemPrefabs)
        {
            if (pair.itemName == itemName)
            {
                return pair.prefab.layer;
            }
        }

        Debug.LogWarning("レイヤーが取得できませんでした：" + itemName);
        return -1;
    }
}