using UnityEngine;
using UnityEngine.Rendering.PostProcessing; // ★ここが変わりました！

public class InventoryUIController : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("インベントリのパネル")]
    public GameObject inventoryPanel;

    [Tooltip("プレイヤーコントローラー")]
    public PlayerController playerController;

    [Tooltip("開閉キー")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("ぼかし設定")]
    [Tooltip("さっき作った InventoryBlurVolume をここに入れる")]
    public PostProcessVolume blurVolume; // ★型が変わりました！

    void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (blurVolume != null) blurVolume.weight = 0f; // 最初はボケなし
    }

    void Update()
    {
        if (playerController == null || !playerController.enabled || !playerController.canControl)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        if (inventoryPanel == null || playerController == null) return;

        bool isActive = !inventoryPanel.activeSelf;

        // パネルの表示切り替え
        inventoryPanel.SetActive(isActive);

        // プレイヤーの操作ロック切り替え
        playerController.isInventoryOpen = isActive;

        // ぼかしの切り替え
        if (blurVolume != null)
        {
            blurVolume.weight = isActive ? 1f : 0f;
        }
    }
}