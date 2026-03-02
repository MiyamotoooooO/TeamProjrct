using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ★ 追加：すべての字幕システムで「現在字幕が再生中か」を共有するためのクラス
public static class GlobalSubtitleState
{
    public static bool IsAnySubtitlePlaying = false;
}

// ====================================================
// 【1】何もないところ用データ（棚移動などのオプション無し）
// ====================================================
[System.Serializable]
public class NormalInteractData
{
    [Tooltip("【重要】リスポーンしても「終わった事」を記憶させるための名前。記憶させたい場合は必ず何か入力してください！")]
    public string eventID;

    [Tooltip("このエリア（BoxCollider等のTrigger）を設定します")]
    public Collider triggerArea;

    [Tooltip("この場所でEキーを押したときに順番に表示させる画像（＋ボタンで追加）")]
    public Image[] subtitleImages;

    [Tooltip("一度見たら、二度と調べられないようにするか（イベント用などにチェック）")]
    public bool playOnlyOnce = false;

    [HideInInspector]
    public bool hasPlayed = false; // 再生済みかどうかの内部フラグ
}

// ====================================================
// 【2】イベント発生地用データ（棚移動などのオプション有り）
// ====================================================
[System.Serializable]
public class SpecialInteractData
{
    [Tooltip("【重要】リスポーンしても「終わった事」を記憶させるための名前。記憶させたい場合は必ず何か入力してください！")]
    public string eventID;

    [Tooltip("このエリア（BoxCollider等のTrigger）を設定します")]
    public Collider triggerArea;

    [Tooltip("この場所でEキーを押したときに順番に表示させる画像（＋ボタンで追加）")]
    public Image[] subtitleImages;

    [Tooltip("一度見たら、二度と調べられないようにするか（イベント用などにチェック）")]
    public bool playOnlyOnce = false;

    [Header("【オプション】字幕終了後の移動演出")]
    [Tooltip("字幕が終わった後に動かしたいオブジェクト（棚など）。動かさない場合は空欄のままでOK")]
    public Transform objectToMove;

    [Tooltip("どれくらい移動させるか（現在の位置からの移動量。例: Xに 1.5 を入れるとX軸方向に1.5m動く）")]
    public Vector3 moveOffset;

    [Tooltip("移動にかける時間（秒）。ゆっくり動かすなら 3 などを設定")]
    public float moveDuration = 2.0f;

    [Header("【オプション】邪魔な壁の無効化")]
    [Tooltip("棚移動後に通れるようにするため、邪魔になっているBoxCollider等があればここに登録して消去します（＋ボタンで追加）")]
    public Collider[] collidersToDisable;

    [HideInInspector]
    public bool hasPlayed = false; // 再生済みかどうかの内部フラグ
}


public class InteractSubtitleManager : MonoBehaviour
{
    // 死んでも消えない、完了したイベントIDの歴史リスト
    public static List<string> clearedInteractEvents = new List<string>();

    [Header("【1】何もないところ（棚移動なし）")]
    [Tooltip("複数の調べるポイントを＋ボタンで追加できます")]
    public NormalInteractData[] normalInteractPoints;

    [Header("【2】イベント発生地（棚移動あり）")]
    [Tooltip("特別なイベントが起こる場所を設定します")]
    public SpecialInteractData specialInteractPoint;

    [Header("字幕：時間・フェード共通設定")]
    [Tooltip("1つの画像を表示にかける時間（秒）")]
    public float duration = 0.8f;
    [Tooltip("文字数（画像を何段階で表示するか）")]
    public int characterCount = 8;
    [Tooltip("すべて表示された後、消え始めるまでの待機時間（秒）")]
    public float displayTime = 1.0f;
    [Tooltip("最後の字幕がうっすら消えていくフェードアウトの時間（秒）")]
    public float fadeDuration = 1.0f;
    [Tooltip("前の字幕が消えてから、次の字幕が表示されるまでの間隔（秒）")]
    public float delayBetweenSubtitles = 0.5f;

    [Header("インタラクト設定")]
    [Tooltip("エリア内に入った時に表示する「Eキー」などの案内UIオブジェクト")]
    public GameObject interactPromptUI;

    [Header("参照設定")]
    [Tooltip("プレイヤーの操作を止めるために使用します")]
    public PlayerController playerController;

