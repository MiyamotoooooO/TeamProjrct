using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("現在持っているアイテム")]
    public List<string> currentItems = new List<string>();

    [Header("辞書（タグ検索用キャッシュ）")]
    public Dictionary<string, string> itemTagDatabase = new Dictionary<string, string>();

    [Header("現在装備中のメインスロット番号")]
    public int equippedIndex = 0;

    [Header("参照")]
    public SaveManager saveManager;

    [System.Serializable]
    public class ItemPrefabPair
    {
        public string itemName;
        public GameObject prefab;
    }

    [Header("★重要：ここにアイテム名とプレハブを登録してください")]
    public List<ItemPrefabPair> itemPrefabs = new List<ItemPrefabPair>();

    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public Sprite icon;
    }

    [Header("アイテムデータ一覧（アイコン用）")]
    public List<ItemData> itemDataList = new List<ItemData>();

    // 初期化：シーン開始時に辞書を再構築する試み
    private void Start()
    {
        // もしすでにアイテムを持っているなら、念のためタグ情報を復元しておく
        foreach (var item in currentItems)
        {
            GetItemTag(item); // 呼ぶだけで辞書に登録される
        }
    }

    public Sprite GetItemIcon(string itemName)
    {
        foreach (var data in itemDataList)
        {
            // 部分一致検索でヒットしやすくする
            if (itemName.Contains(data.itemName) || data.itemName.Contains(itemName))
                return data.icon;
        }
        return null;
    }

    // アイテムを拾う処理
    public void PickUpItem(GameObject itemObj)
    {
        string cleanName = itemObj.name.Replace("(Clone)", "").Trim();
        string tag = itemObj.tag;

        currentItems.Add(cleanName);

        // 辞書に追加
        if (!itemTagDatabase.ContainsKey(cleanName))
        {
            itemTagDatabase.Add(cleanName, tag);
        }

        Destroy(itemObj);

        Debug.Log($"{cleanName} をインベントリに追加しました（タグ: {tag}）");
    }

    public bool HasItem(string itemName)
    {
        return currentItems.Contains(itemName);
    }

    public List<string> GetItemDataForSave()
    {
        return currentItems;
    }

    public void LoadItemData(List<string> loadedItems)
    {
        currentItems = loadedItems;
        ReflectInventoryToScene();

        // ★重要：ロードした直後もタグ情報を復元する
        itemTagDatabase.Clear();
        foreach (var item in currentItems)
        {
            GetItemTag(item);
        }
    }

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

    public void RemoveItem(string itemName)
    {
        if (currentItems.Contains(itemName))
        {
            currentItems.Remove(itemName);
            Debug.Log(itemName + "をインベントリから削除しました");
        }
    }

    public GameObject DropItem(string itemName, Vector3 position)
    {
        if (!currentItems.Contains(itemName))
            return null;

        currentItems.Remove(itemName);

        foreach (var pair in itemPrefabs)
        {
            if (pair.itemName == itemName || itemName.Contains(pair.itemName))
            {
                return Instantiate(pair.prefab, position, Quaternion.identity);
            }
        }

        Debug.LogWarning("対応するプレハブが見つかりません：" + itemName);
        return null;
    }

    // ★★★ 一番重要な修正箇所 ★★★
    // アイテム名からタグを取得する（リスポーン対応版）
    public string GetItemTag(string itemName)
    {
        // 1. まず辞書（キャッシュ）を探す
        if (itemTagDatabase.ContainsKey(itemName))
        {
            return itemTagDatabase[itemName];
        }

        // 2. 辞書にない場合（リスポーン直後など）、登録リストから探す！
        // ※これが「リスポーン後にモデルが出ない」を直す特効薬です
        foreach (var pair in itemPrefabs)
        {
            // 名前が一致するかチェック（(Clone)対策で部分一致も考慮）
            if (pair.itemName == itemName || itemName.Contains(pair.itemName))
            {
                // プレハブについているタグを取得
                string tagFromPrefab = pair.prefab.tag;

                // 次回から早く見つかるように辞書に登録しておく
                itemTagDatabase.Add(itemName, tagFromPrefab);

                return tagFromPrefab;
            }
        }

        // 3. それでも見つからない場合
        Debug.LogWarning($"アイテム '{itemName}' のタグが見つかりません。InspectorのItem Prefabsリストに登録されていますか？");
        return "Untagged";
    }

    public string GetEquippedItem()
    {
        if (currentItems.Count > 0 && equippedIndex < currentItems.Count)
        {
            return currentItems[equippedIndex];
        }
        return "";
    }

    public void SwapItems(int slotA, int slotB)
    {
        if (slotA < currentItems.Count && slotB < currentItems.Count)
        {
            string temp = currentItems[slotA];
            currentItems[slotA] = currentItems[slotB];
            currentItems[slotB] = temp;
        }
    }
}