using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SingleDoorController : MonoBehaviour
{
    [Header("ドアの設定")]
    [Tooltip("動かしたいドアオブジェクト")]
    public Transform targetDoor;

    [Header("UI設定")]
    [Tooltip("近づいた時に表示するテキスト（DoorGuideTextなど）")]
    public GameObject guideText;

    [Header("角度の設定")]
    [Tooltip("ドアが開く角度（例：0, 90, 0）")]
    public Vector3 openAngle = new Vector3(0, 90, 0);

    [Header("ドアの開閉スピード")]
    public float moveDuration = 1.0f;

    [Header("鍵の設定")]
    [Tooltip("trueにすると、スクリプトから自動で開くなどの制御に使えます")]
    public bool isLocked = false;

    [Header("音の設定")]
    [Tooltip("ドアの開閉音")]
    public AudioClip doorSound;

    // 内部変数
    private bool isOpen = false;
    private bool isPlayerNearby = false;
    private bool isAnimating = false;
    private Quaternion closedRot;
    private Quaternion openRot;
    private AudioSource audioSource;

    void Start()
    {
        // AudioSourceの取得
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        // 最初の角度（閉じてる状態）を記憶
        if (targetDoor != null)
        {
            closedRot = targetDoor.localRotation;
            openRot = Quaternion.Euler(openAngle);
        }

        // 最初はテキストを隠す
        if (guideText != null) guideText.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNearby && !isAnimating && Input.GetKeyDown(KeyCode.E))
        {
            if (!isLocked)
            {
                StartCoroutine(OperateDoor());
            }
            else
            {
                Debug.Log("ドアはロックされています");
                // ここに「鍵がかかっている」等のメッセージを出しても良い
            }
        }
    }

    private IEnumerator OperateDoor()
    {
        isAnimating = true; // 操作ロック開始

        // 動いている間はガイド文字を消す
        if (guideText != null) guideText.SetActive(false);

        // 次の状態（開くなら開く角度、閉じるなら閉じる角度）を決める
        Quaternion startRot = targetDoor.localRotation;
        Quaternion endRot = isOpen ? closedRot : openRot;

        // 音を鳴らす
        if (audioSource != null && doorSound != null)
        {
            audioSource.clip = doorSound;
            audioSource.Play();
        }

        // 時間をかけて回転させるループ
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;

            // 滑らかにする（イージング）
            t = Mathf.SmoothStep(0f, 1f, t);

            if (targetDoor != null)
            {
                targetDoor.localRotation = Quaternion.Slerp(startRot, endRot, t);
            }

            yield return null; // 1フレーム待つ
        }

        // 最後にピッタリ合わせる
        if (targetDoor != null) targetDoor.localRotation = endRot;

        // 音停止
        if (audioSource != null) audioSource.Stop(); 

        isOpen = !isOpen; // 状態を反転
        isAnimating = false; // 操作ロック解除

        // 動き終わった時に、まだプレイヤーが近くにいたら文字を再表示
        if (isPlayerNearby && guideText != null)
        {
            guideText.SetActive(true);
        }
    }

    // 近づいた時
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (guideText != null) guideText.SetActive(true);
        }
    }

    // 離れた時
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (guideText != null) guideText.SetActive(false);
        }
    }

    // 外部から強制的に開ける関数（イベント等で使う用）
    public void ForceOpen()
    {
        if (!isAnimating && !isOpen)
        {
            // ロックされていても強制解除して開く
            isLocked = false;
            StartCoroutine(OperateDoor());
        }
    }

    // 外部からロックを解除する関数
    public void Unlock()
    {
        isLocked = false;
    }
}