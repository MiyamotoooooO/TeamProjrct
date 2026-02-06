using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("インベントリ設定")]
    public int maxSlots = 12;
    public int backpackColumns = 3;

    [Header("アイテムデータ")]
    public List<string> currentItems = new List<string>();
    public Dictionary<string, string> itemTagDatabase = new Dictionary<string, string>();
    public int equippedIndex = 0;

    [Header("参照")]
    public SaveManager saveManager;
    public PlayerController playerController;

    [System.Serializable]
    public class ItemPrefabPair { public string itemName; public GameObject prefab; }
    public List<ItemPrefabPair> itemPrefabs = new List<ItemPrefabPair>();

    [System.Serializable]
    public class ItemData { public string itemName; public Sprite icon; }
    public List<ItemData> itemDataList = new List<ItemData>();

    // -----------------------------------------------------------
    // ★ここが変わりました！
    // -----------------------------------------------------------
    [Header("UI参照：インベントリ画面（メニュー内）")]
    [Tooltip("InventoryUI内の全スロット（Main1,2,3 -> Backpack1~9 の順）")]
    public InventorySlot[] allSlots;

    [Header("UI参照：ゲーム画面（HUD）")]
    [Tooltip("ゲーム画面に常時表示するホットバーのアイコン画像（上から1,2,3の順）")]
    public Image[] hudHotbarIcons;

    [Tooltip("ゲーム画面に常時表示するホットバーの選択枠（上から1,2,3の順）")]
    public Image[] hudHotbarFrames;

    [Header("UI参照：カーソル・演出")]
    public RectTransform cursorRect;
    public GameObject swapPromptPanel;

    [Header("HUD透明度設定")]
    [Range(0, 255)] public float selectedAlpha = 255f; // 選択中はくっきり
    [Range(0, 255)] public float unselectedAlpha = 100f; // 非選択は薄く

    // 内部変数
    private int currentCursorIndex = 3;
    private bool isSwapMode = false;

    private void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        while (currentItems.Count < maxSlots) currentItems.Add("");

        InitializeInventorySlots();

        UpdateInventoryUI();       // インベントリ画面の更新
        UpdateHUDSlotSelection();  // ゲーム画面の枠色更新
        UpdateCursorPosition();

        if (swapPromptPanel != null) swapPromptPanel.SetActive(false);
    }

    private void Update()
    {
        if (playerController != null && !playerController.isInventoryOpen)
        {
            // ★インベントリが閉じていても、1,2,3キーで武器切替はできるようにするならココで処理
            // （もしインベントリを開いている時だけ切り替えたいなら、このブロックの下に移動してください）
            HandleWeaponSwitchInput();
            return;
        }

        if (isSwapMode)
        {
            HandleSwapInput();
            return;
        }

        // --- インベントリ操作中 ---

        HandleCursorMovement();
        HandleWeaponSwitchInput(); // インベントリを開いている時も切り替え可能

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentCursorIndex < currentItems.Count && !string.IsNullOrEmpty(currentItems[currentCursorIndex]))
            {
                isSwapMode = true;
                if (swapPromptPanel != null) swapPromptPanel.SetActive(true);
                if (playerController != null) playerController.SetBlurState(true);
            }
        }
    }

    // 武器切り替え入力（共通化）
    void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeSelectedSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeSelectedSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeSelectedSlot(2);
    }

    // --- カーソル移動 ---
    void HandleCursorMovement()
    {
        int prevIndex = currentCursorIndex;

        if (Input.GetKeyDown(KeyCode.W)) { if (currentCursorIndex >= 6) currentCursorIndex -= backpackColumns; }
        if (Input.GetKeyDown(KeyCode.S)) { if (currentCursorIndex <= 8) currentCursorIndex += backpackColumns; }
        if (Input.GetKeyDown(KeyCode.A)) { if (currentCursorIndex % backpackColumns != 0) currentCursorIndex -= 1; }
        if (Input.GetKeyDown(KeyCode.D)) { if ((currentCursorIndex + 1) % backpackColumns != 0) currentCursorIndex += 1; }

        if (prevIndex != currentCursorIndex) UpdateCursorPosition();
    }

    // --- 入れ替え選択 ---
    void HandleSwapInput()
    {
        int targetSlot = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) targetSlot = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) targetSlot = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) targetSlot = 2;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) { EndSwapMode(); return; }

        if (targetSlot != -1)
        {
            SwapItems(currentCursorIndex, targetSlot);
            EndSwapMode();
        }
    }

    void EndSwapMode()
    {
        isSwapMode = false;
        if (swapPromptPanel != null) swapPromptPanel.SetActive(false);
    }

    // --- UI更新関連 ---

    void UpdateCursorPosition()
    {
        if (cursorRect == null || allSlots == null || currentCursorIndex >= allSlots.Length) return;
        cursorRect.position = allSlots[currentCursorIndex].transform.position;
    }

    // ★ゲーム画面（HUD）の選択枠の色だけを変える
    void UpdateHUDSlotSelection()
    {
        if (hudHotbarFrames == null) return;

        for (int i = 0; i < hudHotbarFrames.Length; i++)
        {
            if (hudHotbarFrames[i] == null) continue;

            Color c = hudHotbarFrames[i].color;
            // 装備中の番号なら明るく(selectedAlpha)、それ以外は暗く(unselectedAlpha)
            float alpha = (i == equippedIndex) ? selectedAlpha : unselectedAlpha;
            c.a = alpha / 255f;
            hudHotbarFrames[i].color = c;
        }
    }

    public void SwapItems(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= currentItems.Count || indexB < 0 || indexB >= currentItems.Count) return;

        string temp = currentItems[indexA];
        currentItems[indexA] = currentItems[indexB];
        currentItems[indexB] = temp;

        UpdateInventoryUI();
        if (playerController != null) playerController.UpdateItemModel();
    }

    private void InitializeInventorySlots()
    {
        if (allSlots == null) return;
        for (int i = 0; i < allSlots.Length; i++)
        {
            allSlots[i].slotIndex = i;
            allSlots[i].manager = this;
        }
    }

    // ★インベントリ画面とHUD画面の両方を更新する
    public void UpdateInventoryUI()
    {
        // 1. インベントリ画面（メニュー）の更新
        if (allSlots != null)
        {
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (i < currentItems.Count && !string.IsNullOrEmpty(currentItems[i]))
                    allSlots[i].SetItem(GetItemIcon(currentItems[i]));
                else
                    allSlots[i].ClearSlot();
            }
        }

        // 2. ゲーム画面（HUD）の更新（MainSlot 0,1,2 のみ同期）
        if (hudHotbarIcons != null)
        {
            for (int i = 0; i < hudHotbarIcons.Length; i++) // 0, 1, 2
            {
                // リストの範囲外ならスキップ
                if (i >= currentItems.Count) break;

                // アイテムがあるかチェック
                if (!string.IsNullOrEmpty(currentItems[i]))
                {
                    hudHotbarIcons[i].sprite = GetItemIcon(currentItems[i]);
                    hudHotbarIcons[i].enabled = true;
                }
                else
                {
                    hudHotbarIcons[i].sprite = null;
                    hudHotbarIcons[i].enabled = false;
                }
            }
        }
    }

    // 装備変更（HUDの色も更新）
    public void ChangeSelectedSlot(int slotIndex)
    {
        equippedIndex = slotIndex;
        if (playerController != null) playerController.UpdateItemModel();
        UpdateHUDSlotSelection(); // ★HUDの色を変える
    }

    // --- その他機能（変更なし） ---
    public void PickUpItem(GameObject itemObj)
    {
        string cleanName = itemObj.name.Replace("(Clone)", "").Trim();
        string tag = itemObj.tag;
        int emptyIndex = currentItems.IndexOf("");
        if (emptyIndex != -1)
        {
            currentItems[emptyIndex] = cleanName;
            if (!itemTagDatabase.ContainsKey(cleanName)) itemTagDatabase.Add(cleanName, tag);
            Destroy(itemObj);
            UpdateInventoryUI();
            if (emptyIndex == equippedIndex && playerController != null) playerController.UpdateItemModel();
        }
    }

    public GameObject DropItem(string itemName, Vector3 position)
    {
        int index = currentItems.IndexOf(itemName);
        if (index == -1) return null;
        currentItems[index] = "";
        UpdateInventoryUI();
        foreach (var pair in itemPrefabs)
        {
            if (pair.itemName == itemName || itemName.Contains(pair.itemName))
                return Instantiate(pair.prefab, position, Quaternion.identity);
        }
        return null;
    }

    public void AddItem(string itemName)
    {
        // アイテムリストの空きを探す
        int emptyIndex = currentItems.IndexOf("");

        if (emptyIndex != -1)
        {
            // 名前を登録
            currentItems[emptyIndex] = itemName;

            // タグデータベースに登録（タグが不明な場合はUntaggedにしておく）
            if (!itemTagDatabase.ContainsKey(itemName))
            {
                itemTagDatabase.Add(itemName, "Untagged");
            }

            // UI更新
            UpdateInventoryUI();
            UpdateHUDSlotSelection();

            // もし装備スロットに入ったならモデル更新
            if (emptyIndex == equippedIndex && playerController != null)
            {
                playerController.UpdateItemModel();
            }

            Debug.Log($"アイテム「{itemName}」を手に入れました！");
        }
        else
        {
            Debug.Log("インベントリがいっぱいです！");
        }
    }

    public Sprite GetItemIcon(string itemName) { foreach (var d in itemDataList) if (itemName.Contains(d.itemName)) return d.icon; return null; }
    public string GetItemTag(string itemName) { if (itemTagDatabase.ContainsKey(itemName)) return itemTagDatabase[itemName]; return "Untagged"; }
    public bool HasItem(string itemName) { return currentItems.Contains(itemName); }
    public string GetEquippedItem() { if (currentItems != null && equippedIndex >= 0 && equippedIndex < currentItems.Count) return currentItems[equippedIndex]; return ""; }
    public List<string> GetItemDataForSave() { return currentItems; }
    public void LoadItemData(List<string> loadedItems)
    {
        currentItems = loadedItems;
        while (currentItems.Count < maxSlots) currentItems.Add("");
        itemTagDatabase.Clear();
        foreach (var item in currentItems) if (!string.IsNullOrEmpty(item)) GetItemTag(item);
        ReflectInventoryToScene();
        UpdateInventoryUI();
        UpdateHUDSlotSelection();
    }
    private void ReflectInventoryToScene() { foreach (string itemName in currentItems) { if (string.IsNullOrEmpty(itemName)) continue; GameObject obj = GameObject.Find(itemName); if (obj != null) obj.SetActive(false); } }
}



