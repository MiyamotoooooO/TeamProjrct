using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("アイテム設定")]
    [Tooltip("近づいた時に表示させる名前のオブジェクト（SpriteやText）")]
    public GameObject nameLabelObject;

    [Header("キー設定")]
    public KeyCode pickupKey = KeyCode.E; // 拾うボタン

    // 内部変数
    private bool isPlayerNearby = false;
    //private InventoryManager inventoryManager;
    private Transform mainCameraTransform;

    void Start()
    {
        //inventoryManager = FindAnyObjectByType<InventoryManager>();

        // 最初は名前ラベルを隠す
        if (nameLabelObject != null)
        {
            nameLabelObject.SetActive(false);
        }

        // カメラを取得
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // プレイヤーが近くにいないなら何もしない
        if (!isPlayerNearby) return;

        // ラベルを常にカメラの方向に向ける
        if (nameLabelObject != null && mainCameraTransform != null)
        {
            nameLabelObject.transform.LookAt(
                nameLabelObject.transform.position + mainCameraTransform.rotation * Vector3.forward,
                mainCameraTransform.rotation * Vector3.up
            );
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