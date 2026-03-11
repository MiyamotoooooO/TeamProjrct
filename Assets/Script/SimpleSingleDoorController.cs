using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class SimpleSingleDoorController : MonoBehaviour
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

    [Header("音の設定")]
    [Tooltip("ドアの開閉音")]
    public AudioClip doorSound;

    [Header("参照設定")]
    public PlayerController player;

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
        // 他の字幕が再生中なら、UIを隠して入力を受け付けない
        if (GlobalSubtitleState.IsAnySubtitlePlaying && !isAnimating)
        {
            if (guideText != null) guideText.SetActive(false);
            return;
        }

        // プレイヤーが近くにいて、Eキーを押したら
        if (isPlayerNearby && !isAnimating && !isOpen && !GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            // 案内UIの再表示（他の字幕が終わった時用）
            if (guideText != null && !guideText.activeSelf)
            {
                guideText.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(ForceOpenRoutine());
            }
        }
    }

    // ドアを開ける際のアクション処理
    private IEnumerator ForceOpenRoutine()
    {
        isAnimating = true; // ロック
        if (guideText != null) guideText.SetActive(false);

        // プレイヤーのモーションを再生
        if (player != null)
        {
            player.HandleAttackInput();

            // ★変更：ここにあった「0.9秒待つ」処理を削除しました！
            // 手を動かすと同時にすぐ下の「ドアを開く処理」へ進みます。
        }

        // ドアを開くアニメーションへ即座に移行
        yield return StartCoroutine(OperateDoor());
    }

    // 実際にドアを回転させる処理
    private IEnumerator OperateDoor()
    {
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

            // 滑らかな動きにする（イージング）
            t = Mathf.SmoothStep(0f, 1f, t);

            if (targetDoor != null)
            {
                targetDoor.localRotation = Quaternion.Slerp(startRot, endRot, t);
            }

            yield return null;
        }

        // 最後にピッタリ合わせる
        if (targetDoor != null) targetDoor.localRotation = endRot;

        if (audioSource != null) audioSource.Stop();

        isOpen = true; // 一度開いたら開いた状態にする
        isAnimating = false; // 操作ロック解除
    }

    // 近づいた時
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;

            // 既に開いておらず、かつ字幕表示中でなければUIを出す
            if (!isOpen && guideText != null && !GlobalSubtitleState.IsAnySubtitlePlaying)
            {
                guideText.SetActive(true);
            }
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
}