using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DoubleDoorController : MonoBehaviour
{
    [Header("ドアのペア設定")]
    [Tooltip("1つ目のドア（左など）")]
    public Transform door1;
    [Tooltip("2つ目のドア（右など）")]
    public Transform door2;

    [Header("UI設定")]
    [Tooltip("近づいた時に表示するテキスト（DoorGuideText）")]
    public GameObject guideText;

    [Header("角度の設定")]
    [Tooltip("ドア1が開く角度（例：0, 90, 0）")]
    public Vector3 door1OpenAngle = new Vector3(0, 90, 0);
    [Tooltip("ドア2が開く角度（例：0, -90, 0）")]
    public Vector3 door2OpenAngle = new Vector3(0, -90, 0);
    [Header("ドアの開閉スピード")]
    public float moveDuration = 1.0f;

    [Header("鍵で開く設定")]
    public bool doubleDoorPair1 = false;
    [Header("ドアを開けるための鍵名")]
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
    [Tooltip("ドアの開閉音（ここに音源を入れる）")]
    public AudioClip doorSound;

    public PlayerController player;

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
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

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
        // プレイヤーが近くにいて、Eキーを押したら
        if (isPlayerInside && !isAnimating && !isOpen && Input.GetKeyDown(KeyCode.E))
        {
            TryOpenDoor();
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
        if (guideText != null) guideText.SetActive(false); // Eキー案内を消す

        // ★ここで時間を止める！（敵もプレイヤーも動けなくなる）
        Time.timeScale = 0f;
        player.canControl = false;

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
        player.canControl = true;
        isAnimating = false;

        // まだドアの前にいたらEキー案内を再表示
        if (isPlayerInside && guideText != null) guideText.SetActive(true);
    }

    // 鍵が合っていてドアを開ける時の処理
    private IEnumerator ForceOpenRoutine()
    {
        isAnimating = true; // ロック
        if (guideText != null) guideText.SetActive(false);

        // 攻撃(アクション)モーションを再生
        player.HandleAttackInput();

        // モーションがドアに当たるまで少し待つ
        yield return new WaitForSeconds(0.9f);

        // 鍵をインベントリから消してUI更新
        player.inventoryManager.RemoveItem(requiredKeyName);
        player.inventoryManager.UpdateInventoryUI();
        Debug.Log("鍵を使用してドアを開けました");

        // ドアを開くアニメーションへ移行
        StartCoroutine(OperateDoors());
    }

    private IEnumerator OperateDoors()
    {
        Debug.Log("Door Start");

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
            float t = elapsed / moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            if (door1 != null) door1.localRotation = Quaternion.Slerp(d1Start, d1End, t);
            if (door2 != null) door2.localRotation = Quaternion.Slerp(d2Start, d2End, t);

            yield return null;
        }

        if (door1 != null) door1.localRotation = d1End;
        if (door2 != null) door2.localRotation = d2End;

        if (audioSource != null) audioSource.Stop();

        isOpen = true; // ドアが開いた状態にする
        isAnimating = false; // 操作ロック解除
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (!isOpen && guideText != null) guideText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
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





/*using UnityEngine;
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
}*/