    // 内部変数
    private NormalInteractData currentNormalData = null;
    private SpecialInteractData currentSpecialData = null;
    private bool isAnimating = false;

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        // 【1】汎用ポイントの初期化
        if (normalInteractPoints != null)
        {
            foreach (var data in normalInteractPoints)
            {
                if (data == null || data.triggerArea == null) continue;

                if (!string.IsNullOrEmpty(data.eventID) && clearedInteractEvents.Contains(data.eventID))
                {
                    data.triggerArea.enabled = false;
                    data.hasPlayed = true;
                    continue;
                }

                data.triggerArea.isTrigger = true;
                SubtitleTriggerHandler handler = data.triggerArea.gameObject.AddComponent<SubtitleTriggerHandler>();
                handler.manager = this;
                handler.normalData = data;

                InitImages(data.subtitleImages);
            }
        }

        // 【2】イベント発生地の初期化
        if (specialInteractPoint != null && specialInteractPoint.triggerArea != null)
        {
            if (!string.IsNullOrEmpty(specialInteractPoint.eventID) && clearedInteractEvents.Contains(specialInteractPoint.eventID))
            {
                // すでにクリア済みなら棚を動かした状態にしておく
                if (specialInteractPoint.objectToMove != null && specialInteractPoint.moveOffset != Vector3.zero)
                {
                    specialInteractPoint.objectToMove.position = specialInteractPoint.objectToMove.position + specialInteractPoint.moveOffset;
                }

                // ★追加：リスポーン時も、邪魔なコライダーをしっかり無効化しておく
                if (specialInteractPoint.collidersToDisable != null)
                {
                    foreach (Collider col in specialInteractPoint.collidersToDisable)
                    {
                        if (col != null) col.enabled = false;
                    }
                }

                specialInteractPoint.triggerArea.enabled = false;
                specialInteractPoint.hasPlayed = true;

                // すでにクリア済みの場合も、このオブジェクトのチェックを外して非アクティブにする
                gameObject.SetActive(false);
                return;
            }
            else
            {
                specialInteractPoint.triggerArea.isTrigger = true;
                SubtitleTriggerHandler handler = specialInteractPoint.triggerArea.gameObject.AddComponent<SubtitleTriggerHandler>();
                handler.manager = this;
                handler.specialData = specialInteractPoint;

                InitImages(specialInteractPoint.subtitleImages);
            }
        }

        // 最初はEキー案内を隠しておく
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    private void InitImages(Image[] images)
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
        // 他の字幕が再生中なら、UIを隠して入力を受け付けない
        if (GlobalSubtitleState.IsAnySubtitlePlaying && !isAnimating)
        {
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        if (!isAnimating && !GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            bool promptShouldBeActive = false;

            // --- Normal の判定 ---
            if (currentNormalData != null)
            {
                if (!currentNormalData.playOnlyOnce || !currentNormalData.hasPlayed)
                {
                    promptShouldBeActive = true;
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (interactPromptUI != null) interactPromptUI.SetActive(false);
                        StartCoroutine(PlayNormalSequence(currentNormalData));
                        return; // 実行したらUpdateを抜ける
                    }
                }
            }

            // --- Special の判定 ---
            if (currentSpecialData != null)
            {
                if (!currentSpecialData.playOnlyOnce || !currentSpecialData.hasPlayed)
                {
                    promptShouldBeActive = true;
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (interactPromptUI != null) interactPromptUI.SetActive(false);
                        StartCoroutine(PlaySpecialSequence(currentSpecialData));
                        return; // 実行したらUpdateを抜ける
                    }
                }
            }

