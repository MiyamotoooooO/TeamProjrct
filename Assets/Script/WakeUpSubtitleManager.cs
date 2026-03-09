using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WakeUpSubtitleManager : MonoBehaviour
{
    [Header("視点移動演出の設定")]
    [Tooltip("左に向く角度（マイナスで左、プラスで右）")]
    public float lookLeftAngle = -60f;
    [Tooltip("左を向くのにかかる時間（秒）")]
    public float lookLeftDuration = 1.5f;
    [Tooltip("左を向いたまま待機する時間（秒）")]
    public float lookWaitDuration = 1.0f;
    [Tooltip("正面に戻るのにかかる時間（秒）")]
    public float lookBackDuration = 1.5f;

    [Header("字幕表示設定")]
    [Tooltip("順番に表示させたいUIのImage画像（+ボタンで複数登録可能）")]
    public Image[] subtitleImages;

    [Tooltip("1つの字幕の表示にかける時間（秒）")]
    public float duration = 2.0f;

    [Tooltip("文字数（画像を何段階で表示するか。0なら滑らか）")]
    public int characterCount = 8;

    [Header("参照設定")]
    [Tooltip("プレイヤーの操作を止めるために使用します")]
    public PlayerController playerController;

    [Tooltip("ベッドから起き上がる処理を監視するために使用します")]
    public WakeUpController wakeUpController;

    // 内部変数
    private bool isWaitingForWakeUp = false;
    private bool hasTriggered = false;
    private bool isTyping = false;
    private bool isFullDisplayed = false;
    private Coroutine typingCoroutine;
    private int currentSubtitleIndex = 0;    // 今何番目の字幕を表示しているか

    // ★追加：ゲーム起動後に1度でも表示したかを記憶する魔法の変数
    private static bool hasPlayedOnce = false;

    void Start()
    {
        // アタッチし忘れ防止のため、シーン内から自動取得
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (wakeUpController == null) wakeUpController = FindAnyObjectByType<WakeUpController>();

        // 画像群の初期化
        if (subtitleImages != null)
        {
            foreach (Image img in subtitleImages)
            {
                if (img != null)
                {
                    img.type = Image.Type.Filled;
                    img.fillMethod = Image.FillMethod.Horizontal;
                    img.fillOrigin = (int)Image.OriginHorizontal.Left;
                    img.fillAmount = 0f;
                    img.gameObject.SetActive(false);
                }
            }
        }

        // すでに1度でも字幕を表示したことがあるならスキップ
        if (hasPlayedOnce)
        {
            isWaitingForWakeUp = false;
            hasTriggered = true;
            return;
        }

        // ゲーム開始時に「寝ている状態」なら、起きるのを待つフラグを立てる
        if (wakeUpController != null && wakeUpController.isSleeping)
        {
            isWaitingForWakeUp = true;
        }
        else
        {
            hasTriggered = true;
        }
    }

    void Update()
    {
        // --- 1. 起き上がりを検知してイベントスタート ---
        if (isWaitingForWakeUp && !hasTriggered)
        {
            // isSleepingとisWakingUpが両方ともfalseになった ＝ 起き上がりアニメーションが完了した！
            if (!wakeUpController.isSleeping && !wakeUpController.isWakingUp)
            {
                isWaitingForWakeUp = false;
                StartCoroutine(WakeUpSequence());
            }
        }

        // --- 2. 字幕表示中のSpaceキー操作 ---
        // 視点が動いている最中は入力を受け付けない
        if (isTyping || isFullDisplayed)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isTyping)
                {
                    // パターン1：まだ文字が出ている途中なら「スキップ」
                    SkipAnimation();
                }
                else if (isFullDisplayed)
                {
                    // パターン2：すべて表示済みなら「次の字幕へ」
                    NextSubtitle();
                }
            }
        }
    }

    // 起き上がり後の「視点移動 ＋ 字幕」の一連の処理
    IEnumerator WakeUpSequence()
    {
        hasTriggered = true;
        hasPlayedOnce = true;

        // プレイヤーの操作を無効化
        if (playerController != null)
        {
            playerController.canControl = false;
        }

        // --- ①視点移動演出 ---
        Transform camTransform = null;
        if (playerController != null && playerController.cam != null)
        {
            camTransform = playerController.cam.transform;
        }

        if (camTransform != null)
        {
            // 元の角度と、左を向いた時の角度を計算
            Quaternion startRot = camTransform.localRotation;
            Quaternion targetRot = startRot * Quaternion.Euler(0, lookLeftAngle, 0);

            // 1. ゆっくり左を向く
            float time = 0f;
            while (time < lookLeftDuration)
            {
                time += Time.deltaTime;
                // SmoothStepを使って、最初と最後を滑らかに動かす
                float t = Mathf.SmoothStep(0f, 1f, time / lookLeftDuration);
                camTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            camTransform.localRotation = targetRot;

            // 2. そのまま待機
            yield return new WaitForSeconds(lookWaitDuration);

            // 3. ゆっくり正面に戻る
            time = 0f;
            while (time < lookBackDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, time / lookBackDuration);
                camTransform.localRotation = Quaternion.Slerp(targetRot, startRot, t);
                yield return null;
            }
            camTransform.localRotation = startRot;

            // ★重要：視点を動かしたことによるズレをPlayerControllerに同期させる
            playerController.SyncRotationToCurrent();
        }

        // --- ②字幕表示開始 ---
        currentSubtitleIndex = 0;
        ShowSubtitle(currentSubtitleIndex);
    }

    // 指定したインデックスの字幕を表示する
    private void ShowSubtitle(int index)
    {
        if (subtitleImages == null || index >= subtitleImages.Length || subtitleImages[index] == null)
        {
            CloseText();
            return;
        }

        Image targetImage = subtitleImages[index];
        targetImage.gameObject.SetActive(true);
        typingCoroutine = StartCoroutine(PlayTypewriter(targetImage));
    }

    // 徐々に表示するコルーチン
    IEnumerator PlayTypewriter(Image targetImage)
    {
        isTyping = true;
        isFullDisplayed = false;
        targetImage.fillAmount = 0f;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            if (characterCount > 0)
            {
                // 文字数に合わせてカクカク表示
                float steppedProgress = Mathf.Floor(progress * characterCount) / characterCount;
                targetImage.fillAmount = steppedProgress;
            }
            else
            {
                // 滑らか表示
                targetImage.fillAmount = progress;
            }
            yield return null;
        }

        FinishDisplay();
    }

    // スキップ処理
    void SkipAnimation()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        FinishDisplay();
    }

    // 1枚の字幕の表示完了状態にする
    void FinishDisplay()
    {
        if (subtitleImages != null && currentSubtitleIndex < subtitleImages.Length)
        {
            Image currentImg = subtitleImages[currentSubtitleIndex];
            if (currentImg != null) currentImg.fillAmount = 1.0f; // 完全に表示
        }
        isTyping = false;
        isFullDisplayed = true;
    }

    // 次の字幕へ進む処理
    void NextSubtitle()
    {
        // 今表示している画像を隠す
        if (subtitleImages != null && currentSubtitleIndex < subtitleImages.Length)
        {
            Image currentImg = subtitleImages[currentSubtitleIndex];
            if (currentImg != null)
            {
                currentImg.fillAmount = 0f;
                currentImg.gameObject.SetActive(false);
            }
        }

        currentSubtitleIndex++;

        // まだ次の字幕があれば表示、なければ終了
        if (subtitleImages != null && currentSubtitleIndex < subtitleImages.Length)
        {
            ShowSubtitle(currentSubtitleIndex);
        }
        else
        {
            CloseText();
        }
    }

    // すべての字幕を終え、プレイヤーの操作を再び有効にする
    void CloseText()
    {
        isTyping = false;
        isFullDisplayed = false;

        // 字幕がすべて終わったらプレイヤーが再び動けるようにする
        if (playerController != null)
        {
            playerController.canControl = true;
        }
    }
}