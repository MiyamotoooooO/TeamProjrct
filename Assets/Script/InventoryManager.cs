using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    // ... (設定変数はそのまま) ...
    [Header("インベントリ設定")]
    [Tooltip("アイテムの最大所持数（例: 21ならホットバー3 + 9×2ページ）")]
    public int maxSlots = 21;
    public int backpackColumns = 3;
    private int backpackPageSize = 9;

    [Header("デコイ設定")]
    [Tooltip("デコイのアイテム名（これと一致した時だけクールタイムが発生）")]
    public string decoyItemName = "SpiderDecoy";
    [Tooltip("クールダウン時間（秒）")]
    public float decoyCooldownTime = 20.0f;

    [Header("ページ設定")]
    public int currentPage = 0;
    public int maxPages = 2;

    [Header("アイテムデータ")]
    public List<string> currentItems = new List<string>();
    public Dictionary<string, string> itemTagDatabase = new Dictionary<string, string>();
    public int equippedIndex = 0;
    public static List<string> consumedItems = new List<string>();
    public class DroppedItemInfo
    {
        public string itemName;
        public Vector3 position;
    }
    public static List<DroppedItemInfo> droppedItemsList = new List<DroppedItemInfo>();

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

        // ==========================================
        // ★追加：アイテムアイコンのサイズ調整機能
        // ==========================================
        [Header("アイコン表示設定")]
        [Tooltip("HUDでのアイコンの大きさ (1, 1, 1 が標準)")]
        public Vector3 iconScale = Vector3.one;

        [Header("説明パネル設定")]
        [Tooltip("説明欄に表示する画像")]
        public Sprite description;

        [Tooltip("画像の表示位置 (X, Y)")]
        public Vector2 descriptionPosition = Vector2.zero;

        [Tooltip("画像の大きさ (1,1 が標準)")]
        public Vector3 descriptionScale = Vector3.one;
    }
    public List<ItemData> itemDataList = new List<ItemData>();

    [Header("UI参照：インベントリ画面")]
    [Tooltip("★重要★ 0~2:ホットバー, 3~11:バックパック表示用スロット")]
    public InventorySlot[] allSlots;

    [Header("UI参照：ページアニメーション用")]
    [Tooltip("バックパックの背景とスロットをまとめた親オブジェクトを指定してください")]
    public RectTransform backpackUIContainer;
    public float animationDuration = 0.3f;
    [Tooltip("アニメーションの動き方をグラフで設定します")]
    public AnimationCurve pageChangeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("UI参照：ページ切り替えボタン")]
    public Button nextPageButton;
    public Button prevPageButton;
    public TextMeshProUGUI pageNumberText;

    [Header("UI参照：説明パネル")]
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
    private bool isAnimatingPage = false;
    private Coroutine messageCoroutine;
    private Vector2 originalContainerPosition;

    [Header("デバッグ用")]
    public float currentDecoyCooldown = 0f;

    private void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        while (currentItems.Count < maxSlots) currentItems.Add("");

        InitializeInventorySlots();

        if (backpackUIContainer != null)
        {
            originalContainerPosition = backpackUIContainer.anchoredPosition;
        }

        UpdateInventoryUI();
        UpdateHUDSlotSelection();
        UpdateCursorPosition();
        UpdateDescriptionPanel();

        if (swapPromptPanel != null) swapPromptPanel.SetActive(false);
        if (fullMessageText != null) fullMessageText.gameObject.SetActive(false);

        itemTagDatabase["Key"] = "Key";
        itemTagDatabase["Crowbar"] = "Crowbar";
        itemTagDatabase["Flashlight"] = "Flashlight";
        itemTagDatabase["Lighter"] = "Lighter";
        itemTagDatabase["Item"] = "Item";
        itemTagDatabase["Spider"] = "Spider";
        itemTagDatabase["Detergent"] = "Detergent";
        itemTagDatabase["Dirtykey"] = "Dirtykey";
    }

    private void Update()
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();

            if (playerController != null)
            {
                playerController.UpdateItemModel();
            }

            if (playerController == null) return;
        }

        if (currentDecoyCooldown > 0)
        {
            currentDecoyCooldown -= Time.unscaledDeltaTime;
            if (currentDecoyCooldown < 0) currentDecoyCooldown = 0;
            UpdateCooldownUI();
        }

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

        if (isAnimatingPage) return;

        HandleCursorMovement();
        HandleWeaponSwitchInput();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleSpaceKeyAction();
        }
    }

    public int GetRealDataIndex(int uiSlotIndex)
    {
        if (uiSlotIndex < 3) return uiSlotIndex;
        else
        {
            int backpackIndex = uiSlotIndex - 3;
            return 3 + (currentPage * backpackPageSize) + backpackIndex;
        }
    }

    void UpdateCooldownUI()
    {
        if (allSlots == null) return;

        float fillValue = currentDecoyCooldown / decoyCooldownTime;
        int remainingSeconds = Mathf.CeilToInt(currentDecoyCooldown);
        string textValue = (remainingSeconds > 0) ? remainingSeconds.ToString() : "";

        for (int i = 0; i < allSlots.Length; i++)
        {
            int realIndex = GetRealDataIndex(allSlots[i].slotIndex);

            if (realIndex < currentItems.Count && currentItems[realIndex] == decoyItemName)
            {
                if (allSlots[i].cooldownImage != null)
                {
                    allSlots[i].cooldownImage.fillAmount = fillValue;
                }
                if (allSlots[i].cooldownText != null)
                {
                    allSlots[i].cooldownText.text = textValue;
                    allSlots[i].cooldownText.enabled = (remainingSeconds > 0);
                }
            }
            else
            {
                if (allSlots[i].cooldownImage != null) allSlots[i].cooldownImage.fillAmount = 0;
                if (allSlots[i].cooldownText != null)
                {
                    allSlots[i].cooldownText.text = "";
                    allSlots[i].cooldownText.enabled = false;
                }
            }
        }
    }

    public void UseDecoy()
    {
        currentDecoyCooldown = decoyCooldownTime;
        UpdateCooldownUI();
    }

    public bool IsDecoyReady()
    {
        return currentDecoyCooldown <= 0;
    }

    public void OnNextPageButton()
    {
        if (isAnimatingPage) return;
        if (currentPage < maxPages - 1)
        {
            StartCoroutine(ChangePageRoutine(1));
        }
    }

    public void OnPrevPageButton()
    {
        if (isAnimatingPage) return;
        if (currentPage > 0)
        {
            StartCoroutine(ChangePageRoutine(-1));
        }
    }

    IEnumerator ChangePageRoutine(int direction)
    {
        isAnimatingPage = true;

        if (backpackUIContainer != null)
        {
            float timer = 0f;
            float halfDuration = animationDuration / 2f;

            Quaternion startRot = Quaternion.identity;
            Quaternion endRot = Quaternion.Euler(0, 90f * direction, 0);

            while (timer < halfDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / halfDuration;
                t = t * t;
                backpackUIContainer.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            backpackUIContainer.localRotation = endRot;

            currentPage += direction;
            UpdateInventoryUI();
            UpdateDescriptionPanel();

            timer = 0f;
            startRot = Quaternion.Euler(0, -90f * direction, 0);
            endRot = Quaternion.identity;

            while (timer < halfDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / halfDuration;
                t = 1f - (1f - t) * (1f - t);
                backpackUIContainer.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            backpackUIContainer.localRotation = Quaternion.identity;
        }
        else
        {
            currentPage += direction;
            UpdateInventoryUI();
            UpdateDescriptionPanel();
        }

        isAnimatingPage = false;
    }

    public void UpdateInventoryUI()
    {
        if (allSlots != null)
        {
            for (int i = 0; i < allSlots.Length; i++)
            {
                int realIndex = GetRealDataIndex(i);
                if (realIndex < currentItems.Count && !string.IsNullOrEmpty(currentItems[realIndex]))
                {
                    allSlots[i].SetItem(GetItemIcon(currentItems[realIndex]));
                }
                else
                {
                    allSlots[i].ClearSlot();
                }
            }
        }

        UpdateDescriptionPanel();
        UpdateCooldownUI();

        if (pageNumberText != null) pageNumberText.text = (currentPage + 1) + " / " + maxPages;

        if (hudHotbarIcons != null)
        {
            for (int i = 0; i < hudHotbarIcons.Length; i++)
            {
                if (i >= currentItems.Count) break;
                if (!string.IsNullOrEmpty(currentItems[i]))
                {
                    hudHotbarIcons[i].sprite = GetItemIcon(currentItems[i]);
                    // ★追加：ここでHUDアイコンの大きさを設定したサイズに変更する
                    hudHotbarIcons[i].rectTransform.localScale = GetItemIconScale(currentItems[i]);
                    hudHotbarIcons[i].enabled = true;
                }
                else
                {
                    hudHotbarIcons[i].sprite = null;
                    // ★追加：アイテムが無いスロットは標準の大きさ(1,1,1)に戻しておく
                    hudHotbarIcons[i].rectTransform.localScale = Vector3.one;
                    hudHotbarIcons[i].enabled = false;
                }
            }
        }

        if (prevPageButton != null) prevPageButton.gameObject.SetActive(currentPage > 0);
        if (nextPageButton != null) nextPageButton.gameObject.SetActive(currentPage < maxPages - 1);
    }

    public void UpdateDescriptionPanel()
    {
        if (descriptionDisplayImage == null) return;
        int realIndex = GetRealDataIndex(currentCursorIndex);
        if (realIndex < 0 || realIndex >= currentItems.Count) { descriptionDisplayImage.enabled = false; return; }
        string itemName = currentItems[realIndex];
        if (string.IsNullOrEmpty(itemName)) { descriptionDisplayImage.enabled = false; return; }
        ItemData data = GetItemData(itemName);
        if (data != null && data.description != null)
        {
            descriptionDisplayImage.sprite = data.description;
            descriptionDisplayImage.rectTransform.anchoredPosition = data.descriptionPosition;
            descriptionDisplayImage.rectTransform.localScale = data.descriptionScale;
            descriptionDisplayImage.enabled = true;
        }
        else { descriptionDisplayImage.enabled = false; }
    }

    public ItemData GetItemData(string itemName)
    {
        foreach (var d in itemDataList) if (itemName.Contains(d.itemName)) return d;
        return null;
    }

    void HandleSpaceKeyAction()
    {
        int realCurrentIndex = GetRealDataIndex(currentCursorIndex);
        if (realCurrentIndex >= currentItems.Count || string.IsNullOrEmpty(currentItems[realCurrentIndex])) return;
        if (currentCursorIndex < 3) MoveItemToBackpack(realCurrentIndex);
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
        { if (string.IsNullOrEmpty(currentItems[i])) { emptySlot = i; break; } }
        if (emptySlot != -1)
        {
            currentItems[emptySlot] = currentItems[fromIndex];
            currentItems[fromIndex] = ""; UpdateInventoryUI();
            if (playerController != null) playerController.UpdateItemModel();
        }
        else ShowFullMessage();
    }
    public void SwapItems(int uiIndexA, int uiIndexB)
    {
        int realIndexA = GetRealDataIndex(uiIndexA);
        int realIndexB = GetRealDataIndex(uiIndexB); if (realIndexA >= currentItems.Count || realIndexB >= currentItems.Count) return;
        string temp = currentItems[realIndexA];
        currentItems[realIndexA] = currentItems[realIndexB];
        currentItems[realIndexB] = temp; UpdateInventoryUI();
        if (playerController != null) playerController.UpdateItemModel();
    }
    void HandleCursorMovement()
    {
        int prevIndex = currentCursorIndex; if (Input.GetKeyDown(KeyCode.W))
        {
            if (currentCursorIndex == 1) currentCursorIndex = 0;
            else if (currentCursorIndex == 2) currentCursorIndex = 1;
            else if (currentCursorIndex >= 6) currentCursorIndex -= 3;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (currentCursorIndex == 0) currentCursorIndex = 1;
            else if (currentCursorIndex == 1) currentCursorIndex = 2;
            else if (currentCursorIndex >= 3 && currentCursorIndex < 9) currentCursorIndex += 3;
        }
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
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (currentCursorIndex == 0) currentCursorIndex = 3;
            else if (currentCursorIndex == 1) currentCursorIndex = 6;
            else if (currentCursorIndex == 2) currentCursorIndex = 9;
            else if (currentCursorIndex >= 3)
            { if (currentCursorIndex != 5 && currentCursorIndex != 8 && currentCursorIndex != 11) currentCursorIndex += 1; }
        }
        if (prevIndex != currentCursorIndex) { UpdateCursorPosition(); UpdateDescriptionPanel(); }
    }
    void HandleSwapInput() { int targetUiSlot = -1; if (Input.GetKeyDown(KeyCode.Alpha1)) targetUiSlot = 0; if (Input.GetKeyDown(KeyCode.Alpha2)) targetUiSlot = 1; if (Input.GetKeyDown(KeyCode.Alpha3)) targetUiSlot = 2; if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) { EndSwapMode(); return; } if (targetUiSlot != -1) { SwapItems(currentCursorIndex, targetUiSlot); EndSwapMode(); } }
    void EndSwapMode() { isSwapMode = false; if (swapPromptPanel != null) swapPromptPanel.SetActive(false); if (playerController != null) playerController.SetBlurState(false); }
    void ShowFullMessage() { if (fullMessageText == null) return; if (messageCoroutine != null) StopCoroutine(messageCoroutine); messageCoroutine = StartCoroutine(FadeOutMessageRoutine()); }
    IEnumerator FadeOutMessageRoutine() { fullMessageText.gameObject.SetActive(true); if (fullMessageCanvasGroup != null) fullMessageCanvasGroup.alpha = 1f; yield return new WaitForSecondsRealtime(2.0f); float duration = 1.0f; float timer = 0f; while (timer < duration) { timer += Time.unscaledDeltaTime; if (fullMessageCanvasGroup != null) fullMessageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / duration); yield return null; } fullMessageText.gameObject.SetActive(false); }
    void HandleWeaponSwitchInput() { if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeSelectedSlot(0); if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeSelectedSlot(1); if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeSelectedSlot(2); }
    void UpdateCursorPosition() { if (cursorRect == null || allSlots == null || currentCursorIndex >= allSlots.Length) return; cursorRect.position = allSlots[currentCursorIndex].transform.position; }
    void UpdateHUDSlotSelection() { if (hudHotbarFrames == null) return; for (int i = 0; i < hudHotbarFrames.Length; i++) { if (hudHotbarFrames[i] == null) continue; Color c = hudHotbarFrames[i].color; float alpha = (i == equippedIndex) ? selectedAlpha : unselectedAlpha; c.a = alpha / 255f; hudHotbarFrames[i].color = c; } }
    private void InitializeInventorySlots() { if (allSlots == null) return; for (int i = 0; i < allSlots.Length; i++) { allSlots[i].slotIndex = i; allSlots[i].manager = this; } }
    public void ChangeSelectedSlot(int slotIndex) { if (slotIndex < 0 || slotIndex >= currentItems.Count || string.IsNullOrEmpty(currentItems[slotIndex])) { return; } equippedIndex = slotIndex; if (playerController != null) playerController.UpdateItemModel(); UpdateHUDSlotSelection(); }
    public void PickUpItem(GameObject itemObj)
    {
        string cleanName = itemObj.name.Replace("(Clone)", "").Trim();
        string tag = itemObj.tag;
        int emptyIndex = currentItems.IndexOf("");

        if (emptyIndex != -1)
        {
            currentItems[emptyIndex] = cleanName;
            if (!itemTagDatabase.ContainsKey(cleanName)) itemTagDatabase.Add(cleanName, tag);

            for (int i = 0; i < droppedItemsList.Count; i++)
            {
                if (droppedItemsList[i].itemName == cleanName)
                {
                    droppedItemsList.RemoveAt(i);
                    break;
                }
            }

            Destroy(itemObj);
            UpdateInventoryUI();
            if (emptyIndex == equippedIndex && playerController != null) playerController.UpdateItemModel();
        }
        else
        {
            ShowFullMessage();
        }
    }
    public void AddItem(string itemName) { int emptyIndex = currentItems.IndexOf(""); if (emptyIndex != -1) { currentItems[emptyIndex] = itemName; if (!itemTagDatabase.ContainsKey(itemName)) itemTagDatabase.Add(itemName, "Untagged"); UpdateInventoryUI(); UpdateHUDSlotSelection(); if (emptyIndex == equippedIndex && playerController != null) playerController.UpdateItemModel(); } else { ShowFullMessage(); } }
    public GameObject DropItem(string itemName, Vector3 position) { int index = currentItems.IndexOf(itemName); if (index == -1) return null; currentItems[index] = ""; UpdateInventoryUI(); if (!consumedItems.Contains(itemName)) { consumedItems.Add(itemName); } foreach (var pair in itemPrefabs) { if (pair.itemName == itemName || itemName.Contains(pair.itemName)) { GameObject droppedObj = Instantiate(pair.prefab, position, Quaternion.identity); droppedItemsList.Add(new DroppedItemInfo { itemName = itemName, position = position }); return droppedObj; } } return null; }
    public void RemoveItem(string itemName) { if (currentItems.Contains(itemName)) currentItems.Remove(itemName); if (!consumedItems.Contains(itemName)) { consumedItems.Add(itemName); } }

    public Sprite GetItemIcon(string itemName) { foreach (var d in itemDataList) if (itemName.Contains(d.itemName)) return d.icon; return null; }

    // ★追加：アイテムのアイコンサイズを取得する関数
    public Vector3 GetItemIconScale(string itemName) { foreach (var d in itemDataList) if (itemName.Contains(d.itemName)) return d.iconScale; return Vector3.one; }

    public string GetItemTag(string itemName) { if (itemTagDatabase.ContainsKey(itemName)) return itemTagDatabase[itemName]; return "Untagged"; }
    public bool HasItem(string itemName) { return currentItems.Contains(itemName); }
    public string GetEquippedItem() { if (currentItems != null && equippedIndex >= 0 && equippedIndex < currentItems.Count) return currentItems[equippedIndex]; return ""; }
    public List<string> GetItemDataForSave() { return currentItems; }

    public void LoadItemData(List<string> loadedItems)
    {
        currentItems = loadedItems;
        while (currentItems.Count < maxSlots) currentItems.Add("");
        itemTagDatabase.Clear();

        foreach (var item in currentItems)
        {
            if (!string.IsNullOrEmpty(item)) GetItemTag(item);
        }

        ReflectInventoryToScene();
        UpdateInventoryUI();
        UpdateHUDSlotSelection();

        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.UpdateItemModel();
        }
    }
    private void ReflectInventoryToScene()
    {
        foreach (string itemName in currentItems)
        {
            if (string.IsNullOrEmpty(itemName)) continue;
            GameObject obj = GameObject.Find(itemName);
            if (obj != null) obj.SetActive(false);
        }

        foreach (string itemName in consumedItems)
        {
            if (string.IsNullOrEmpty(itemName)) continue;
            GameObject obj = GameObject.Find(itemName);
            if (obj != null) obj.SetActive(false);
        }

        foreach (DroppedItemInfo info in droppedItemsList)
        {
            foreach (var pair in itemPrefabs)
            {
                if (pair.itemName == info.itemName || info.itemName.Contains(pair.itemName))
                {
                    Instantiate(pair.prefab, info.position, Quaternion.identity);
                    break;
                }
            }
        }
    }
}