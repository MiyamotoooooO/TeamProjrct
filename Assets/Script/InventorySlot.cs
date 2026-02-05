using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [Header("UI参照")]
    public Image iconImage;   // 中身のアイコン
    public Image frameImage;  // 外枠

    // 自動設定される変数
    [HideInInspector] public int slotIndex;
    [HideInInspector] public InventoryManager manager;

    public void SetItem(Sprite itemSprite)
    {
        if (iconImage == null) return;
        iconImage.sprite = itemSprite;
        iconImage.enabled = true;
    }

    public void ClearSlot()
    {
        if (iconImage == null) return;
        iconImage.sprite = null;
        iconImage.enabled = false;
    }
}





/*using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI参照")]
    public Image iconImage;
    public Image frameImage;
    public Button button;

    [Header("デザイン")]
    public Sprite normalSprite;
    public Sprite selectedSprite;

    [HideInInspector] public int slotIndex;
    [HideInInspector] public InventoryManager manager;

    private GameObject dragIconObject;
    private Canvas parentCanvas;

    void Awake()
    {
        if (frameImage == null) frameImage = GetComponent<Image>();
        if (button == null) button = GetComponent<Button>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void SetItem(Sprite itemSprite)
    {
        if (iconImage == null) return;
        iconImage.sprite = itemSprite;
        iconImage.enabled = true;
    }

    public void ClearSlot()
    {
        if (iconImage == null) return;
        iconImage.sprite = null;
        iconImage.enabled = false;
    }

    // ★ドラッグ開始の合図
    public void OnBeginDrag(PointerEventData eventData)
    {
        // ログが出なければ、クリック自体が反応していません（Raycast Targetを確認！）
        Debug.Log($"[Slot {slotIndex}] ドラッグ開始しようとしています...");

        if (iconImage.sprite == null || !iconImage.enabled)
        {
            Debug.Log("→ アイテムがないので中止しました");
            return;
        }
        if (parentCanvas == null) return;

        dragIconObject = new GameObject("DragIcon");
        dragIconObject.transform.SetParent(parentCanvas.transform);
        dragIconObject.transform.SetAsLastSibling();
        dragIconObject.transform.position = transform.position;

        Image img = dragIconObject.AddComponent<Image>();
        img.sprite = iconImage.sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;

        RectTransform rt = dragIconObject.GetComponent<RectTransform>();
        RectTransform originalRt = GetComponent<RectTransform>();
        rt.sizeDelta = originalRt.sizeDelta;

        Color c = iconImage.color;
        c.a = 0.3f;
        iconImage.color = c;

        Debug.Log("→ ドラッグ成功！仮アイコン生成完了");
    }

    // ★ドラッグ中
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIconObject != null)
        {
            dragIconObject.transform.position = eventData.position;
        }
    }

    // ★ドロップ（離した時）
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[Slot {slotIndex}] 何かがドロップされました！");

        if (eventData.pointerDrag == null) return;

        InventorySlot droppedSlot = eventData.pointerDrag.GetComponent<InventorySlot>();

        if (droppedSlot != null && manager != null)
        {
            Debug.Log($"→ Slot {droppedSlot.slotIndex} から Slot {this.slotIndex} へ入れ替えを実行します");
            manager.SwapItems(droppedSlot.slotIndex, this.slotIndex);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIconObject != null) Destroy(dragIconObject);
        if (iconImage != null)
        {
            Color c = iconImage.color;
            c.a = 1f;
            iconImage.color = c;
        }
    }
}*/