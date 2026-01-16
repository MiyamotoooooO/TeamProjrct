using UnityEngine;
using System.Collections;

public class DoubleDoorController : MonoBehaviour
{
    [Header("ドアのペア設定")]
    [Tooltip("1つ目のドア（左など）")]
    public Transform door1;
    [Tooltip("2つ目のドア（右など）")]
    public Transform door2;

    [Header("UI設定（新機能）")]
    [Tooltip("近づいた時に表示するテキスト（DoorGuideText）")]
    public GameObject guideText;

    [Header("角度の設定")]
    [Tooltip("ドア1が開く角度（例：0, 90, 0）")]
    public Vector3 door1OpenAngle = new Vector3(0, 90, 0);
    [Tooltip("ドア2が開く角度（例：0, -90, 0）")]
    public Vector3 door2OpenAngle = new Vector3(0, -90, 0);
    [Header("ドアの開閉スピード")]
    public float moveDuration = 1.0f;

    [Header("音の設定")]
    [Tooltip("ドアの開閉音（ここに音源を入れる）")]
    public AudioClip doorSound;

    // private
    private bool isOpen = false;
    private bool isPlayerInside = false;
    private bool isAnimating = false;
    private Quaternion door1ClosedRot;
    private Quaternion door2ClosedRot;
    private Quaternion door1OpenRot;
    private Quaternion door2OpenRot;
    private AudioSource audioSource;

    void Start()
    {
        // 最初の角度（閉じてる状態）を記憶
        if (door1 != null)
        {
            door1ClosedRot = door1.localRotation;
            door1OpenRot = Quaternion.Euler(door1OpenAngle);
        }
        if (door2 != null)
        {
            door2ClosedRot = door2.localRotation;
            door2OpenRot = Quaternion.Euler(door2OpenAngle);
        }

        // 最初はテキストを隠す
        if (guideText != null) guideText.SetActive(false);

        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false; // 勝手に鳴らないように
            audioSource.loop = false;        // ループしないように
        }
    }

    void Update()
    {
        // プレイヤーが近くにいて、Eキーを押したら
        if (isPlayerInside && !isAnimating && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(OperateDoors());
        }
    }

    IEnumerator OperateDoors()
    {
        isAnimating = true; // 操作ロック開始

        // 動いている間は文字を消す！
        if (guideText != null) guideText.SetActive(false);

        // 次の状態（開くなら開く角度、閉じるなら閉じる角度）を決める
        Quaternion d1Start = door1.localRotation;
        Quaternion d2Start = door2.localRotation;
        Quaternion d1End = isOpen ? door1ClosedRot : door1OpenRot;
        Quaternion d2End = isOpen ? door2ClosedRot : door2OpenRot;

        // 音を鳴らす
        if (audioSource != null && doorSound != null)
        {
            audioSource.clip = doorSound; // 音をセット
            audioSource.Play();           // 再生！
        }

        // 時間をかけて回転させるループ
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;

            // 滑らかにする（イージング）
            t = Mathf.SmoothStep(0f, 1f, t);

            if (door1 != null) door1.localRotation = Quaternion.Slerp(d1Start, d1End, t);
            if (door2 != null) door2.localRotation = Quaternion.Slerp(d2Start, d2End, t);

            yield return null; // 1フレーム待つ
        }

        // 念のため最後にピッタリ合わせる
        if (door1 != null) door1.localRotation = d1End;
        if (door2 != null) door2.localRotation = d2End;

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        isOpen = !isOpen; // 状態を反転
        isAnimating = false; // 操作ロック解除
        // 動き終わった時に、まだプレイヤーが目の前にいたら文字を再表示
        if (isPlayerInside && guideText != null)
        {
            guideText.SetActive(true);
        }
    }

    // 近づいた時
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("プレイヤーがドアに触れました");
            isPlayerInside = true;
            if (guideText != null) guideText.SetActive(true); // 文字を表示！
        }
    }

    // 離れた時
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (guideText != null) guideText.SetActive(false); // 文字を消す！
        }
    }
}