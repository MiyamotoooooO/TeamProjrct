using UnityEngine;

public class FlashlightSystem : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("ライトの本体（Spotlightなどの光源）")]
    public GameObject lightSource;

    [Tooltip("ライトのON/OFFを切り替えるキー")]
    public KeyCode toggleKey = KeyCode.F;

    [Tooltip("インベントリマネージャーを参照")]
    public InventoryManager inventoryManager;

    [Tooltip("アイテムとして認識する名前（InventoryManagerの登録名）")]
    public string flashlightItemName = "Flashlight";

    // 外部から制御するための変数
    [HideInInspector] public bool isFlashlightOn = false;
    [HideInInspector] public bool canUseFlashlight = true;

    void Start()
    {
        if (inventoryManager == null)
            inventoryManager = Object.FindAnyObjectByType<InventoryManager>();

        isFlashlightOn = false; // 最初はOFF
        ApplyState();
    }

    void Update()
    {
        // ★変更：現在持っているアイテムがFlashlightかチェック
        bool isHoldingFlashlight = CheckIfHoldingFlashlight();

        // 懐中電灯を持っていないのにライトが点いていたら消す
        if (!isHoldingFlashlight && isFlashlightOn)
        {
            isFlashlightOn = false;
            ApplyState();
        }

        // Fキーが押されたら切り替える処理
        if (Input.GetKeyDown(toggleKey))
        {
            // ライトが使用可能 かつ 懐中電灯を手に持っている時だけ切り替え
            if (canUseFlashlight && isHoldingFlashlight)
            {
                ToggleFlashlight();
            }
        }
    }

    // 現在の装備アイテムが懐中電灯か確認する
    bool CheckIfHoldingFlashlight()
    {
        if (inventoryManager != null && inventoryManager.currentItems.Count > 0)
        {
            // インベントリの先頭のアイテム名が "Flashlight" なら true
            return inventoryManager.GetEquippedItem().Contains(flashlightItemName);
        }
        return false;
    }

    void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;
        ApplyState();
    }

    public void ApplyState()
    {
        if (lightSource != null)
        {
            lightSource.SetActive(isFlashlightOn);
        }
    }

    public void TurnOff()
    {
        if (!canUseFlashlight) return;
        isFlashlightOn = false;
        ApplyState();
    }

    public void TurnOn()
    {
        if (!canUseFlashlight) return;
        isFlashlightOn = true;
        ApplyState();
    }
}