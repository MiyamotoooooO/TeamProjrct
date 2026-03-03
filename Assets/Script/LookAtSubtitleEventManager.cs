using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LookAtSubtitleEventManager : MonoBehaviour
{
    [Header("【重要】イベント設定")]
    [Tooltip("プレイヤーが強制的に向く対象のオブジェクト（空オブジェクト等）")]
    public Transform lookTarget;

    [Tooltip("振り向く速さ")]
    public float turnSpeed = 3.0f;

    [Tooltip("リスポーンしても「終わった事」を記憶させるための名前（例: LookEvent1）。空欄なら毎回発動します")]
    public string eventID;

    [Header("表示設定")]
    [Tooltip("順番に表示させたいUIのImage画像（＋ボタンで何個でも登録できます）")]
    public Image[] targetImages;

    [Tooltip("1つの画像を表示にかける時間（秒）")]
    public float duration = 2.0f;

    [Tooltip("文字数（画像を何段階で表示するか）")]
    public int characterCount = 8;

    [Header("自動消去・フェード・間隔設定")]
    [Tooltip("すべて表示された後、消え始めるまでの待機時間（秒）")]
    public float displayTime = 3.0f;

    [Tooltip("最後の字幕がうっすら消えていくフェードアウトの時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("前の字幕が消えてから、次の字幕が表示されるまでの間隔（秒）")]
    public float delayBetweenSubtitles = 0.5f;

    [Header("参照設定")]
    public PlayerController playerController;

    // 内部変数
    private bool hasTriggered = false;

    // 完了したイベントIDの歴史リスト
    public static List<string> clearedLookEvents = new List<string>();

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        // すでに終わっているイベントなら、このトリガーをオフにする
        if (!string.IsNullOrEmpty(eventID) && clearedLookEvents.Contains(eventID))
        {
            hasTriggered = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            return;
        }

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

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            // 他の字幕が再生中なら完全に無視する
            if (GlobalSubtitleState.IsAnySubtitlePlaying) return;

            hasTriggered = true;
            StartCoroutine(PlayLookAtAndSubtitle());
        }
    }

    IEnumerator PlayLookAtAndSubtitle()
    {
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // グローバルロックON

        // ★時間を止めて、敵もプレイヤーも動けなくする！
        Time.timeScale = 0f;

        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // --- ① ターゲットの方を強制的に向く処理 ---
        if (lookTarget != null && playerController != null)
        {
            Transform playerTransform = playerController.transform;
            Transform camTransform = playerController.cam.transform;

            // プレイヤーの体の回転（Y軸のみ）を計算
            Vector3 direction = lookTarget.position - playerTransform.position;
            direction.y = 0; // 上下は無視
            Quaternion targetBodyRot = playerTransform.rotation;
            if (direction != Vector3.zero)
            {
                targetBodyRot = Quaternion.LookRotation(direction);
            }

            // カメラの回転（X軸：上下のみ）を計算
            Vector3 camDirection = lookTarget.position - camTransform.position;
            Quaternion targetCamLookRot = Quaternion.LookRotation(camDirection);
            Vector3 camEuler = targetCamLookRot.eulerAngles;
            Quaternion targetCamRot = Quaternion.Euler(camEuler.x, 0, 0);

            Quaternion startBodyRot = playerTransform.rotation;
            Quaternion startCamRot = camTransform.localRotation;

            float t = 0f;
            while (t < 1.0f)
            {
                // 時間が止まっているので Time.unscaledDeltaTime を使う
                t += Time.unscaledDeltaTime * turnSpeed;

                playerTransform.rotation = Quaternion.Slerp(startBodyRot, targetBodyRot, t);
                camTransform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
                playerController.SyncRotationToCurrent();

                yield return null;
            }

            // 最後にピッタリ合わせる
            playerTransform.rotation = targetBodyRot;
            camTransform.localRotation = targetCamRot;
            playerController.SyncRotationToCurrent();
        }

        // --- ② 字幕の処理 ---
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

                // 時間停止中なので Time.unscaledDeltaTime を使う
                while (timer < duration)
                {
                    timer += Time.unscaledDeltaTime;
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

                // 時間停止中なので WaitForSecondsRealtime を使う
                yield return new WaitForSecondsRealtime(displayTime);

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
                    yield return new WaitForSecondsRealtime(delayBetweenSubtitles);
                }
            }
        }

        // --- ③ 終了処理 ---
        if (!string.IsNullOrEmpty(eventID) && !clearedLookEvents.Contains(eventID))
        {
            clearedLookEvents.Add(eventID);
        }

        // このトリガーを二度と踏めないように無効化する
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // ★時間を元に戻して敵も動けるようにする
        Time.timeScale = 1f;

        if (playerController != null)
        {
            playerController.canControl = true;
        }

        GlobalSubtitleState.IsAnySubtitlePlaying = false; // ★ グローバルロックOFF
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