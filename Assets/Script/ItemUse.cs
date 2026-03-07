using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Collections; // ★追加
using UnityEngine.UI; // ★追加
using UnityEngine.Rendering.PostProcessing; // ★追加

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

    // ==========================================
    // ★追加：演出・字幕・暗転設定
    // ==========================================
    [Header("【演出】暗転設定")]
    [Tooltip("画面を真っ暗にするためのPostProcessVolume")]
    public PostProcessVolume blackFadeVolume;
    [Tooltip("暗転にかかる時間（秒）")]
    public float blackFadeDuration = 1.5f;
    [Tooltip("真っ暗な状態を維持する時間（秒）")]
    public float blackWaitTime = 1.0f;

    [Header("字幕：時間・フェード共通設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("字幕：画像データ")]
    [Tooltip("洗剤を使用した直後（暗転前）に出る字幕")]
    public Image[] startSubtitleImages;
    [Tooltip("画面が明るくなった後に出る字幕")]
    public Image[] endSubtitleImages;

    private void Start()
    {
        // 画像の初期化
        InitImages(startSubtitleImages);
        InitImages(endSubtitleImages);

        // 暗転ボリュームの初期化
        if (blackFadeVolume != null) blackFadeVolume.weight = 0f;
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
        // ★ 追加：他の字幕が再生中なら入力を受け付けない
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

        // 何も当たらなかったら即終了
        if (!Physics.Raycast(ray, out hit, useDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // ★ Bloodlump 処理
        if (hit.collider.CompareTag("Bloodlump"))
        {
            if (player.inventoryManager.HasItem(detergentName))
            {
                // ★ 変更：演出用のコルーチンを呼び出す
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

            // Dirtykeyを持っているか
            if (player.inventoryManager.HasItem(dirtykeyName))
            {
                player.PlayItemSwing(); // 動作
                await Task.Delay(800);

                // Dirtykeyを削除してKeyを追加
                player.inventoryManager.RemoveItem(dirtykeyName);
                player.inventoryManager.AddItem(cleankeyName);

                player.UpdateItemModel();
                Debug.Log("Dirtykeyを洗ってKeyに変化させました");

                return;
            }

            return;
        }

        // Door 以外なら鍵処理は絶対にしない
        var door = hit.collider.GetComponentInParent<DoubleDoorController>();
        if (door == null)
        {
            return;
        }

        // 鍵名
        string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();

        // 鍵を持っていなければ振らない
        if (!player.inventoryManager.HasItem(requiredKeyName))
        {
            Debug.Log("鍵を持っていません：" + requiredKeyName);
            return;
        }

        // 鍵のタグ確認
        string tag = player.inventoryManager.GetItemTag(requiredKeyName);
        if (tag != "Key")
        {
            return; // 鍵じゃないなら振らない
        }

        // ここまで来て初めて鍵を振る（ドア確定）
        player.canControl = false; // 移動停止
        player.canLock = false; // 視点移動

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
            if (btn != null) btn.OnHover(); // ボタンの色を明るくする
            return; // ボタンを見つめている時は他の判定をしない
        }

        if (hit.collider.CompareTag("RotateObject"))
        {
            RotateObject rot = hit.collider.GetComponent<RotateObject>();
            if (rot != null) rot.OnHover();
            return;
        }

        // Bloodlump
        if (hit.collider.CompareTag("Bloodlump"))
        {
            if (player.inventoryManager.HasItem(detergentName))
            {
                UseText.enabled = true;
            }
            return;
        }

        // Door
        var door = hit.collider.GetComponentInParent<DoubleDoorController>();
        if (door != null)
        {
            string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();

            if (player.inventoryManager.HasItem(requiredKeyName))
            {
                UseText.enabled = true;
            }
        }

        // Sink
        if (hit.collider.CompareTag("Sink"))
        {
            if (player.inventoryManager.HasItem("Dirtykey"))
            {
                UseText.enabled = true;
            }
            return;
        }
    }

    // ==========================================
    // ★追加：血の塊の演出シーケンス
    // ==========================================
    private IEnumerator BloodlumpSequence(GameObject bloodlumpObj, Vector3 hitPoint)
    {
        GlobalSubtitleState.IsAnySubtitlePlaying = true; // 他の字幕やUIをロック

        // プレイヤーの操作をロックして立ち止まらせる
        if (player != null)
        {
            player.canControl = false;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // アイテムを振るアニメーション
        player.PlayItemSwing();
        yield return new WaitForSeconds(0.9f);

        // 1. 開始時の字幕（暗転前）を表示
        yield return StartCoroutine(ShowImagesRoutine(startSubtitleImages));

        // 2. 画面を徐々に暗転させる（PostProcessVolumeのWeightを上げる）
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

        // 3. 真っ暗な間に血の塊を消して、鍵を出す
        if (bloodlumpObj != null)
        {
            Destroy(bloodlumpObj);
        }

        GameObject spawnedObj = Instantiate(spawnSpherePrefab, hitPoint + spawnOffset, Quaternion.identity);
        spawnedObj.transform.localScale = spawnScale;

        // インベントリから洗剤を消す
        player.inventoryManager.RemoveItem(detergentName);
        player.UpdateItemModel();

        // そのまま少し待機
        yield return new WaitForSeconds(blackWaitTime);

        // 4. 画面を徐々に明転させる（Weightを下げる）
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

        // 5. 終了時の字幕（暗転後）を表示
        yield return StartCoroutine(ShowImagesRoutine(endSubtitleImages));

        // 6. 完了処理（操作を戻す）
        if (player != null)
        {
            player.canControl = true;
        }
        GlobalSubtitleState.IsAnySubtitlePlaying = false; // ロック解除
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