/*using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("インベントリ設定")]
    public int maxSlots = 12;
    public int backpackColumns = 3;

    [Header("アイテムデータ")]
    public List<string> currentItems = new List<string>();
    public Dictionary<string, string> itemTagDatabase = new Dictionary<string, string>();
    public int equippedIndex = 0;

    [Header("参照")]
    public SaveManager saveManager;
    public PlayerController playerController;

    [System.Serializable]
    public class ItemPrefabPair { public string itemName; public GameObject prefab; }
    public List<ItemPrefabPair> itemPrefabs = new List<ItemPrefabPair>();

    [System.Serializable]
    public class ItemData { public string itemName; public Sprite icon; }
    public List<ItemData> itemDataList = new List<ItemData>();

    // -----------------------------------------------------------
    // ★ここが変わりました！
    // -----------------------------------------------------------
    [Header("UI参照：インベントリ画面（メニュー内）")]
    [Tooltip("InventoryUI内の全スロット（Main1,2,3 -> Backpack1~9 の順）")]
    public InventorySlot[] allSlots;

    [Header("UI参照：ゲーム画面（HUD）")]
    [Tooltip("ゲーム画面に常時表示するホットバーのアイコン画像（上から1,2,3の順）")]
    public Image[] hudHotbarIcons;

    [Tooltip("ゲーム画面に常時表示するホットバーの選択枠（上から1,2,3の順）")]
    public Image[] hudHotbarFrames;

    [Header("UI参照：カーソル・演出")]
    public RectTransform cursorRect;
    public GameObject swapPromptPanel;

    [Header("HUD透明度設定")]
    [Range(0, 255)] public float selectedAlpha = 255f; // 選択中はくっきり
    [Range(0, 255)] public float unselectedAlpha = 100f; // 非選択は薄く

    // 内部変数
    private int currentCursorIndex = 3;
    private bool isSwapMode = false;

    private void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        while (currentItems.Count < maxSlots) currentItems.Add("");

        InitializeInventorySlots();

        UpdateInventoryUI();       // インベントリ画面の更新
        UpdateHUDSlotSelection();  // ゲーム画面の枠色更新
        UpdateCursorPosition();

        if (swapPromptPanel != null) swapPromptPanel.SetActive(false);
    }

    private void Update()
    {
        if (playerController != null && !playerController.isInventoryOpen)
        {
            // ★インベントリが閉じていても、1,2,3キーで武器切替はできるようにするならココで処理
            // （もしインベントリを開いている時だけ切り替えたいなら、このブロックの下に移動してください）
            HandleWeaponSwitchInput();
            return;
        }

        if (isSwapMode)
        {
            HandleSwapInput();
            return;
        }

        // --- インベントリ操作中 ---

        HandleCursorMovement();
        HandleWeaponSwitchInput(); // インベントリを開いている時も切り替え可能

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentCursorIndex < currentItems.Count && !string.IsNullOrEmpty(currentItems[currentCursorIndex]))
            {
                isSwapMode = true;
                if (swapPromptPanel != null) swapPromptPanel.SetActive(true);
                if (playerController != null) playerController.SetBlurState(true);
            }
        }
    }

    // 武器切り替え入力（共通化）
    void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeSelectedSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeSelectedSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeSelectedSlot(2);
    }

    // --- カーソル移動 ---
    void HandleCursorMovement()
    {
        int prevIndex = currentCursorIndex;

        if (Input.GetKeyDown(KeyCode.W)) { if (currentCursorIndex >= 6) currentCursorIndex -= backpackColumns; }
        if (Input.GetKeyDown(KeyCode.S)) { if (currentCursorIndex <= 8) currentCursorIndex += backpackColumns; }
        if (Input.GetKeyDown(KeyCode.A)) { if (currentCursorIndex % backpackColumns != 0) currentCursorIndex -= 1; }
        if (Input.GetKeyDown(KeyCode.D)) { if ((currentCursorIndex + 1) % backpackColumns != 0) currentCursorIndex += 1; }

        if (prevIndex != currentCursorIndex) UpdateCursorPosition();
    }

    // --- 入れ替え選択 ---
    void HandleSwapInput()
    {
        int targetSlot = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) targetSlot = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) targetSlot = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) targetSlot = 2;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) { EndSwapMode(); return; }

        if (targetSlot != -1)
        {
            SwapItems(currentCursorIndex, targetSlot);
            EndSwapMode();
        }
    }

    void EndSwapMode()
    {
        isSwapMode = false;
        if (swapPromptPanel != null) swapPromptPanel.SetActive(false);
    }

    // --- UI更新関連 ---

    void UpdateCursorPosition()
    {
        if (cursorRect == null || allSlots == null || currentCursorIndex >= allSlots.Length) return;
        cursorRect.position = allSlots[currentCursorIndex].transform.position;
    }

    // ★ゲーム画面（HUD）の選択枠の色だけを変える
    void UpdateHUDSlotSelection()
    {
        if (hudHotbarFrames == null) return;

        for (int i = 0; i < hudHotbarFrames.Length; i++)
        {
            if (hudHotbarFrames[i] == null) continue;

            Color c = hudHotbarFrames[i].color;
            // 装備中の番号なら明るく(selectedAlpha)、それ以外は暗く(unselectedAlpha)
            float alpha = (i == equippedIndex) ? selectedAlpha : unselectedAlpha;
            c.a = alpha / 255f;
            hudHotbarFrames[i].color = c;
        }
    }

    public void SwapItems(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= currentItems.Count || indexB < 0 || indexB >= currentItems.Count) return;

        string temp = currentItems[indexA];
        currentItems[indexA] = currentItems[indexB];
        currentItems[indexB] = temp;

        UpdateInventoryUI();
        if (playerController != null) playerController.UpdateItemModel();
    }

    private void InitializeInventorySlots()
    {
        if (allSlots == null) return;
        for (int i = 0; i < allSlots.Length; i++)
        {
            allSlots[i].slotIndex = i;
            allSlots[i].manager = this;
        }
    }

    // ★インベントリ画面とHUD画面の両方を更新する
    public void UpdateInventoryUI()
    {
        // 1. インベントリ画面（メニュー）の更新
        if (allSlots != null)
        {
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (i < currentItems.Count && !string.IsNullOrEmpty(currentItems[i]))
                    allSlots[i].SetItem(GetItemIcon(currentItems[i]));
                else
                    allSlots[i].ClearSlot();
            }
        }

        // 2. ゲーム画面（HUD）の更新（MainSlot 0,1,2 のみ同期）
        if (hudHotbarIcons != null)
        {
            for (int i = 0; i < hudHotbarIcons.Length; i++) // 0, 1, 2
            {
                // リストの範囲外ならスキップ
                if (i >= currentItems.Count) break;

                // アイテムがあるかチェック
                if (!string.IsNullOrEmpty(currentItems[i]))
                {
                    hudHotbarIcons[i].sprite = GetItemIcon(currentItems[i]);
                    hudHotbarIcons[i].enabled = true;
                }
                else
                {
                    hudHotbarIcons[i].sprite = null;
                    hudHotbarIcons[i].enabled = false;
                }
            }
        }
    }

    // 装備変更（HUDの色も更新）
    public void ChangeSelectedSlot(int slotIndex)
    {
        equippedIndex = slotIndex;
        if (playerController != null) playerController.UpdateItemModel();
        UpdateHUDSlotSelection(); // ★HUDの色を変える
    }

    // --- その他機能（変更なし） ---
    public void PickUpItem(GameObject itemObj)
    {
        string cleanName = itemObj.name.Replace("(Clone)", "").Trim();
        string tag = itemObj.tag;
        int emptyIndex = currentItems.IndexOf("");
        if (emptyIndex != -1)
        {
            currentItems[emptyIndex] = cleanName;
            if (!itemTagDatabase.ContainsKey(cleanName)) itemTagDatabase.Add(cleanName, tag);
            Destroy(itemObj);
            UpdateInventoryUI();
            if (emptyIndex == equippedIndex && playerController != null) playerController.UpdateItemModel();
        }
    }

    public GameObject DropItem(string itemName, Vector3 position)
    {
        int index = currentItems.IndexOf(itemName);
        if (index == -1) return null;
        currentItems[index] = "";
        UpdateInventoryUI();
        foreach (var pair in itemPrefabs)
        {
            if (pair.itemName == itemName || itemName.Contains(pair.itemName))
                return Instantiate(pair.prefab, position, Quaternion.identity);
        }
        return null;
    }

    public Sprite GetItemIcon(string itemName) { foreach (var d in itemDataList) if (itemName.Contains(d.itemName)) return d.icon; return null; }
    public string GetItemTag(string itemName) { if (itemTagDatabase.ContainsKey(itemName)) return itemTagDatabase[itemName]; return "Untagged"; }
    public bool HasItem(string itemName) { return currentItems.Contains(itemName); }
    public string GetEquippedItem() { if (currentItems != null && equippedIndex >= 0 && equippedIndex < currentItems.Count) return currentItems[equippedIndex]; return ""; }
    public List<string> GetItemDataForSave() { return currentItems; }
    public void LoadItemData(List<string> loadedItems)
    {
        currentItems = loadedItems;
        while (currentItems.Count < maxSlots) currentItems.Add("");
        itemTagDatabase.Clear();
        foreach (var item in currentItems) if (!string.IsNullOrEmpty(item)) GetItemTag(item);
        ReflectInventoryToScene();
        UpdateInventoryUI();
        UpdateHUDSlotSelection();
    }
    private void ReflectInventoryToScene() { foreach (string itemName in currentItems) { if (string.IsNullOrEmpty(itemName)) continue; GameObject obj = GameObject.Find(itemName); if (obj != null) obj.SetActive(false); } }
}*/