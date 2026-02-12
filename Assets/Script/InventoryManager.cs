using System.Collections;
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
    public class ItemData
    {
        public string itemName;
        public Sprite icon;
        [Tooltip("説明欄に表示する画像")]
        public Sprite description;
    }
    public List<ItemData> itemDataList = new List<ItemData>();

    [Header("UI参照：インベントリ画面")]
    [Tooltip("★重要★ 0~2:ホットバー(上中下), 3~11:バックパック(左上〜右下) の順で登録してください")]
    public InventorySlot[] allSlots;

    [Header("UI参照：説明パネル")]
    [Tooltip("右側の説明欄にあるImageコンポーネントを入れてください")]
    public Image descriptionDisplayImage;

    [Header("UI参照：HUD")]
    public Image[] hudHotbarIcons;
    public Image[] hudHotbarFrames;

    [Header("UI参照：カーソル・演出")]
    public RectTransform cursorRect;
    public GameObject swapPromptPanel;

    [Header("UI参照：メッセージ")]
    public TextMeshProUGUI fullMessageText;
    public CanvasGroup fullMessageCanvasGroup;

    [Header("HUD透明度設定")]
    [Range(0, 255)] public float selectedAlpha = 255f;
    [Range(0, 255)] public float unselectedAlpha = 100f;

    // 内部変数
    private int currentCursorIndex = 0;
    private bool isSwapMode = false;
    private Coroutine messageCoroutine;

    private void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        while (currentItems.Count < maxSlots) currentItems.Add("");

        InitializeInventorySlots();

        UpdateInventoryUI();
        UpdateHUDSlotSelection();
        UpdateCursorPosition();
        UpdateDescriptionPanel();

        if (swapPromptPanel != null) swapPromptPanel.SetActive(false);
        if (fullMessageText != null) fullMessageText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerController != null && !playerController.isInventoryOpen)
        {
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
        HandleWeaponSwitchInput();

        // Spaceキー：決定 / 移動
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleSpaceKeyAction();
        }
    }

    void HandleCursorMovement()
    {
        int prevIndex = currentCursorIndex;

        // --- W (上) ---
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (currentCursorIndex == 1) currentCursorIndex = 0;
            else if (currentCursorIndex == 2) currentCursorIndex = 1;
            else if (currentCursorIndex >= 6) currentCursorIndex -= 3;
        }
        // --- S (下) ---
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (currentCursorIndex == 0) currentCursorIndex = 1;
            else if (currentCursorIndex == 1) currentCursorIndex = 2;
            else if (currentCursorIndex >= 3 && currentCursorIndex < 9) currentCursorIndex += 3;
        }
        // --- A (左) ---
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (currentCursorIndex >= 3)
            {
                if (currentCursorIndex == 3) currentCursorIndex = 0;
                else if (currentCursorIndex == 6) currentCursorIndex = 1;
                else if (currentCursorIndex == 9) currentCursorIndex = 2;
                else currentCursorIndex -= 1;
            }
        }
        // --- D (右) ---
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (currentCursorIndex == 0) currentCursorIndex = 3;
            else if (currentCursorIndex == 1) currentCursorIndex = 6;
            else if (currentCursorIndex == 2) currentCursorIndex = 9;
            else if (currentCursorIndex >= 3)
            {
                if (currentCursorIndex != 5 && currentCursorIndex != 8 && currentCursorIndex != 11)
                    currentCursorIndex += 1;
            }
        }

        // 変化があったらカーソル位置と説明パネルを更新
        if (prevIndex != currentCursorIndex)
        {
            UpdateCursorPosition();
            UpdateDescriptionPanel(); // ★追加：カーソル移動時に説明を更新
        }
    }

    // --- ★追加：説明パネルの更新処理 ---
    public void UpdateDescriptionPanel()
    {
        // UIが設定されていない場合は無視
        if (descriptionDisplayImage == null) return;

        // カーソル位置が範囲外なら非表示
        if (currentCursorIndex < 0 || currentCursorIndex >= currentItems.Count)
        {
            descriptionDisplayImage.enabled = false;
            return;
        }

        // 現在のカーソル位置にあるアイテム名を取得
        string itemName = currentItems[currentCursorIndex];

        // アイテムがない場合は非表示
        if (string.IsNullOrEmpty(itemName))
        {
            descriptionDisplayImage.enabled = false;
            return;
        }

        // アイテム名から説明画像を取得
        Sprite descSprite = GetItemDescription(itemName);

        // 画像があれば表示、なければ非表示
        if (descSprite != null)
        {
            descriptionDisplayImage.sprite = descSprite;
            descriptionDisplayImage.enabled = true;
        }
        else
        {
            descriptionDisplayImage.enabled = false;
        }
    }

    void HandleSpaceKeyAction()
    {
        if (currentCursorIndex >= currentItems.Count || string.IsNullOrEmpty(currentItems[currentCursorIndex])) return;

        if (currentCursorIndex < 3)
        {
            MoveItemToBackpack(currentCursorIndex);
        }
        else
        {
            isSwapMode = true;
            if (swapPromptPanel != null) swapPromptPanel.SetActive(true);
            if (playerController != null) playerController.SetBlurState(true);
        }
    }

    void MoveItemToBackpack(int fromIndex)
    {
        int emptySlot = -1;
        for (int i = 3; i < currentItems.Count; i++)
        {
            if (string.IsNullOrEmpty(currentItems[i]))
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot != -1)
        {
            currentItems[emptySlot] = currentItems[fromIndex];
            currentItems[fromIndex] = "";
            UpdateInventoryUI();
            if (playerController != null) playerController.UpdateItemModel();
        }
        else
        {
            ShowFullMessage();
        }
    }

    void ShowFullMessage()
    {
        if (fullMessageText == null) return;
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(FadeOutMessageRoutine());
    }

    IEnumerator FadeOutMessageRoutine()
    {
        fullMessageText.gameObject.SetActive(true);
        if (fullMessageCanvasGroup != null) fullMessageCanvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.0f);

        float duration = 1.0f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            if (fullMessageCanvasGroup != null)
                fullMessageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / duration);
            yield return null;
        }
        fullMessageText.gameObject.SetActive(false);
    }

    void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeSelectedSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeSelectedSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeSelectedSlot(2);
    }

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
        if (playerController != null) playerController.SetBlurState(false);
    }

    void UpdateCursorPosition()
    {
        if (cursorRect == null || allSlots == null || currentCursorIndex >= allSlots.Length) return;
        cursorRect.position = allSlots[currentCursorIndex].transform.position;
    }

    void UpdateHUDSlotSelection()
    {
        if (hudHotbarFrames == null) return;
        for (int i = 0; i < hudHotbarFrames.Length; i++)
        {
            if (hudHotbarFrames[i] == null) continue;
            Color c = hudHotbarFrames[i].color;
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

    public void UpdateInventoryUI()
    {
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

        UpdateDescriptionPanel(); // ★追加：アイテムの移動や削除があった時も説明を更新

        if (hudHotbarIcons != null)
        {
            for (int i = 0; i < hudHotbarIcons.Length; i++)
            {
                if (i >= currentItems.Count) break;
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

    public void ChangeSelectedSlot(int slotIndex)
    {
        equippedIndex = slotIndex;
        if (playerController != null) playerController.UpdateItemModel();
        UpdateHUDSlotSelection();
    }

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

    public void RemoveItem(string itemName)
    {
        if (currentItems.Contains(itemName))
        {
            currentItems.Remove(itemName);
        }
    }

    public void AddItem(string itemName)
    {
        int emptyIndex = currentItems.IndexOf("");
        if (emptyIndex != -1)
        {
            currentItems[emptyIndex] = itemName;
            if (!itemTagDatabase.ContainsKey(itemName)) itemTagDatabase.Add(itemName, "Untagged");
            UpdateInventoryUI();
            UpdateHUDSlotSelection();
            if (emptyIndex == equippedIndex && playerController != null) playerController.UpdateItemModel();
        }
        else
        {
            ShowFullMessage();
        }
    }

    public Sprite GetItemIcon(string itemName) { foreach (var d in itemDataList) if (itemName.Contains(d.itemName)) return d.icon; return null; }

    // ★追加：アイテム名から説明画像を取得する関数
    public Sprite GetItemDescription(string itemName)
    {
        foreach (var d in itemDataList)
        {
            if (itemName.Contains(d.itemName))
                return d.description;
        }
        return null;
    }

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