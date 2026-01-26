using UnityEngine;
using System.Collections;

public class LockDoorController : MonoBehaviour
{
    [Header("ロック設定")]
    [Tooltip("チェックを入れると最初は鍵がかかった状態になります")]
    public bool isLocked = true;

    [Header("ドアのペア設定")]
    public Transform door1; // 左のドア
    public Transform door2; // 右のドア

    [Header("UI設定")]
    [Tooltip("開けられる時に出る文字")]
    public GameObject guideText;
    [Tooltip("鍵がかかっている時に出る文字")]
    public GameObject lockedText;

    [Header("角度の設定")]
    public Vector3 door1OpenAngle = new Vector3(0, 90, 0);
    public Vector3 door2OpenAngle = new Vector3(0, -90, 0);
    public float moveDuration = 1.0f;

    [Header("音の設定")]
    public AudioClip doorSound;   // 開く音
    public AudioClip lockedSound; // 鍵がかかっている音（ガチャガチャ）

    // 内部変数
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
        // 初期の角度を記憶
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

        if (guideText != null) guideText.SetActive(false);
        if (lockedText != null) lockedText.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // プレイヤーが近くにいて、Eキーを押したら
        if (isPlayerInside && !isAnimating && Input.GetKeyDown(KeyCode.E))
        {
            if (isLocked)
            {
                // ロックされているなら「ガチャガチャ」
                PlayLockedEffect();
            }
            else
            {
                // ロック解除済みならドアを開閉
                StartCoroutine(OperateDoors());
            }
        }
    }

    // ★重要：パスワード画面からこの関数が呼ばれて鍵が開く
    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("ロック解除！");

        // 鍵がかかってる表示を消して、開けられる表示を出す
        if (isPlayerInside)
        {
            if (lockedText != null) lockedText.SetActive(false);
            if (guideText != null) guideText.SetActive(true);
        }
    }

    void PlayLockedEffect()
    {
        if (audioSource != null && lockedSound != null)
        {
            audioSource.PlayOneShot(lockedSound);
        }
        // ここに「鍵がかかっている」というメッセージを一瞬出す処理を入れてもOK
    }

    IEnumerator OperateDoors()
    {
        isAnimating = true;
        if (guideText != null) guideText.SetActive(false);

        Quaternion d1Start = door1.localRotation;
        Quaternion d2Start = door2.localRotation;
        Quaternion d1End = isOpen ? door1ClosedRot : door1OpenRot;
        Quaternion d2End = isOpen ? door2ClosedRot : door2OpenRot;

        if (audioSource != null && doorSound != null)
        {
            audioSource.clip = doorSound;
            audioSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);

            if (door1 != null) door1.localRotation = Quaternion.Slerp(d1Start, d1End, t);
            if (door2 != null) door2.localRotation = Quaternion.Slerp(d2Start, d2End, t);
            yield return null;
        }

        if (door1 != null) door1.localRotation = d1End;
        if (door2 != null) door2.localRotation = d2End;

        isOpen = !isOpen;
        isAnimating = false;

        if (isPlayerInside && guideText != null) guideText.SetActive(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (isLocked)
            {
                if (lockedText != null) lockedText.SetActive(true);
            }
            else
            {
                if (guideText != null) guideText.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (guideText != null) guideText.SetActive(false);
            if (lockedText != null) lockedText.SetActive(false);
        }
    }
}