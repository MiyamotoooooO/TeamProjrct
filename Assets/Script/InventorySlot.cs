using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI参照")]
    public Image iconImage;   // 中身のアイコン
    public Image frameImage;  // 外枠

    [Tooltip("クールダウンを示す半透明の円（Filled Image）")]
    public Image cooldownImage;

    [Tooltip("クールダウンの残り秒数を表示するテキスト")]
    public TextMeshProUGUI cooldownText; // ★追加：数字表示用

    // 自動設定される変数
    [HideInInspector] public int slotIndex;
    [HideInInspector] public InventoryManager manager;

    public void SetItem(Sprite itemSprite)
    {
        if (iconImage == null) return;
        iconImage.sprite = itemSprite;
        iconImage.enabled = true;

        // アイテムが入った時はクールダウン表示をリセット
        if (cooldownImage != null) cooldownImage.fillAmount = 0;
        if (cooldownText != null) cooldownText.text = "";
    }

    public void ClearSlot()
    {
        if (iconImage == null) return;
        iconImage.sprite = null;
        iconImage.enabled = false;

        // 空っぽならクールダウン表示も消す
        if (cooldownImage != null) cooldownImage.fillAmount = 0;
        if (cooldownText != null) cooldownText.text = "";
    }
}