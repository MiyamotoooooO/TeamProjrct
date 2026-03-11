using UnityEngine;
using System.Collections;

public class LockDoorController : MonoBehaviour
{
    [Header("鍵がかかっているかの確認")]
    public bool isLocked = true;

    [Header("ドアの設定")]
    [Tooltip("動かしたい1枚のドアを入れます")]
    public Transform targetDoor;

    [Header("ドアを開けられる時に出る案内文字")]
    public GameObject guideText;
    [Header("鍵がかかっている時に出る案内文字")]
    public GameObject lockedText;

    [Header("ドアの目標角度設定")]
    [Tooltip("ドアが開く角度（例：0, 90, 0 または 0, -90, 0）")]
    public Vector3 openAngle = new Vector3(0, 90, 0);

    [Header("ドアの開閉スピード")]
    public float moveDuration = 1.0f;

    [Header("ドアの開閉音")]
    public AudioClip doorSound;

    [Header("ドアの鍵がかかっている音")]
    public AudioClip lockedSound;

    // 内部変数
    private bool isOpen = false; // 今ドアは開いているかのフラグ
    private bool isPlayerInside = false; // Playerはドアの近くにいるかのフラグ
    private bool isAnimating = false; // 今ドアは動いている最中かのフラグ
    private Quaternion closedRot; // 閉まっているときの角度
    private Quaternion openRot; // 開いたときの目標角度
    private AudioSource audioSource; // 音を鳴らすスピーカー

    void Start()
    {
        // 初期の角度を記憶
        if (targetDoor != null)
        {
            closedRot = targetDoor.localRotation;
            openRot = Quaternion.Euler(openAngle);
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
                StartCoroutine(OperateDoor());
            }
        }
    }

    // パスワード画面からこの関数が呼ばれて鍵が開く
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
    }

    IEnumerator OperateDoor()
    {
        isAnimating = true;
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
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);

            if (targetDoor != null) targetDoor.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        if (targetDoor != null) targetDoor.localRotation = endRot;

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