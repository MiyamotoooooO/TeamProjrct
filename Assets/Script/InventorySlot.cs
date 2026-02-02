using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("アイテムアイコンを表示するImage")]
    public Image iconImage;

    [Header("Itemの枠の画像")]
    public Image frameImage;

    [Header("ボタンコンポーネントを追加")]
    public Button button;

    [Header(" ---枠のデザイン設定---")]
    [Header("通常時の枠画像")]
    public Sprite normalSprite;

    [Header("選択されている時の枠画像")]
    public Sprite selectedSprite;

    void Awake()
    {
        // 自動でコンポーネントを取得
        if (frameImage == null) frameImage = GetComponent<Image>();
        if (button == null) button = GetComponent<Button>();

        // ゲーム開始時、念のため最初は「通常画像」にしておく
        if (frameImage != null && normalSprite != null)
        {
            frameImage.sprite = normalSprite;
            // 色による着色はリセットして、画像本来の色で表示する
            frameImage.color = Color.white;
        }
    }

    // アイテムをセットして表示する
    public void SetItem(Sprite itemSprite)
    {
        if (iconImage == null) return;
        iconImage.sprite = itemSprite;
        iconImage.enabled = true;
    }

    // アイテムをクリアして非表示にする
    public void ClearSlot()
    {
        if (iconImage == null) return;
        iconImage.sprite = null;
        iconImage.enabled = false;
    }

    // 選択状態に合わせて画像を切り替える
    public void SetSelected(bool isSelected)
    {
        if (frameImage == null) return;

        // 画像をきれいに表示するために、色は常に白（透明）にしておく
        frameImage.color = Color.white;

        if (isSelected)
        {
            // メインに選ばれた時
            if (selectedSprite != null)
            {
                frameImage.sprite = selectedSprite;
            }
        }
        else
        {
            // 通常に戻った時
            if (normalSprite != null)
            {
                frameImage.sprite = normalSprite;
            }
        }
    }
}