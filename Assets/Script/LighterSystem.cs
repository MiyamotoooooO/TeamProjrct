using UnityEngine;

public class LighterSystem : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("インベントリマネージャー")]
    public InventoryManager inventoryManager;

    [Tooltip("アイテムとしての名前（例: Lighter）")]
    public string lighterItemName = "Lighter";

    [Tooltip("火のオブジェクト（LightやParticle）")]
    public GameObject flameEffect;

    [Tooltip("ライト（光源）")]
    public Light lighterLight;

    [Header("状態フラグ")]
    // 演出（WakeUpControllerなど）から操作されるフラグ
    // trueなら使用許可、falseなら強制禁止
    public bool canUseLighter = true;

    // 現在火がついているかどうか
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
        // ----------------------------------------------------
        // 1. 【最優先】演出中は強制的にオフ＆操作禁止
        // ----------------------------------------------------
        if (!canUseLighter)
        {
            // もし火がついていたら消す
            if (isLighterOn)
            {
                TurnOff();
            }
            return; // ここで処理を終わらせる（他のスクリプトは無視！）
        }

        // ----------------------------------------------------
        // 2. インベントリチェック（メイン枠にあるか？）
        // ----------------------------------------------------
        if (!IsHoldingLighter())
        {
            // 持っていないのに火がついていたら消す（装備を変えた時など）
            if (isLighterOn)
            {
                TurnOff();
            }
            return; // 持っていないので操作させない
        }

        // ----------------------------------------------------
        // 3. 入力処理（ここまで来た＝使ってOKな状態）
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.T))
        {
            isLighterOn = !isLighterOn; // ON/OFF切り替え
            ApplyState();

            // 音を鳴らしたい場合はここに PlayOneShot など
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

    // メイン枠（左上）にライターがあるか確認する関数
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