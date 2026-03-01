using System.Collections;
using System.Collections.Generic; // ★追加：Listを使うために必要
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ForceTurnBackWithSubtitle : MonoBehaviour
{
    // ★追加：一度でも突破した壁の名前を記憶し続けるリスト（死んでも消えない）
    public static List<string> clearedWalls = new List<string>();

    [Header("必須設定")]
    [Tooltip("チェック対象となるSearchPointViewer（手がかり）")]
    public SearchPointViewer targetSearchPoint;

    [Tooltip("【重要】物理的に通さないための壁オブジェクト（BoxColliderなど）")]
    public GameObject invisibleWall;

    [Header("通過条件")]
    [Tooltip("このタグを持つアイテムを持っていないと通れない")]
    public string requiredItemTag = "Key";

    [Header("強制移動の設定")]
    [Tooltip("強制的に歩かせる歩数")]
    public int stepsToWalk = 3;

    [Tooltip("振り向く速さ")]
    public float turnSpeed = 5.0f;

    [Tooltip("強制歩行の速さ")]
    public float walkSpeed = 3.0f;

    [Header("字幕：表示設定")]
    [Tooltip("順番に表示させたいUIのImage画像（＋ボタンで何個でも登録できます）")]
    public Image[] targetImages;

    [Tooltip("1つの画像を表示にかける時間（秒）")]
    public float duration = 2.0f;

    [Tooltip("文字数（画像を何段階で表示するか）")]
    public int characterCount = 8;

    [Header("字幕：時間・フェード設定")]
    [Tooltip("すべて表示された後、消え始めるまでの待機時間（秒）")]
    public float displayTime = 3.0f;

    [Tooltip("最後の字幕がうっすら消えていくフェードアウトの時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("前の字幕が消えてから、次の字幕が表示されるまでの間隔（秒）")]
    public float delayBetweenSubtitles = 0.5f;

    [Header("字幕：音設定")]
    [Tooltip("強制歩行が終わって、字幕が出る直前に鳴る効果音（不要なら空欄）")]
    public AudioClip subtitleSound;

    // 内部変数
    private PlayerController playerController;
    private InventoryManager inventoryManager;
    private AudioSource audioSource;
    private bool isEventActive = false;

    private bool hasShownSubtitle = false;

    void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        audioSource = GetComponent<AudioSource>();

        // ★追加：リスポーン時に「この壁はもう突破済みか？」をチェックする
        if (clearedWalls.Contains(gameObject.name))
        {
            // すでに突破済みなら、見えない壁を消して、このUターン判定自体を完全にオフにする
            if (invisibleWall != null) invisibleWall.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        if (invisibleWall != null) invisibleWall.SetActive(true);

        // 字幕画像の初期化（隠す）
        if (targetImages != null)
        {
            foreach (Image img in targetImages)
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
    }

    void Update()
    {
        // 通過条件を満たした時の処理
        if (CheckPassCondition())
        {
            // ★追加：条件を満たしたら「この壁は突破した」とリストに記憶させる
            if (!clearedWalls.Contains(gameObject.name))
            {
                clearedWalls.Add(gameObject.name);
            }

            if (invisibleWall != null) invisibleWall.SetActive(false);

            // イベント中でなければ、この判定用スクリプト自体をオフにする
            if (!isEventActive)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤー以外、またはイベント中なら無視
        if (!other.CompareTag("Player") || isEventActive) return;

        // 通過条件を満たしているなら無視
        if (CheckPassCondition()) return;

        // イベント開始！
        StartCoroutine(TurnBackAndSubtitleRoutine(other.transform));
    }

    private bool CheckPassCondition()
    {
        bool hasViewed = (targetSearchPoint != null && targetSearchPoint.hasBeenViewed);
        if (!hasViewed) return false;

        bool hasKey = HasItemWithTag(requiredItemTag);
        if (!hasKey) return false;

        return true;
    }

    private bool HasItemWithTag(string tagToCheck)
    {
        if (inventoryManager == null) return false;
        foreach (string itemName in inventoryManager.currentItems)
        {
            if (string.IsNullOrEmpty(itemName)) continue;
            string itemTag = inventoryManager.GetItemTag(itemName);
            if (itemTag == tagToCheck) return true;
        }
        return false;
    }

    // 強制Uターン ➔ 歩行 ➔ 字幕表示 を一気に行うコルーチン
    IEnumerator TurnBackAndSubtitleRoutine(Transform playerTransform)
    {
        isEventActive = true;

        // 1. 操作禁止 & 停止
        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // 2. 振り向き処理（180度Uターン ＋ 正面を向く）
        Quaternion startRot = playerTransform.rotation;
        Vector3 backwardDirection = -playerTransform.forward;
        backwardDirection.y = 0;
        Quaternion targetRot = Quaternion.LookRotation(backwardDirection);

        Transform camTransform = null;
        Quaternion startCamRot = Quaternion.identity;
        Quaternion targetCamRot = Quaternion.Euler(0, 0, 0); // 視線を水平に戻す

        if (playerController != null && playerController.cam != null)
        {
            camTransform = playerController.cam.transform;
            startCamRot = camTransform.localRotation;
        }

        float t = 0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime * turnSpeed;
            playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            if (camTransform != null) camTransform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
            if (playerController != null) playerController.SyncRotationToCurrent();
            yield return null;
        }

        playerTransform.rotation = targetRot;
        if (camTransform != null) camTransform.localRotation = targetCamRot;
        if (playerController != null) playerController.SyncRotationToCurrent();

        // 3. 強制歩行
        float distanceToWalk = stepsToWalk * 0.7f;
        Vector3 startPos = playerTransform.position;
        Vector3 targetPos = startPos + playerTransform.forward * distanceToWalk;

        while (Vector3.Distance(playerTransform.position, targetPos) > 0.1f)
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position, targetPos, walkSpeed * Time.deltaTime);
            yield return null;
        }

        // まだ字幕を表示していない（初回）の時だけ、音と字幕を出す
        if (!hasShownSubtitle)
        {
            // 4. 音を鳴らして待機
            if (subtitleSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(subtitleSound);
                yield return new WaitForSeconds(subtitleSound.length);
            }

            // 5. 字幕のシーケンス開始
            if (targetImages != null && targetImages.Length > 0)
            {
                for (int i = 0; i < targetImages.Length; i++)
                {
                    Image currentImage = targetImages[i];
                    if (currentImage == null) continue;

                    currentImage.gameObject.SetActive(true);
                    currentImage.fillAmount = 0f;
                    SetAlpha(currentImage, 1f);

                    float timer = 0f;

                    // タイプライター表示
                    while (timer < duration)
                    {
                        timer += Time.deltaTime;
                        float progress = timer / duration;

                        if (characterCount > 0)
                        {
                            float steppedProgress = Mathf.Floor(progress * characterCount) / characterCount;
                            currentImage.fillAmount = steppedProgress;
                        }
                        else
                        {
                            currentImage.fillAmount = progress;
                        }
                        yield return null;
                    }

                    currentImage.fillAmount = 1.0f;

                    // 待機
                    yield return new WaitForSeconds(displayTime);

                    // 最後の字幕だけフェードアウト
                    if (i == targetImages.Length - 1)
                    {
                        timer = 0f;
                        while (timer < fadeDuration)
                        {
                            timer += Time.deltaTime;
                            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                            SetAlpha(currentImage, alpha);
                            yield return null;
                        }
                    }

                    SetAlpha(currentImage, 0f);
                    currentImage.gameObject.SetActive(false);

                    if (i < targetImages.Length - 1)
                    {
                        yield return new WaitForSeconds(delayBetweenSubtitles);
                    }
                }
            }

            // 初回が終わったのでフラグを立てて、次からは字幕が出ないようにする
            hasShownSubtitle = true;
        }

        // 6. 全て終わったら操作許可
        if (playerController != null) playerController.canControl = true;
        isEventActive = false;
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