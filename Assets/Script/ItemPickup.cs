using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("アイテム設定")]
    [Tooltip("インベントリに登録される名前（空欄ならオブジェクト名が使われます）")]
    public string itemName;

    [Tooltip("近づいた時に表示させる名前のオブジェクト（SpriteやText）")]
    public GameObject nameLabelObject;

    [Header("キー設定")]
    public KeyCode pickupKey = KeyCode.E; // 拾うボタン

    // 内部変数
    private bool isPlayerNearby = false;
    private InventoryManager inventoryManager;
    private Transform mainCameraTransform;

    void Start()
    {
        inventoryManager = FindAnyObjectByType<InventoryManager>();

        // もし名前が設定されていなければ、オブジェクト名をそのまま使う
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = gameObject.name.Replace("(Clone)", "").Trim();
        }

        // 最初は名前ラベルを隠す
        if (nameLabelObject != null)
        {
            nameLabelObject.SetActive(false);
        }

        // カメラを取得（ラベルを常にカメラに向けるため）
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // プレイヤーが近くにいないなら何もしない
        if (!isPlayerNearby) return;

        // ★ラベルを常にカメラの方向に向ける（ビルボード処理）
        if (nameLabelObject != null && mainCameraTransform != null)
        {
            nameLabelObject.transform.LookAt(
                nameLabelObject.transform.position + mainCameraTransform.rotation * Vector3.forward,
                mainCameraTransform.rotation * Vector3.up
            );
        }

        // 拾うボタンが押されたら
        if (Input.GetKeyDown(pickupKey))
        {
            PickUp();
        }
    }

    void PickUp()
    {
        if (inventoryManager != null)
        {
            // InventoryManagerのPickUpItem機能を呼ぶ
            // （このオブジェクト自体を渡して、削除や登録を任せる）

            // 名前を確実に渡すためにオブジェクト名を変更しておく（InventoryManagerが名前で判定しているため）
            this.gameObject.name = itemName;

            inventoryManager.PickUpItem(this.gameObject);
        }
        else
        {
            // 万が一InventoryManagerがない場合でも消えるようにしておく
            Destroy(gameObject);
        }
    }

    // 範囲に入ったら表示
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (nameLabelObject != null) nameLabelObject.SetActive(true);
        }
    }

    // 範囲から出たら非表示
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (nameLabelObject != null) nameLabelObject.SetActive(false);
        }
    }
}