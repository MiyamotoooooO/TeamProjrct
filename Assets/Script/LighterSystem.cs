using UnityEngine;
using System.Collections.Generic;

public class LighterSystem : MonoBehaviour
{
    [Header("連携設定")]
    [Tooltip("インベントリマネージャー")]
    public InventoryManager inventoryManager;

    [Tooltip("インベントリ内でのアイテム名（一文字でも違うと反応しません）")]
    public string lighterItemName = "Lighter";

    [Header("ライト設定")]
    [Tooltip("ライターの火（Lightコンポーネントがついているオブジェクト）")]
    public GameObject lightSource;
    [Tooltip("点火キー")]
    public KeyCode toggleKey = KeyCode.T;

    [Header("音の設定")]
    public AudioClip igniteSound;
    public AudioClip offSound;

    // 外部（イベント等）から制御するための変数
    // ※「ライターを持っていても、イベント中は使わせたくない」場合などに false にする
    [HideInInspector] public bool canUseLighter = true;

    // 内部状態
    [HideInInspector] public bool isLighterOn = false;
    private AudioSource audioSource;

    void Start()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindAnyObjectByType<InventoryManager>();
        }

        if (lightSource != null) lightSource.SetActive(false);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // 1. ストーリー上の許可が出ているか？
        if (!canUseLighter) return;

        // 2. 現在、手にライターを持っているか？
        bool isHoldingLighter = CheckIfHoldingLighter();

        // もしライターを持っていない（他のアイテムに持ち替えた）のに火がついていたら消す
        if (!isHoldingLighter && isLighterOn)
        {
            TurnOff();
        }

        // 3. キー入力判定
        if (Input.GetKeyDown(toggleKey))
        {
            if (isHoldingLighter)
            {
                ToggleLighter();
            }
            else
            {
                // 手に持っていない時はログを出す（デバッグ用）
                // Debug.Log("ライターを手に持っていません（装備してください）");
            }
        }
    }

    // ★追加：現在装備している（インベントリ先頭の）アイテムがライターか確認する
    bool CheckIfHoldingLighter()
    {
        if (inventoryManager != null && inventoryManager.currentItems.Count > 0)
        {
            // 先頭のアイテム名が lighterItemName と一致するか？
            return inventoryManager.currentItems[0] == lighterItemName;
        }
        return false;
    }

    void ToggleLighter()
    {
        isLighterOn = !isLighterOn;
        ApplyState();
        if (audioSource != null) audioSource.PlayOneShot(isLighterOn ? igniteSound : offSound);
    }

    public void TurnOff()
    {
        isLighterOn = false;
        ApplyState();
    }

    public void TurnOn()
    {
        // 強制点灯の場合も「持っているか」チェックを入れるのが安全ですが、
        // 演出で強制的に点けたい場合もあるので、ここはそのまま点灯させます
        isLighterOn = true;
        ApplyState();
    }

    public void ApplyState()
    {
        if (lightSource != null) lightSource.SetActive(isLighterOn);
    }
}