using UnityEngine;
using System.Collections;

public class WakeUpController : MonoBehaviour
{
    [Header("スポーン設定")]
    [Tooltip("スポーンする座標（例：0, 2, 0）")]
    public Vector3 spawnPosition = new Vector3(0f, 2f, 0f);

    [Tooltip("ベッドで寝ている時の体の向き（Y軸回転）")]
    public float bedRotationY = 0f;

    [Header("演出設定")]
    [Tooltip("起き上がるのにかかる時間（秒）")]
    public float wakeUpDuration = 2.0f;

    [Header("割り当て")]
    [Tooltip("プレイヤーの親オブジェクト（移動させる対象）")]
    public Transform playerTransform;

    [Tooltip("プレイヤーのカメラ（回転させる対象）")]
    public Transform playerCamera;

    [Tooltip("移動を止めるためのプレイヤースクリプト（FPSControllerなど）")]
    public MonoBehaviour playerMovementScript;

    // private
    private bool isSleeping = true;
    private bool isWakingUp = false;

    void Start()
    {
        // 1. プレイヤーを強制的にスポーン位置へ移動
        if (playerTransform != null)
        {
            playerTransform.position = spawnPosition;
            // 体の向き（枕の方向など）を合わせる
            playerTransform.rotation = Quaternion.Euler(0, bedRotationY, 0);
        }

        // 2. カメラを真上に向ける（寝ている視点）
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        // 3. 起きるまでは動けないように操作スクリプトを止める
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
    }

    void Update()
    {
        // 寝ていて、まだ起き上がり中でない時に Bキー を押したら
        if (isSleeping && !isWakingUp)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                StartCoroutine(WakeUpSequence());
            }
        }
    }

    // ゆっくり起き上がるコルーチン
    IEnumerator WakeUpSequence()
    {
        isWakingUp = true;
        Debug.Log("起き上がります...");

        float elapsed = 0f;
        Quaternion startRotation = playerCamera.localRotation; // 真上（-90度）
        Quaternion endRotation = Quaternion.Euler(0f, 0f, 0f); // 正面（0度）

        while (elapsed < wakeUpDuration)
        {
            // 時間経過に合わせて回転を滑らかに変化させる
            float t = elapsed / wakeUpDuration;
            // SmoothStepを使うと動き出しと終わりが滑らかになります
            t = Mathf.SmoothStep(0f, 1f, t);

            playerCamera.localRotation = Quaternion.Slerp(startRotation, endRotation, t);

            elapsed += Time.deltaTime;
            yield return null; // 1フレーム待つ
        }

        // 念のため最後にカチッと正面に向ける
        playerCamera.localRotation = endRotation;

        // 操作を許可する
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        isSleeping = false;
        Debug.Log("おはようございます！操作可能です。");
    }
}