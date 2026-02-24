using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EventDoorController : MonoBehaviour
{
    [Header("ドアの設定")]
    [Tooltip("動かしたいドアオブジェクト")]
    public Transform targetDoor;

    [Tooltip("近づいた時に表示するテキスト")]
    public GameObject guideText;

    [Tooltip("ドアが開く角度（例：0, 90, 0）")]
    public Vector3 openAngle = new Vector3(0, 90, 0);

    [Tooltip("ドアの開閉スピード")]
    public float moveDuration = 1.0f;

    [Tooltip("最初は鍵がかかっているか")]
    public bool isLocked = true;

    [Header("【重要】鍵解除の条件設定")]
    [Tooltip("ここにアタッチしたオブジェクト（毒蜘蛛など）が消滅(Destroy)すると、自動で鍵が開きます")]
    public GameObject targetKeyObject;

    [Header("音の設定")]
    [Tooltip("ドアを開け閉めする時の音")]
    public AudioClip doorSound;

    [Tooltip("対象オブジェクト消滅時に鳴る「鍵が開く音」")]
    public AudioClip unlockSound;

    // 内部変数
    private bool isOpen = false;
    private bool isPlayerNearby = false;
    private bool isAnimating = false;
    private Quaternion closedRot;
    private Quaternion openRot;
    private AudioSource audioSource;

    // オブジェクトの消滅を監視するためのフラグ
    private bool isWaitingForKeyObjectDestroy = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        if (targetDoor != null)
        {
            closedRot = targetDoor.localRotation;
            openRot = Quaternion.Euler(openAngle);
        }
        if (guideText != null) guideText.SetActive(false);

        // Target Key Object が設定されている場合、消滅監視をスタートする
        if (targetKeyObject != null)
        {
            isWaitingForKeyObjectDestroy = true;
        }
    }

    void Update()
    {
        // ★鍵オブジェクトの消滅（Destroy）を監視
        if (isWaitingForKeyObjectDestroy && targetKeyObject == null)
        {
            UnlockDoor();
            isWaitingForKeyObjectDestroy = false; // 1回だけ実行するための制御
        }

        // プレイヤーによる手動操作 (Eキー)
        if (isPlayerNearby && !isAnimating && Input.GetKeyDown(KeyCode.E))
        {
            if (!isLocked)
            {
                // 鍵が開いていればドアを開閉する
                StartCoroutine(OperateDoor());
            }
            else
            {
                // 鍵がかかっている場合
                Debug.Log("ドアには鍵がかかっている！");
            }
        }
    }

    // 鍵を開ける処理
    private void UnlockDoor()
    {
        if (isLocked)
        {
            isLocked = false; // 鍵を解除！
            Debug.Log("対象のオブジェクトが消滅しました。ドアの鍵が開きました！");

            // 鍵が開く音を再生
            if (audioSource != null && unlockSound != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }
        }
    }

    // 手動でドアを開け閉めする処理
    private IEnumerator OperateDoor()
    {
        isAnimating = true; // 連続で押せないようにロック

        if (guideText != null) guideText.SetActive(false);

        Quaternion startRot = targetDoor.localRotation;
        Quaternion endRot = isOpen ? closedRot : openRot;

        // ドアの開閉音を再生
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
        isAnimating = false;

        // ドアが動き終わった後、まだプレイヤーが近くにいればテキストを再表示
        if (isPlayerNearby && guideText != null) guideText.SetActive(true);
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