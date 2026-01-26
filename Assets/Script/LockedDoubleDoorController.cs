using UnityEngine;
using System.Collections;

public class LockedDoubleDoorController : MonoBehaviour
{
    [Header("鍵の設定")]
    [Tooltip("このレイヤーがついたオブジェクトを持っていると開きます")]
    public LayerMask keyLayer;

    [Tooltip("鍵を使ってドアを開けた時、その鍵を消滅させるか？")]
    public bool destroyKeyOnUse = true; // ★追加：使い捨てにするか設定可能に

    [Tooltip("鍵を持っていない時の音（ガチャガチャ...）")]
    public AudioClip lockedSound;

    [Tooltip("鍵がかかっている時に表示するテキスト")]
    public GameObject lockedMessage;

    [Header("ドアのペア設定")]
    public Transform door1;
    public Transform door2;

    [Header("UI設定")]
    public GameObject guideText;

    [Header("角度の設定")]
    public Vector3 door1OpenAngle = new Vector3(0, 90, 0);
    public Vector3 door2OpenAngle = new Vector3(0, -90, 0);
    public float moveDuration = 1.0f;

    [Header("音の設定")]
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
    private GameObject playerObject;

    void Start()
    {
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
        if (lockedMessage != null) lockedMessage.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // プレイヤーが近くにいて、動いていなくて、Eキーを押したら
        if (isPlayerInside && !isAnimating && Input.GetKeyDown(KeyCode.E))
        {
            if (isOpen)
            {
                // 開いている時は閉じるだけ
                StartCoroutine(OperateDoors());
            }
            else
            {
                // 閉まっている時は鍵を探す
                GameObject foundKey = FindKeyObject();

                if (foundKey != null)
                {
                    // ★鍵が見つかった！ -> 削除処理
                    if (destroyKeyOnUse)
                    {
                        RemoveKey(foundKey);
                    }

                    // ドアを開ける
                    StartCoroutine(OperateDoors());
                }
                else
                {
                    // 鍵がない -> ガチャガチャ
                    StartCoroutine(PlayLockedEffect());
                }
            }
        }
    }

    // ★修正：持っている鍵オブジェクトそのものを探して返す関数
    GameObject FindKeyObject()
    {
        if (playerObject == null) return null;

        // プレイヤーの子オブジェクト（手持ちアイテム）から指定レイヤーを探す
        Transform[] allChildren = playerObject.GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            if (((1 << child.gameObject.layer) & keyLayer) != 0)
            {
                Debug.Log("鍵を発見: " + child.name);
                return child.gameObject;
            }
        }
        return null;
    }

    // ★追加：鍵を削除する関数
    void RemoveKey(GameObject keyObj)
    {
        // 1. 手持ち（シーン上の実体）を削除
        Debug.Log("鍵を消費しました: " + keyObj.name);

        // ※もしInventoryManagerを使っていて、データ削除も必要ならここで呼ぶ
        // 例: InventoryManager.Instance.RemoveItem(keyObj.name);

        Destroy(keyObj);
    }

    IEnumerator PlayLockedEffect()
    {
        if (audioSource != null && lockedSound != null)
        {
            audioSource.PlayOneShot(lockedSound);
        }

        if (guideText != null) guideText.SetActive(false);
        if (lockedMessage != null) lockedMessage.SetActive(true);

        isAnimating = true;
        Quaternion d1Original = door1.localRotation;
        Quaternion d2Original = door2.localRotation;

        float shakeTime = 0.3f;
        float elapsed = 0f;

        while (elapsed < shakeTime)
        {
            float shakeZ = Mathf.Sin(elapsed * 50f) * 1.5f;
            if (door1 != null) door1.localRotation = d1Original * Quaternion.Euler(0, 0, shakeZ);
            if (door2 != null) door2.localRotation = d2Original * Quaternion.Euler(0, 0, -shakeZ);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (door1 != null) door1.localRotation = d1Original;
        if (door2 != null) door2.localRotation = d2Original;

        if (lockedMessage != null) lockedMessage.SetActive(false);
        if (guideText != null) guideText.SetActive(true);

        isAnimating = false;
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
            playerObject = other.gameObject;
            if (guideText != null) guideText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            playerObject = null;
            if (guideText != null) guideText.SetActive(false);
            if (lockedMessage != null) lockedMessage.SetActive(false);
        }
    }
}