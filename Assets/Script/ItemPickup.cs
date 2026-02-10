using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("表示設定")]
    [Tooltip("近づいた時に表示させるオブジェクト（子オブジェクトのスプライトやテキスト）")]
    public GameObject nameLabelObject;

    [Tooltip("表示を開始する距離（メートル）")]
    public float displayDistance = 3.0f; // 初期値3m

    [Header("位置調整")]
    [Tooltip("中心位置からの微調整 (X, Y, Z)")]
    public Vector3 labelOffset = new Vector3(0f, 0.5f, 0f);

    //[Header("キー設定")]
    //public KeyCode pickupKey = KeyCode.E; // 拾うボタン

    // 内部変数
    private Transform mainCameraTransform; // プレイヤー（カメラ）の位置
    private Collider itemCollider;         // アイテムの形（中心）を知るため
    private InventoryManager inventoryManager; // 拾う処理用

    void Start()
    {
        // 必要なコンポーネントを取得
        itemCollider = GetComponent<Collider>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();

        // 最初はラベルを隠す
        if (nameLabelObject != null)
        {
            nameLabelObject.SetActive(false);
        }

        // カメラ（プレイヤー視点）を取得
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (mainCameraTransform == null) return;

        // プレイヤーとの距離を計算
        float distance = Vector3.Distance(transform.position, mainCameraTransform.position);

        // --- 距離判定 ---
        if (distance <= displayDistance)
        {
            // 範囲内：ラベルを表示
            if (nameLabelObject != null && !nameLabelObject.activeSelf)
            {
                nameLabelObject.SetActive(true);
            }

            // 拾うキー入力の受付
            //if (Input.GetKeyDown(pickupKey))
            //{
              //  PickUp();
            //}
        }
        else
        {
            // 範囲外：ラベルを非表示
            if (nameLabelObject != null && nameLabelObject.activeSelf)
            {
                nameLabelObject.SetActive(false);
            }
        }
    }

    // カメラやプレイヤーが動いた後に位置合わせをする
    void LateUpdate()
    {
        if (nameLabelObject == null || !nameLabelObject.activeSelf || mainCameraTransform == null) return;

        // --- 1. 位置の固定 ---
        Vector3 targetCenter = (itemCollider != null) ? itemCollider.bounds.center : transform.position;
        nameLabelObject.transform.position = targetCenter + labelOffset;

        // --- 2. 回転の制御（反転対策済み） ---

        Vector3 targetPosition = mainCameraTransform.position;

        // 高さを合わせてY軸回転のみにする
        targetPosition.y = nameLabelObject.transform.position.y;

        // ① まずカメラの方を向く（これだと裏返しになる）
        nameLabelObject.transform.LookAt(targetPosition);

        // ② ★追加：そのまま180度回して「正面」を見せる
        nameLabelObject.transform.Rotate(0, 180, 0);
    }

    void PickUp()
    {
        if (inventoryManager != null)
        {
            inventoryManager.PickUpItem(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}