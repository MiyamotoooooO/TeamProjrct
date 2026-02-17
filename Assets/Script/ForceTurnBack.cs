using System.Collections;
using UnityEngine;

public class ForceTurnBack : MonoBehaviour
{
    [Header("必須設定")]
    [Tooltip("チェック対象となるSearchPointViewer（手がかり）")]
    public SearchPointViewer targetSearchPoint;

    [Tooltip("【重要】物理的に通さないための壁オブジェクト（BoxColliderなど）")]
    public GameObject invisibleWall;

    [Header("通過条件")]
    [Tooltip("このタグを持つアイテムを持っていないと通れない")]
    public string requiredItemTag = "Key";

    [Header("強制移動の設定")]
    // targetRotationY は自動計算するため削除しました

    [Tooltip("強制的に歩かせる歩数")]
    public int stepsToWalk = 3;

    [Tooltip("振り向く速さ")]
    public float turnSpeed = 5.0f;

    [Tooltip("強制歩行の速さ")]
    public float walkSpeed = 3.0f;

    // 内部変数
    private PlayerController playerController;
    private InventoryManager inventoryManager;
    private bool isEventActive = false;

    void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();

        if (invisibleWall != null) invisibleWall.SetActive(true);
    }

    void Update()
    {
        if (CheckPassCondition())
        {
            if (invisibleWall != null) invisibleWall.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isEventActive) return;
        if (CheckPassCondition()) return;

        StartCoroutine(TurnBackRoutine(other.transform));
    }

    private bool CheckPassCondition()
    {
        bool hasViewed = (targetSearchPoint != null && targetSearchPoint.hasBeenViewed);
        if (!hasViewed) return false;

        bool hasKey = HasItemWithTag(requiredItemTag);
        if (!hasKey) return false;

        return true;
    }

    private bool HasItemWithTag(string tagToCheck)
    {
        if (inventoryManager == null) return false;
        foreach (string itemName in inventoryManager.currentItems)
        {
            if (string.IsNullOrEmpty(itemName)) continue;
            string itemTag = inventoryManager.GetItemTag(itemName);
            if (itemTag == tagToCheck) return true;
        }
        return false;
    }

    IEnumerator TurnBackRoutine(Transform playerTransform)
    {
        isEventActive = true;

        // 1. 操作禁止 & 停止
        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // 2. 振り向き処理（180度Uターン ＋ 正面を向く）
        Quaternion startRot = playerTransform.rotation;

        // 現在の向きの「真後ろ」を計算してターゲットにする
        Vector3 backwardDirection = -playerTransform.forward;
        backwardDirection.y = 0; // 上下方向の傾きは無視して水平にする
        Quaternion targetRot = Quaternion.LookRotation(backwardDirection);

        // カメラの上下角度もリセットするための準備
        Transform camTransform = null;
        Quaternion startCamRot = Quaternion.identity;
        Quaternion targetCamRot = Quaternion.Euler(0, 0, 0); // 正面（水平）

        if (playerController != null && playerController.cam != null)
        {
            camTransform = playerController.cam.transform;
            startCamRot = camTransform.localRotation;
        }

        float t = 0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime * turnSpeed;

            // 体の回転（Y軸）
            playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            // カメラの回転（視線を正面に戻す）
            if (camTransform != null)
            {
                camTransform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
            }

            // PlayerController内の内部変数とも同期させる
            if (playerController != null) playerController.SyncRotationToCurrent();

            yield return null;
        }

        // 念の為、最終値をきっちりセット
        playerTransform.rotation = targetRot;
        if (camTransform != null) camTransform.localRotation = targetCamRot;
        if (playerController != null) playerController.SyncRotationToCurrent();

        // 3. 強制歩行
        float distanceToWalk = stepsToWalk * 0.7f;
        Vector3 startPos = playerTransform.position;
        Vector3 targetPos = startPos + playerTransform.forward * distanceToWalk;

        while (Vector3.Distance(playerTransform.position, targetPos) > 0.1f)
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position, targetPos, walkSpeed * Time.deltaTime);
            yield return null;
        }

        // 4. 操作許可
        if (playerController != null) playerController.canControl = true;

        isEventActive = false;
    }
}