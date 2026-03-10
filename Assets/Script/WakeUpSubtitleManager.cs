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

    [Tooltip("1つの字幕の文字が全部出るまでにかかる時間（秒）")]
    public float duration = 2.0f;
    [Tooltip("文字数（画像を何段階で表示するか。0なら滑らか）")]
    public int characterCount = 8;
    [Tooltip("文字が全部出た後に表示したままにする時間（秒）")]
    public float displayTime = 1.0f;
    [Tooltip("★最後の字幕が消える時のフェードアウト時間（秒）")]
    public float fadeDuration = 1.0f;
    [Tooltip("次の字幕が出るまでの待機時間（秒）")]
    public float delayBetweenSubtitles = 0.5f;

    [Header("参照設定")]
    [Tooltip("プレイヤーの操作を止めるために使用します")]
    public PlayerController playerController;

    [Tooltip("ベッドから起き上がる処理を監視するために使用します")]
    public WakeUpController wakeUpController;

    // 内部変数
    private bool isWaitingForWakeUp = false;
    private bool hasTriggered = false;

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
                    SetAlpha(img, 1f);
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

        // --- ②自動字幕表示開始 ---
        if (subtitleImages != null && subtitleImages.Length > 0)
        {
            for (int i = 0; i < subtitleImages.Length; i++)
            {
                Image currentImage = subtitleImages[i];
                if (currentImage == null) continue;

                currentImage.gameObject.SetActive(true);
                currentImage.fillAmount = 0f;
                SetAlpha(currentImage, 1f);

                float timer = 0f;

                // 1. タイプライター表示
                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / duration;

                    if (characterCount > 0) currentImage.fillAmount = Mathf.Floor(progress * characterCount) / characterCount;
                    else currentImage.fillAmount = progress;

                    yield return null;
                }
                currentImage.fillAmount = 1.0f;

                // 2. 表示状態をキープ
                yield return new WaitForSeconds(displayTime);

                // 3. 最後の字幕だけフェードアウト
                if (i == subtitleImages.Length - 1)
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

                // 4. 画像を非表示にする
                SetAlpha(currentImage, 0f);
                currentImage.gameObject.SetActive(false);

                // 5. 次の字幕への待機（最後でなければ）
                if (i < subtitleImages.Length - 1)
                {
                    yield return new WaitForSeconds(delayBetweenSubtitles);
                }
            }
        }

        // 字幕がすべて終わったらプレイヤーが再び動けるようにする
        if (playerController != null)
        {
            playerController.canControl = true;
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