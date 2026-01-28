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
    public GameObject lightSource;
    public KeyCode toggleKey = KeyCode.T;
    public AudioClip igniteSound;
    public AudioClip offSound;

    [Header("状態確認")]
    public bool hasLighterItem = false;

    [HideInInspector] public bool isLighterOn = false;
    [HideInInspector] public bool canUseLighter = true;

    private AudioSource audioSource;

    void Start()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindAnyObjectByType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogError("【エラー】LighterSystem: InventoryManagerが見つかりません！Hierarchyにありますか？");
            }
            else
            {
                Debug.Log("【成功】LighterSystem: InventoryManagerを見つけました。");
            }
        }

        if (lightSource != null) lightSource.SetActive(false);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        Debug.Log("ライターはつけれないよ");
        if (!canUseLighter) return;
        Debug.Log("ライターの許可");

        // --- ★デバッグ用チェック ---
        if (inventoryManager != null && inventoryManager.currentItems != null)
        {
            // インベントリにあるかチェック
            bool found = inventoryManager.currentItems.Contains(lighterItemName);

            // 状態が変わった時だけログを出す（毎フレーム出るとうるさいので）
            if (found != hasLighterItem)
            {
                hasLighterItem = found;
                if (found)
                {
                    Debug.Log($"【発見】インベントリ内に '{lighterItemName}' を検知しました！ライター使用可能です。");
                }
                else
                {
                    Debug.Log($"【未発見】インベントリ内に '{lighterItemName}' が見つかりません。");
                    // 現在の中身をすべて表示してみる（名前ミスの確認用）
                    string allItems = string.Join(", ", inventoryManager.currentItems);
                    Debug.Log($"　→ 現在のインベントリの中身: [{allItems}]");
                }
            }
        }
        // ---------------------------

        if (Input.GetKeyDown(toggleKey))
        {
            if (hasLighterItem)
            {
                Debug.Log("ライター着火");
                ToggleLighter();
            }
            else
            {
                // 押したときに詳細な理由を表示
                if (inventoryManager == null) Debug.Log("エラー: InventoryManagerが空です。");
                else Debug.Log($"失敗: インベントリに '{lighterItemName}' がありません。");
            }
        }

        if (isLighterOn && !hasLighterItem)
        {
            TurnOff();
        }
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
        isLighterOn = true;
        ApplyState();
    }

    public void ApplyState()
    {
        if (lightSource != null) lightSource.SetActive(isLighterOn);
    }
}