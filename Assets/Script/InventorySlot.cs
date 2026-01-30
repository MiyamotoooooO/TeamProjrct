using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("部品の割り当て")]
    [Tooltip("アイテムアイコンを表示するImage")]
    public Image iconImage;

    [Tooltip("枠の画像（このオブジェクト自身のImage）")]
    public Image frameImage;

    [Tooltip("ボタンコンポーネント")]
    public Button button;

    [Header("枠のデザイン設定（ここに画像をセットする）")]
    [Tooltip("通常時（選択されていない時）の枠画像")]
    public Sprite normalSprite; // ★追加：通常の画像

    [Tooltip("選択されている時の枠画像（メイン表示）")]
    public Sprite selectedSprite; // ★追加：メインの画像

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

    // アイテムをセットして表示する（ここは変更なし）
    public void SetItem(Sprite itemSprite)
    {
        if (iconImage == null) return;
        iconImage.sprite = itemSprite;
        iconImage.enabled = true;
    }

    // アイテムをクリアして非表示にする（ここは変更なし）
    public void ClearSlot()
    {
        if (iconImage == null) return;
        iconImage.sprite = null;
        iconImage.enabled = false;
    }

    // ★ここが重要！選択状態に合わせて「画像」を切り替える
    public void SetSelected(bool isSelected)
    {
        if (frameImage == null) return;

        // 画像をきれいに表示するために、色は常に白（透明）にしておく
        frameImage.color = Color.white;

        if (isSelected)
        {
            // ■ メインに選ばれた時
            // 「メイン画像」が設定されていれば、それに切り替える
            if (selectedSprite != null)
            {
                frameImage.sprite = selectedSprite;
            }
        }
        else
        {
            // ■ 通常に戻った時
            // 「通常画像」が設定されていれば、それに切り替える
            if (normalSprite != null)
            {
                frameImage.sprite = normalSprite;
            }
        }
    }
}