            // 案内UIの表示切替
            if (interactPromptUI != null)
            {
                if (promptShouldBeActive && !interactPromptUI.activeSelf)
                    interactPromptUI.SetActive(true);
                else if (!promptShouldBeActive && interactPromptUI.activeSelf)
                    interactPromptUI.SetActive(false);
            }
        }
    }

    // ==========================================
    // プレイヤーのエリア出入り管理（Normal用）
    // ==========================================
    public void OnPlayerEnterNormal(NormalInteractData data)
    {
        if (data.playOnlyOnce && data.hasPlayed) return;
        currentNormalData = data;
    }

    public void OnPlayerExitNormal(NormalInteractData data)
    {
        if (currentNormalData == data)
        {
            currentNormalData = null;
            if (interactPromptUI != null && currentSpecialData == null) interactPromptUI.SetActive(false);
        }
    }

    // ==========================================
    // プレイヤーのエリア出入り管理（Special用）
    // ==========================================
    public void OnPlayerEnterSpecial(SpecialInteractData data)
    {
        if (data.playOnlyOnce && data.hasPlayed) return;
        currentSpecialData = data;
    }

    public void OnPlayerExitSpecial(SpecialInteractData data)
    {
        if (currentSpecialData == data)
        {
            currentSpecialData = null;
            if (interactPromptUI != null && currentNormalData == null) interactPromptUI.SetActive(false);
        }
    }

    // ==========================================
    // 字幕シーケンス（Normal用：棚移動なし）
    // ==========================================
    IEnumerator PlayNormalSequence(NormalInteractData data)
    {
        isAnimating = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        yield return StartCoroutine(ShowImagesRoutine(data.subtitleImages));

        data.hasPlayed = true;
        if (data.playOnlyOnce && !string.IsNullOrEmpty(data.eventID))
        {
            if (!clearedInteractEvents.Contains(data.eventID)) clearedInteractEvents.Add(data.eventID);
            if (data.triggerArea != null) data.triggerArea.enabled = false;
        }

        if (playerController != null) playerController.canControl = true;

        isAnimating = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;

        if (currentNormalData == data && data.playOnlyOnce) currentNormalData = null;
    }

    // ==========================================
    // 字幕シーケンス（Special用：棚移動あり）
    // ==========================================
    IEnumerator PlaySpecialSequence(SpecialInteractData data)
    {
        isAnimating = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        yield return StartCoroutine(ShowImagesRoutine(data.subtitleImages));

        // --- オプションのオブジェクト移動 ---
        if (data.objectToMove != null && data.moveOffset != Vector3.zero)
        {
            Vector3 startPos = data.objectToMove.position;
            Vector3 endPos = startPos + data.moveOffset;
            float elapsed = 0f;

            while (elapsed < data.moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / data.moveDuration);
                data.objectToMove.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            data.objectToMove.position = endPos;
        }

        // --- ★追加：邪魔な壁（Collider）の無効化 ---
        if (data.collidersToDisable != null)
        {
            foreach (Collider col in data.collidersToDisable)
            {
                // 指定されたColliderを無効化して、通れるようにする！
                if (col != null) col.enabled = false;
            }
        }

        data.hasPlayed = true;
        if (data.playOnlyOnce && !string.IsNullOrEmpty(data.eventID))
        {
            if (!clearedInteractEvents.Contains(data.eventID)) clearedInteractEvents.Add(data.eventID);
            if (data.triggerArea != null) data.triggerArea.enabled = false;
        }

        if (playerController != null) playerController.canControl = true;

        isAnimating = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;

        if (currentSpecialData == data && data.playOnlyOnce) currentSpecialData = null;

        // 棚を動かす処理をしたら、このスクリプトがついているオブジェクトのチェックマークを外す！
        gameObject.SetActive(false);
    }

    // 画像表示部分の共通コルーチン
    IEnumerator ShowImagesRoutine(Image[] images)
    {
        if (images != null && images.Length > 0)
        {
            for (int i = 0; i < images.Length; i++)
            {
                Image currentImage = images[i];
                if (currentImage == null) continue;

                currentImage.gameObject.SetActive(true);
                currentImage.fillAmount = 0f;
                SetAlpha(currentImage, 1f);

                float timer = 0f;

                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / duration;

                    if (characterCount > 0) currentImage.fillAmount = Mathf.Floor(progress * characterCount) / characterCount;
                    else currentImage.fillAmount = progress;

                    yield return null;
                }

                currentImage.fillAmount = 1.0f;
                yield return new WaitForSeconds(displayTime);

                if (i == images.Length - 1)
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

                if (i < images.Length - 1)
                {
                    yield return new WaitForSeconds(delayBetweenSubtitles);
                }
            }
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

// =========================================================
// ★ 対象のBoxColliderに自動でくっついてプレイヤーを検知する補助スクリプト
// =========================================================
public class SubtitleTriggerHandler : MonoBehaviour
{
    [HideInInspector] public InteractSubtitleManager manager;
    [HideInInspector] public NormalInteractData normalData;
    [HideInInspector] public SpecialInteractData specialData;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (normalData != null) manager.OnPlayerEnterNormal(normalData);
            if (specialData != null) manager.OnPlayerEnterSpecial(specialData);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (normalData != null) manager.OnPlayerExitNormal(normalData);
            if (specialData != null) manager.OnPlayerExitSpecial(specialData);
        }
    }
}