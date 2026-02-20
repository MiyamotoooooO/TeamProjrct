using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EventDoorController : MonoBehaviour
{
    [Header("ドアの設定")]
    public Transform targetDoor;
    public GameObject guideText;
    public Vector3 openAngle = new Vector3(0, 90, 0);
    public float moveDuration = 1.0f;

    [Tooltip("trueにすると、プレイヤーの手動操作(Eキー)を無効化します")]
    public bool isLocked = true; // イベントで開けるため最初はロック推奨
    public AudioClip doorSound;

    [Header("--- イベント演出設定 ---")]
    [Tooltip("カメラがズームする先のターゲット（ドアノブなど。空欄ならこのオブジェクトの場所）")]
    public Transform cameraTarget;

    [Tooltip("ズーム時の視野角（通常は60。小さいほどズームします）")]
    public float zoomFOV = 30f;

    [Tooltip("カメラがズームする時間（秒）")]
    public float cameraMoveDuration = 1.0f;

    [Tooltip("ドアが開いた後、元の視点に戻るまでの待機時間（秒）")]
    public float postOpenWaitTime = 1.0f;

    // 内部変数
    private bool isOpen = false;
    private bool isPlayerNearby = false;
    private bool isAnimating = false;
    private Quaternion closedRot;
    private Quaternion openRot;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) { audioSource.playOnAwake = false; audioSource.loop = false; }

        if (targetDoor != null)
        {
            closedRot = targetDoor.localRotation;
            openRot = Quaternion.Euler(openAngle);
        }
        if (guideText != null) guideText.SetActive(false);
    }

    void Update()
    {
        // プレイヤーによる手動操作
        if (isPlayerNearby && !isAnimating && Input.GetKeyDown(KeyCode.E))
        {
            if (!isLocked) StartCoroutine(OperateDoor());
            else Debug.Log("ドアはロックされています（イベントで開きます）");
        }
    }

    // ★外部（毒蜘蛛）から呼ばれるイベント専用関数
    public void TriggerOpenEvent()
    {
        if (!isOpen && !isAnimating)
        {
            StartCoroutine(EventSequence());
        }
    }

    // カメラ演出付きのオープン処理
    private IEnumerator EventSequence()
    {
        isAnimating = true;

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null || player.cam == null)
        {
            // プレイヤーが見つからなければカメラ演出なしで普通に開ける
            yield return StartCoroutine(OperateDoorInternal());
            isAnimating = false;
            yield break;
        }

        Camera pCam = player.cam.GetComponent<Camera>();

        // 1. プレイヤーの操作をロック
        player.canControl = false;
        if (guideText != null) guideText.SetActive(false);

        // 2. カメラと体の元の状態を保存
        Quaternion startPlayerRot = player.transform.rotation;
        Quaternion startCamRot = pCam.transform.localRotation;
        float startFOV = pCam.fieldOfView;

        // ターゲット位置を決定
        Transform target = cameraTarget != null ? cameraTarget : transform;
        Vector3 dirToTarget = (target.position - pCam.transform.position).normalized;

        // プレイヤーの体（Y軸）とカメラ（X軸）の目標角度を計算
        Vector3 flatDir = new Vector3(dirToTarget.x, 0, dirToTarget.z);
        Quaternion targetPlayerRot = Quaternion.LookRotation(flatDir);
        Vector3 localDir = Quaternion.Inverse(targetPlayerRot) * dirToTarget;
        Quaternion targetCamRot = Quaternion.LookRotation(localDir);

        // 3. ドアを向いてズームする
        float t = 0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime / cameraMoveDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            player.transform.rotation = Quaternion.Slerp(startPlayerRot, targetPlayerRot, easedT);
            pCam.transform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, easedT);
            pCam.fieldOfView = Mathf.Lerp(startFOV, zoomFOV, easedT);

            player.SyncRotationToCurrent(); // PlayerControllerの視点を同期
            yield return null;
        }

        // 4. 少し待ってからドアを開ける
        yield return new WaitForSeconds(0.5f);
        isLocked = false; // イベントでロック解除
        yield return StartCoroutine(OperateDoorInternal()); // 開閉処理を実行

        // 5. ドアが開いたら少し待つ
        yield return new WaitForSeconds(postOpenWaitTime);

        // 6. 元の視点・FOVに戻る
        t = 0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime / cameraMoveDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            player.transform.rotation = Quaternion.Slerp(targetPlayerRot, startPlayerRot, easedT);
            pCam.transform.localRotation = Quaternion.Slerp(targetCamRot, startCamRot, easedT);
            pCam.fieldOfView = Mathf.Lerp(zoomFOV, startFOV, easedT);

            player.SyncRotationToCurrent();
            yield return null;
        }

        // 完全に元に戻す
        player.transform.rotation = startPlayerRot;
        pCam.transform.localRotation = startCamRot;
        pCam.fieldOfView = startFOV;
        player.SyncRotationToCurrent();

        // 7. 操作再開
        player.canControl = true;
        isAnimating = false;

        if (isPlayerNearby && guideText != null) guideText.SetActive(true);
    }

    private IEnumerator OperateDoor()
    {
        isAnimating = true;
        yield return StartCoroutine(OperateDoorInternal());
        isAnimating = false;
    }

    private IEnumerator OperateDoorInternal()
    {
        if (guideText != null) guideText.SetActive(false);

        Quaternion startRot = targetDoor.localRotation;
        Quaternion endRot = isOpen ? closedRot : openRot;

        if (audioSource != null && doorSound != null)
        {
            audioSource.clip = doorSound;
            audioSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            if (targetDoor != null) targetDoor.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        if (targetDoor != null) targetDoor.localRotation = endRot;
        if (audioSource != null) audioSource.Stop();
        isOpen = !isOpen;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (!isAnimating && guideText != null) guideText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (guideText != null) guideText.SetActive(false);
        }
    }
}