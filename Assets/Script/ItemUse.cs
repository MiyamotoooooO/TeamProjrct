using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class ItemUse : MonoBehaviour
{
    public PlayerController player;
    public GameObject cam;

    [Header("使用距離")]
    public float useDistance = 3f;

    [Header("鍵アイテム（Door 用）")]
    public GameObject keyObject;

    [Header("Bloodlump 除去に必要なアイテム名")]
    public string detergentName = "Detergent";

    [Header("Bloodlump 除去後に出す Sphere")]
    public GameObject spawnSpherePrefab;

    [Tooltip("出現位置のズレ（当たった場所からのズレ。例: Yを0.5にすると少し浮いて出ます）")]
    public Vector3 spawnOffset = Vector3.zero;
    [Tooltip("出現するアイテムの大きさ (1, 1, 1が標準)")]
    public Vector3 spawnScale = Vector3.one;

    public TMP_Text UseText;

    public ParticleSystem ps;
    public float bubble_duration = 2f;

    // ==========================================
    // 演出・字幕・暗転設定
    // ==========================================
    [Header("【演出】暗転設定")]
    [Tooltip("画面を真っ暗にするためのPostProcessVolume")]
    public PostProcessVolume blackFadeVolume;
    [Tooltip("暗転にかかる時間（秒）")]
    public float blackFadeDuration = 1.5f;

    // ==========================================
    // ★追加：音声演出の設定
    // ==========================================
    [Header("【演出】音声設定")]
    [Tooltip("音を鳴らすためのAudioSource（プレイヤーやカメラに付けたものを登録）")]
    public AudioSource audioSource;
    [Tooltip("暗転直後に鳴らす1つ目の音")]
    public AudioClip firstSound;
    [Tooltip("1つ目の後に鳴らす2つ目の音")]
    public AudioClip secondSound;
    [Tooltip("2つ目の音をそのまま鳴らす時間（秒）")]
    public float secondSoundDuration = 3.0f;
    [Tooltip("2つ目の音がフェードアウトして消えるまでの時間（秒）")]
    public float audioFadeDuration = 2.0f;

    [Header("字幕：時間・フェード共通設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("字幕：画像データ")]
    [Tooltip("洗剤を使用した直後（選択肢が出る前）に出る字幕")]
    public Image[] startSubtitleImages;
    [Tooltip("画面が明るくなった後に出る字幕")]
    public Image[] endSubtitleImages;

    // ==========================================
    // 選択肢UI設定
    // ==========================================
    [Header("【Bloodlump】選択肢UI設定")]
    [Tooltip("クイズの問題文とボタンが含まれる親パネル")]
    public GameObject choicePanel;
    [Tooltip("「はい」のボタン")]
    public Button yesButton;
    [Tooltip("「いいえ」のボタン")]
    public Button noButton;

    // 内部変数（選択結果を待つため）
    private bool isChoiceMade = false;
    private bool isYesChosen = false;

    private void Start()
    {
        // 画像の初期化
        InitImages(startSubtitleImages);
        InitImages(endSubtitleImages);

        // 暗転ボリュームの初期化
        if (blackFadeVolume != null) blackFadeVolume.weight = 0f;

        // 選択肢UIの初期化とボタン機能の登録
        if (choicePanel != null) choicePanel.SetActive(false);
        if (yesButton != null) yesButton.onClick.AddListener(() => { isChoiceMade = true; isYesChosen = true; });
        if (noButton != null) noButton.onClick.AddListener(() => { isChoiceMade = true; isYesChosen = false; });
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

    private void Update()
    {
        if (GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            UseText.enabled = false;
            return;
        }

        if (player.isInventoryOpen)
        {
            UseText.enabled = false;
            return;
        }

        ShowClickUI();

        if (Input.GetMouseButtonDown(0))
        {
            TryUseItem();
        }
        player.UpdateKeySwing();
    }

    async void TryUseItem()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, useDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // Bloodlump 処理
        if (hit.collider.CompareTag("Bloodlump"))
        {
            if (player.inventoryManager.HasItem(detergentName))
            {
                StartCoroutine(BloodlumpSequence(hit.collider.gameObject, hit.point));
            }
            return;
        }

        // PuzzleButton
        if (hit.collider.CompareTag("PuzzleButton"))
        {
            PuzzleButton btn = hit.collider.GetComponent<PuzzleButton>();
            if (btn != null) btn.PressButton();
            return;
        }

        // 回転パズル
        if (hit.collider.CompareTag("RotateObject"))
        {
            RotateObject rot = hit.collider.GetComponent<RotateObject>();
            if (rot != null) rot.RotateLeft();
            return;
        }

        // Sink
        if (hit.collider.CompareTag("Sink"))
        {
            string dirtykeyName = "Dirtykey";
            string cleankeyName = "Key";

            if (player.inventoryManager.HasItem(dirtykeyName))
            {
                player.PlayItemSwing();
                await Task.Delay(800);

                player.inventoryManager.RemoveItem(dirtykeyName);
                player.inventoryManager.AddItem(cleankeyName);

                player.UpdateItemModel();
                Debug.Log("Dirtykeyを洗ってKeyに変化させました");

                return;
            }
            return;
        }

        // Door
        var door = hit.collider.GetComponentInParent<DoubleDoorController>();
        if (door == null) return;

        string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();

        if (!player.inventoryManager.HasItem(requiredKeyName)) return;

        string tag = player.inventoryManager.GetItemTag(requiredKeyName);
        if (tag != "Key") return;

        player.canControl = false;
        player.canLock = false;

        player.PlayKeySwing();
        await Task.Delay(1000);

        player.inventoryManager.RemoveItem(requiredKeyName);

        await Task.Delay(3000);
        player.canControl = true;
        player.canLock = true;
    }

    void ShowClickUI()
    {
        UseText.enabled = false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, useDistance)) return;

        if (hit.collider.CompareTag("PuzzleButton"))
        {
            PuzzleButton btn = hit.collider.GetComponent<PuzzleButton>();
            if (btn != null) btn.OnHover();
            return;
        }

        if (hit.collider.CompareTag("RotateObject"))
        {
            RotateObject rot = hit.collider.GetComponent<RotateObject>();
            if (rot != null) rot.OnHover();
            return;
        }

        if (hit.collider.CompareTag("Bloodlump"))
        {
            if (player.inventoryManager.HasItem(detergentName)) UseText.enabled = true;
            return;
        }

        var door = hit.collider.GetComponentInParent<DoubleDoorController>();
        if (door != null)
        {
            string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();
            if (player.inventoryManager.HasItem(requiredKeyName)) UseText.enabled = true;
        }

        if (hit.collider.CompareTag("Sink"))
        {
            if (player.inventoryManager.HasItem("Dirtykey")) UseText.enabled = true;
            return;
        }
    }

    // ==========================================
    // 血の塊の演出シーケンス
    // ==========================================
    private IEnumerator BloodlumpSequence(GameObject bloodlumpObj, Vector3 hitPoint)
    {
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        if (player != null)
        {
            player.canControl = false;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // 1. 開始時の字幕を表示
        yield return StartCoroutine(ShowImagesRoutine(startSubtitleImages));

        // 2. 選択肢UIを表示してマウスクリックを待つ
        isChoiceMade = false;
        isYesChosen = false;

        if (choicePanel != null) choicePanel.SetActive(true);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        yield return new WaitUntil(() => isChoiceMade);

        if (choicePanel != null) choicePanel.SetActive(false);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (!isYesChosen)
        {
            if (player != null) player.canControl = true;
            GlobalSubtitleState.IsAnySubtitlePlaying = false;
            yield break;
        }

        player.PlayItemSwing();
        StartCoroutine(PlayForSeconds());
        yield return new WaitForSeconds(0.9f);

        // 3. 画面を徐々に暗転させる
        if (blackFadeVolume != null)
        {
            float elapsed = 0f;
            while (elapsed < blackFadeDuration)
            {
                elapsed += Time.deltaTime;
                blackFadeVolume.weight = Mathf.Lerp(0f, 1f, elapsed / blackFadeDuration);
                yield return null;
            }
            blackFadeVolume.weight = 1f;
        }

        // 4. 真っ暗な間に血の塊を消して、鍵を出す
        if (bloodlumpObj != null) Destroy(bloodlumpObj);
        GameObject spawnedObj = Instantiate(spawnSpherePrefab, hitPoint + spawnOffset, Quaternion.identity);
        spawnedObj.transform.localScale = spawnScale;

        player.inventoryManager.RemoveItem(detergentName);
        player.UpdateItemModel();

        // ==========================================
        // ★追加：音声シーケンス（暗転中に実行）
        // ==========================================
        if (audioSource != null)
        {
            audioSource.volume = 1f; // 音量を最大にしておく

            // ① 1つ目の音を鳴らす
            if (firstSound != null)
            {
                audioSource.PlayOneShot(firstSound);
                // 鳴り終わるまで待機
                yield return new WaitForSeconds(firstSound.length);
            }

            // ② 2つ目の音を鳴らす
            if (secondSound != null)
            {
                audioSource.clip = secondSound;
                audioSource.Play();

                // 3秒間そのまま鳴らし続ける
                yield return new WaitForSeconds(secondSoundDuration);

                // ③ 音量を徐々に小さくする（フェードアウト）
                float fadeElapsed = 0f;
                float startVolume = audioSource.volume;

                while (fadeElapsed < audioFadeDuration)
                {
                    fadeElapsed += Time.deltaTime;
                    audioSource.volume = Mathf.Lerp(startVolume, 0f, fadeElapsed / audioFadeDuration);
                    yield return null;
                }

                // 完全に音が消えたら停止する
                audioSource.volume = 0f;
                audioSource.Stop();
                audioSource.clip = null; // クリップを外しておく
            }
        }
        else
        {
            // もしAudioSourceが設定されていない場合は、とりあえず1秒待つ
            yield return new WaitForSeconds(1.0f);
        }

        // 5. 画面を徐々に明転させる（音が完全に消えた後）
        if (blackFadeVolume != null)
        {
            float elapsed = 0f;
            while (elapsed < blackFadeDuration)
            {
                elapsed += Time.deltaTime;
                blackFadeVolume.weight = Mathf.Lerp(1f, 0f, elapsed / blackFadeDuration);
                yield return null;
            }
            blackFadeVolume.weight = 0f;
        }

        // 6. 終了時の字幕を表示
        yield return StartCoroutine(ShowImagesRoutine(endSubtitleImages));

        // 7. 完了処理
        if (player != null) player.canControl = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

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

    IEnumerator PlayForSeconds()
    {
        ps.Play();
        yield return new WaitForSeconds(bubble_duration);
        ps.Stop(false);
    }
}