using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class InventoryUIController : MonoBehaviour
{
    [Header("InventoryのPanelを参照")]
    public GameObject inventoryPanel;

    [Tooltip("PlayerControllerを参照")]
    public PlayerController playerController;

    [Tooltip("Inventoryの開閉キー")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("ぼかしのPostProcessVoluneを参照")]
    public PostProcessVolume blurVolume;

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