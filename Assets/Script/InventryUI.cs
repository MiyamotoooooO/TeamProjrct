using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventryUI : MonoBehaviour
{
    [Header("インベントリ管理")]
    public InventoryManager inventoryManager;

    [Header("インベントリUIパネル")]
    public GameObject inventoryPanel;

    [Header("アイテム一覧表示")]
    public Transform itemGrid;

    [Header("アイコンのプレハブ")]
    public GameObject itemIconPrefab;

    public PlayerController playerController;

    private bool isOpen = false;

    private void Start()
    {
        // 最初は閉じておく
        inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);

        if (isOpen)
        {
            RefreshInventoryUI();
            playerController.isInventoryOpen = true;
            playerController.canLock = false; // 視点ロック
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            playerController.isInventoryOpen = false;
            playerController.canLock = true; // 視点解除
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void RefreshInventoryUI()
    {
        // 既存のアイコンを全部削除
        foreach (Transform child in itemGrid)
        {
            Destroy(child.gameObject);
        }

        // アイテムをアイコンとして生成
        foreach (string itemName in inventoryManager.currentItems)
        {
            GameObject iconObj = Instantiate(itemIconPrefab, itemGrid);
            Image img = iconObj.GetComponent<Image>();

            Sprite icon = inventoryManager.GetItemIcon(itemName);
            if (icon != null)
            {
                img.sprite = icon;
            }
        }
    }

}

