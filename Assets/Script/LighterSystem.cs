using UnityEngine;

public class LighterSystem : MonoBehaviour
{
    [Header("InventoryManagerを参照")]
    public InventoryManager inventoryManager;

    [Header("ライターのLayer名")]
    public string lighterItemName = "Lighter";

    [Header("TorchEffectを参照")]
    public GameObject flameEffect;

    [Tooltip("SpotLightを参照")]
    public Light lighterLight;

    [Header("使用可能かどうかのフラグ")]
    public bool canUseLighter = true;

    [Header("現在火がついてるかどうかのフラグ")]
    public bool isLighterOn = false;

    void Start()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();

        // ゲーム開始時、まずは火を消した状態でスタート
        isLighterOn = false;
        ApplyState();
    }

    void Update()
    {
        // 1. 演出中は強制的にオフ＆操作禁止
        if (!canUseLighter)
        {
            // もし火がついていたら消す
            if (isLighterOn)
            {
                TurnOff();
            }
            return; // ここで処理を終わらせる
        }
        
        // 2. インベントリチェック
        if (!IsHoldingLighter())
        {
            // 持っていないのに火がついていたら消す
            if (isLighterOn)
            {
                TurnOff();
            }
            return; // 持っていないので操作させない
        }

        // 3. 入力処理
        if (Input.GetKeyDown(KeyCode.T))
        {
            isLighterOn = !isLighterOn; // ON/OFF切り替え
            ApplyState();

            Debug.Log("ライターの状態: " + isLighterOn);
        }
    }

    // 火を消す専用関数
    void TurnOff()
    {
        isLighterOn = false;
        ApplyState();
    }

    // 実際の見た目を反映する関数
    public void ApplyState()
    {
        if (flameEffect != null) flameEffect.SetActive(isLighterOn);
        if (lighterLight != null) lighterLight.enabled = isLighterOn;
    }

    // メイン枠にライターがあるか確認する関数
    bool IsHoldingLighter()
    {
        if (inventoryManager != null)
        {
            // メイン枠のアイテム名を取得
            string equipped = inventoryManager.GetEquippedItem();

            // 名前が含まれているかチェック
            if (!string.IsNullOrEmpty(equipped) && equipped.Contains(lighterItemName))
            {
                return true;
            }
        }
        return false;
    }
}