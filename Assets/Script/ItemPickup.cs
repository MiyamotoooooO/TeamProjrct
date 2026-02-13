using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("表示設定")]
    [Tooltip("常に表示させるオブジェクト（子オブジェクトのスプライトやテキスト）")]
    public GameObject nameLabelObject;

    // 距離設定（displayDistance）は不要になったため削除しました

    [Header("位置調整")]
    [Tooltip("中心位置からの微調整 (X, Y, Z)")]
    public Vector3 labelOffset = new Vector3(0f, 0.5f, 0f);

    // 内部変数
    private Transform mainCameraTransform; // プレイヤー（カメラ）の位置
    private Collider itemCollider;         // アイテムの形（中心）を知るため
    private InventoryManager inventoryManager; // 拾う処理用

    void Start()
    {
        // 必要なコンポーネントを取得
        itemCollider = GetComponent<Collider>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();

        // ★変更点1：距離に関係なく、最初から表示状態にする
        if (nameLabelObject != null)
        {
            nameLabelObject.SetActive(true);
        }

        // カメラ（プレイヤー視点）を取得
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    // ★変更点2：距離判定を行っていた Update() は不要になったため削除しました

    // カメラやプレイヤーが動いた後に位置合わせをする（ビルボード処理）
    void LateUpdate()
    {
        // カメラが見つかっていない、またはラベルが消えている（拾われた後など）場合は処理しない
        if (nameLabelObject == null || !nameLabelObject.activeInHierarchy || mainCameraTransform == null) return;

        // --- 1. 位置の固定 ---
        Vector3 targetCenter = (itemCollider != null) ? itemCollider.bounds.center : transform.position;
        nameLabelObject.transform.position = targetCenter + labelOffset;

        // --- 2. 回転の制御（常にカメラの方を向く） ---
        Vector3 targetPosition = mainCameraTransform.position;

        // 高さを合わせてY軸回転のみにする（看板が変に傾かないように）
        targetPosition.y = nameLabelObject.transform.position.y;

        // ① まずカメラの方を向く
        nameLabelObject.transform.LookAt(targetPosition);

        // ② そのまま180度回して「正面」を見せる（LookAtはZ軸を向けるため、UIなどは裏返る対策）
        nameLabelObject.transform.Rotate(0, 180, 0);
    }

    // 外部（PlayerControllerなど）から呼ばれる用
    public void PickUp()
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