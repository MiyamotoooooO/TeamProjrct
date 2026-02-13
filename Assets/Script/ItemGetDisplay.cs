using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemGetDisplay : MonoBehaviour
{
    [Header("UI参照")]
    public GameObject itemGetPanel;
    public Image displayImage;
    public TextMeshProUGUI messageText;

    [Header("参照スクリプト")]
    public PlayerController playerController;
    public InventoryManager inventoryManager;

    // 演出中かどうか
    public bool isDisplaying = false;

    void Start()
    {
        // 最初は隠しておく
        if (itemGetPanel != null) itemGetPanel.SetActive(false);
    }

    void Update()
    {
        // 表示中だけ Spaceキーの入力を受け付ける
        if (isDisplaying && Input.GetKeyDown(KeyCode.Space))
        {
            CloseDisplay();
        }
    }

    // アイテム入手時にPlayerControllerから呼ばれる関数
    public void ShowItemGet(string itemName)
    {
        Debug.Log("ShowItemGetが呼ばれた");
        if (inventoryManager == null) return;

        // 1. アイテム名からアイコンを取得
        Sprite icon = inventoryManager.GetItemIcon(itemName);

        // アイコンがあれば表示
        if (icon != null && displayImage != null)
        {
            displayImage.sprite = icon;
            displayImage.preserveAspect = true;
            displayImage.enabled = true;
        }
        else
        {
            if (displayImage != null) displayImage.enabled = false;
        }

        // 2. テキスト更新
        if (messageText != null)
        {
            messageText.text = $"{itemName} を手に入れた！";
        }

        // 3. パネルを表示
        if (itemGetPanel != null) itemGetPanel.SetActive(true);

        // 4. フラグをON
        isDisplaying = true;

        // 5. プレイヤーの動きを止める & Blurをオンにする
        if (playerController != null)
        {
            playerController.canControl = false; // 操作不能に
            playerController.SetBlurState(true); // ブラーON

            // 物理的な滑りを止める
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;

            // カーソルを表示する（必要なら）
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;
        }
    }

    // 閉じる処理
    void CloseDisplay()
    {
        // 1. パネルを隠す
        if (itemGetPanel != null) itemGetPanel.SetActive(false);

        // 2. フラグをOFF
        isDisplaying = false;

        // 3. プレイヤーの操作を戻す
        if (playerController != null)
        {
            playerController.canControl = true;  // 操作可能に
            playerController.SetBlurState(false); // ブラーOFF

            // カーソルをロックに戻す
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}