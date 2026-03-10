using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleRotateManager : MonoBehaviour
{
    [Header("回転オブジェクト数")]
    public int objectCount = 3;

    private int[] currentDirections;
    private int[] correctDirections;

    [Header("正解時に出す鍵")]
    public GameObject keyPrefab;
    public Transform spawnPoint;

    [Header("字幕：時間・フェード共通設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("字幕データ（順番に表示）")]
    [Tooltip("正解したときに出る字幕")]
    public Image[] clearSubtitleImages;

    [Header("参照設定")]
    public PlayerController playerController;

    private bool keySpawned = false;

    private void Awake()
    {
        currentDirections = new int[objectCount];
        correctDirections = new int[objectCount];

        for (int i = 0; i < objectCount; i++)
        {
            // ★追加：最初は「まだ判定されていない状態」として -1 を入れておく
            currentDirections[i] = -1;
            // ユーザーの希望通り、初期の目標(正解)を 0 に設定する
            correctDirections[i] = 0;
        }
    }

    private void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        InitImages(clearSubtitleImages);
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

    public void SetCorrectDirection(int id, int dir)
    {
        if (id >= 0 && id < objectCount)
        {
            correctDirections[id] = dir;
        }
    }

    public void UpdateDirection(int id, int dir)
    {
        if (id < 0 || id >= objectCount)
        {
            Debug.LogError($"エラー：ObjectID[{id}]は無効です！");
            return;
        }

        currentDirections[id] = dir;

        if (keySpawned) return;

        string currentLog = "現在: ";
        string correctLog = "目標: ";
        bool isAllCorrect = true;
        bool isAllInitialized = true;

        for (int i = 0; i < objectCount; i++)
        {
            // -1の場合は「未」と表示してわかりやすくする
            currentLog += (currentDirections[i] == -1 ? "未" : currentDirections[i].ToString()) + " ";
            correctLog += correctDirections[i] + " ";

            if (currentDirections[i] == -1)
            {
                isAllInitialized = false; // まだ判定が終わっていない像がある
            }

            if (currentDirections[i] != correctDirections[i])
            {
                isAllCorrect = false; // 1つでも不正解（1）があればダメ
            }
        }

        Debug.Log($"【パズル判定】{currentLog}| {correctLog}");

        // ★追加：まだすべてのオブジェクトの初期判定が終わっていない、または不正解ならクリアにしない
        if (!isAllInitialized || !isAllCorrect) return;

        // 全問正解！
        keySpawned = true;
        StartCoroutine(ClearSequence());
    }

    // ==========================================
    // クリア時の演出シーケンス
    // ==========================================
    IEnumerator ClearSequence()
    {
        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        if (keyPrefab != null && spawnPoint != null)
        {
            Instantiate(keyPrefab, spawnPoint.position, Quaternion.identity);
            Debug.Log("パズル正解！鍵が出現しました。");
        }

        yield return StartCoroutine(ShowImagesRoutine(clearSubtitleImages));

        if (playerController != null)
        {
            playerController.canControl = true;
        }
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    // ==========================================
    // 画像表示部分の共通コルーチン
    // ==========================================
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