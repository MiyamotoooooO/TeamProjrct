using UnityEngine;
using System.Collections;
using UnityEngine.UI;

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

    [Header("鍵で開く設定")]
    [Tooltip("ドアを開けるための鍵名（空欄なら鍵なしで開きます）")]
    public string requiredKeyName = "";

    [Header("字幕：鍵を持っていない時")]
    [Tooltip("何も持っていない、または鍵以外のアイテムを持っている時に表示する画像")]
    public Image[] noKeySubtitleImages;

    [Header("字幕：違う鍵を持っている時")]
    [Tooltip("鍵は持っているが、このドアの鍵ではない時に表示する画像")]
    public Image[] wrongKeySubtitleImages;

    [Header("字幕：アニメーション設定")]
    public float textDuration = 2.0f;
    public int characterCount = 8;
    public float displayTime = 2.0f;
    public float fadeDuration = 1.0f;

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

        // 字幕用画像を初期化（隠す）
        InitializeSubtitleImages(noKeySubtitleImages);
        InitializeSubtitleImages(wrongKeySubtitleImages);
    }

    private void InitializeSubtitleImages(Image[] images)
    {
        if (images == null) return;
        foreach (Image img in images)
        {
            if (img != null)
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
                img.fillAmount = 0f;
                img.gameObject.SetActive(false);
                SetAlpha(img, 1f);
            }
        }
    }

    void Update()
    {
        // ★ 他の字幕が再生中なら、UIを隠して入力を受け付けない
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
                TryOpenDoor();
            }
        }
    }

    // Eキーを押した時の判定処理
    private void TryOpenDoor()
    {
        // 鍵が必要ないドアならそのまま開ける
        if (string.IsNullOrEmpty(requiredKeyName))
        {
            StartCoroutine(ForceOpenRoutine());
            return;
        }

        // プレイヤーが現在「手に持っている（装備している）」アイテムを取得
        string equippedItem = player.inventoryManager.GetEquippedItem();
        string equippedItemTag = "";

        if (!string.IsNullOrEmpty(equippedItem))
        {
            equippedItemTag = player.inventoryManager.GetItemTag(equippedItem);
        }

        // ① 正解の鍵を持っている場合
        if (equippedItem == requiredKeyName)
        {
            StartCoroutine(ForceOpenRoutine());
        }
        // ② 手に持っているアイテムのTagが「Key」だけど、名前が違う場合（別の鍵）
        else if (equippedItemTag == "Key")
        {
            StartCoroutine(ShowSubtitleRoutine(wrongKeySubtitleImages));
        }
        // ③ 何も持っていない、または鍵以外のアイテムを持っている場合
        else
        {
            StartCoroutine(ShowSubtitleRoutine(noKeySubtitleImages));
        }
    }

    // 字幕を表示して時間を止めるコルーチン
    private IEnumerator ShowSubtitleRoutine(Image[] targetImages)
    {
        if (targetImages == null || targetImages.Length == 0) yield break;

        isAnimating = true; // 連打防止
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // ★ グローバルロックON

        if (guideText != null) guideText.SetActive(false); // Eキー案内を消す

        // ★時間を止める！（敵もプレイヤーも動けなくなる）
        Time.timeScale = 0f;
        if (player != null) player.canControl = false;

        for (int i = 0; i < targetImages.Length; i++)
        {
            Image currentImage = targetImages[i];
            if (currentImage == null) continue;

            currentImage.gameObject.SetActive(true);
            currentImage.fillAmount = 0f;
            SetAlpha(currentImage, 1f);

            float timer = 0f;

            // 時間が止まっているので Time.unscaledDeltaTime を使う
            while (timer < textDuration)
            {
                timer += Time.unscaledDeltaTime;
                float progress = timer / textDuration;

                if (characterCount > 0)
                {
                    currentImage.fillAmount = Mathf.Floor(progress * characterCount) / characterCount;
                }
                else
                {
                    currentImage.fillAmount = progress;
                }
                yield return null;
            }

            currentImage.fillAmount = 1.0f;

            // 待機（リアルタイム）
            yield return new WaitForSecondsRealtime(displayTime);

            // 最後の字幕だけフェードアウト
            if (i == targetImages.Length - 1)
            {
                timer = 0f;
                while (timer < fadeDuration)
                {
                    timer += Time.unscaledDeltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                    SetAlpha(currentImage, alpha);
                    yield return null;
                }
            }

            SetAlpha(currentImage, 0f);
            currentImage.gameObject.SetActive(false);

            if (i < targetImages.Length - 1)
            {
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        // ★時間が動き出す！
        Time.timeScale = 1f;
        if (player != null) player.canControl = true;

        isAnimating = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false; // ★ グローバルロックOFF

        // まだドアの前にいたらEキー案内を再表示
        if (isPlayerNearby && guideText != null) guideText.SetActive(true);
    }

    // 鍵が合っていてドアを開ける時の処理
    private IEnumerator ForceOpenRoutine()
    {
        isAnimating = true; // ロック
        if (guideText != null) guideText.SetActive(false);

        // 攻撃(アクション)モーションを再生
        if (player != null)
        {
            player.HandleAttackInput();

            // モーションがドアに当たるまで少し待つ
            yield return new WaitForSeconds(0.9f);

            // 鍵が必要なドアだった場合は、鍵をインベントリから消してUI更新
            if (!string.IsNullOrEmpty(requiredKeyName))
            {
                player.inventoryManager.RemoveItem(requiredKeyName);
                player.inventoryManager.UpdateInventoryUI();
                Debug.Log("鍵を使用してドアを開けました");
            }
        }

        // ドアを開くアニメーションへ移行
        StartCoroutine(OperateDoor());
    }

    private IEnumerator OperateDoor()
    {
        Debug.Log("Door Start");

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

            if (targetDoor != null)
            {
                targetDoor.localRotation = Quaternion.Slerp(startRot, endRot, t);
            }

            yield return null;
        }

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

    private void SetAlpha(Image img, float alpha)
    {
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}