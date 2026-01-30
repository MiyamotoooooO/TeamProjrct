using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryDisplay : MonoBehaviour
{
    public InventoryManager inventoryManager;
    public InventorySlot[] slots;

    [System.Serializable]
    public class ItemIconData
    {
        public string itemName;
        public Sprite icon;
    }
    public List<ItemIconData> itemIcons;

    void Start()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();

        // ボタンのクリックイベントを登録
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;

            // 安全策：Buttonコンポーネントがあるかチェックして登録
            InventorySlot slotScript = slots[i];
            if (slotScript != null && slotScript.button != null)
            {
                slotScript.button.onClick.RemoveAllListeners();
                slotScript.button.onClick.AddListener(() => OnSlotClicked(index));
            }
        }

        // ★追加：ゲーム開始時に、強制的に「0番（左上）」を選択状態にする
        if (inventoryManager != null)
        {
            inventoryManager.equippedIndex = 0;
        }

        // 画面を更新して、Slot1をメイン画像に切り替える
        UpdateUI();
    }

    void Update()
    {
        // 常に最新状態を表示
        UpdateUI();
    }

    // スロットがクリックされた時の処理
    void OnSlotClicked(int index)
    {
        if (inventoryManager != null)
        {
            // クリックされた場所を「装備中（メイン）」に変更する
            inventoryManager.equippedIndex = index;

            // ログで確認
            Debug.Log($"スロット {index} がメインに切り替わりました");
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (inventoryManager == null) return;

        List<string> currentItems = inventoryManager.currentItems;
        int equippedIndex = inventoryManager.equippedIndex;

        for (int i = 0; i < slots.Length; i++)
        {
            // 1. アイテムの絵を更新
            if (i < currentItems.Count)
            {
                slots[i].SetItem(GetIconFor(currentItems[i]));
            }
            else
            {
                slots[i].ClearSlot();
            }

            // 2. メイン判定（画像の切り替え）
            // 「今の番号(i)」が「装備番号(equippedIndex)」と同じならメイン画像にする
            bool isMain = (i == equippedIndex);

            // さっきInventorySlotに書いた関数を呼び出す
            slots[i].SetSelected(isMain);
        }
    }

    Sprite GetIconFor(string name)
    {
        foreach (var data in itemIcons)
        {
            if (name.Contains(data.itemName)) return data.icon;
        }
        return null;
    }
}