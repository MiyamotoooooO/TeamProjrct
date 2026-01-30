using UnityEngine;

public class FlashlightSystem : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("インベントリマネージャー")]
    public InventoryManager inventoryManager;

    [Tooltip("アイテムとしての名前")]
    public string flashlightItemName = "Flashlight";

    [Tooltip("懐中電灯のライト（Spotlightなど）")]
    public Light flashlightLight;

    [Header("状態")]
    // 現在ライトがついているかどうか
    public bool isFlashlightOn = false;

    void Start()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();

        // ゲーム開始時は消しておく
        isFlashlightOn = false;
        ApplyState();
    }

    void Update()
    {
        // ----------------------------------------------------
        // 判定：メイン枠（左上）に懐中電灯があるか？
        // ----------------------------------------------------
        bool hasFlashlight = IsHoldingFlashlight();

        if (hasFlashlight)
        {
            // ■ 持っている場合
            // Fキーを押したらON/OFFを切り替える
            if (Input.GetKeyDown(KeyCode.F))
            {
                isFlashlightOn = !isFlashlightOn;
                ApplyState();
            }
        }
        else
        {
            // ■ 持っていない場合
            // もしライトがつけっぱなしなら、強制的に消す
            if (isFlashlightOn)
            {
                isFlashlightOn = false;
                ApplyState();
            }
        }
    }

    // 実際のライトのON/OFFを反映する関数
    public void ApplyState()
    {
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isFlashlightOn;
        }
    }

    // メイン枠（左上）に懐中電灯があるか確認する関数
    bool IsHoldingFlashlight()
    {
        if (inventoryManager != null)
        {
            // メイン枠のアイテム名を取得
            string equipped = inventoryManager.GetEquippedItem();

            // 名前が含まれているかチェック
            if (!string.IsNullOrEmpty(equipped) && equipped.Contains(flashlightItemName))
            {
                return true;
            }
        }
        return false;
    }
}