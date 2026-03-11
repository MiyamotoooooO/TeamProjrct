using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic; // ★追加：Dictionary用
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

    [Header("Bloodlump 除去設定")]
    public string detergentName = "Detergent";

    [Tooltip("【重要】シーン内にもともと隠してある鍵オブジェクトをアタッチしてください")]
    public GameObject sceneKeyObject; // ★生成ではなく、既存のものを表示させる

    public TMP_Text UseText;

    public ParticleSystem ps;
    public float bubble_duration = 2f;

    // ==========================================
    // 演出・字幕・暗転設定
    // ==========================================
    [Header("【演出】暗転設定")]
    public PostProcessVolume blackFadeVolume;
    public float blackFadeDuration = 1.5f;

    [Header("【演出】音声設定")]
    public AudioSource eventAudioSource; // スクリプト内のAudioSourceと被らないよう名前変更
    public AudioClip firstSound;
    public AudioClip secondSound;
    public float secondSoundDuration = 3.0f;
    public float audioFadeDuration = 2.0f;

    [Header("字幕設定")]
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    [Header("字幕：画像データ")]
    public Image[] startSubtitleImages;
    public Image[] endSubtitleImages;

    [Header("【Bloodlump】選択肢UI設定")]
    public GameObject choicePanel;
    public Button yesButton;
    public Button noButton;

    // 内部変数
    private bool isChoiceMade = false;
    private bool isYesChosen = false;
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    private void Start()
    {
        InitImages(startSubtitleImages);
        InitImages(endSubtitleImages);

        if (blackFadeVolume != null) blackFadeVolume.weight = 0f;

        // ★追加：アタッチされた鍵を最初は確実に非表示にしておく
        if (sceneKeyObject != null) sceneKeyObject.SetActive(false);

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

        if (!Physics.Raycast(ray, out hit, useDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore)) return;

        if (hit.collider.CompareTag("Bloodlump"))
        {
            if (player.inventoryManager.HasItem(detergentName))
            {
                StartCoroutine(BloodlumpSequence(hit.collider.gameObject));
            }
            return;
        }

        // --- 他のインタラクト処理はそのまま ---
        if (hit.collider.CompareTag("PuzzleButton")) { hit.collider.GetComponent<PuzzleButton>()?.PressButton(); return; }
        if (hit.collider.CompareTag("RotateObject")) { hit.collider.GetComponent<RotateObject>()?.RotateLeft(); return; }

        if (hit.collider.CompareTag("Sink"))
        {
            if (player.inventoryManager.HasItem("Dirtykey"))
            {
                player.PlayItemSwing();
                await Task.Delay(800);
                player.inventoryManager.RemoveItem("Dirtykey");
                player.inventoryManager.AddItem("Key");
                player.UpdateItemModel();
                return;
            }
        }

        var door = hit.collider.GetComponentInParent<DoubleDoorController>();
        if (door != null)
        {
            string requiredKeyName = keyObject.name.Replace("(Clone)", "").Trim();
            if (!player.inventoryManager.HasItem(requiredKeyName)) return;
            if (player.inventoryManager.GetItemTag(requiredKeyName) != "Key") return;

            player.canControl = false;
            player.canLock = false;
            player.PlayKeySwing();
            await Task.Delay(1000);
            player.inventoryManager.RemoveItem(requiredKeyName);
            await Task.Delay(3000);
            player.canControl = true;
            player.canLock = true;
        }
    }

    void ShowClickUI()
    {
        UseText.enabled = false;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, useDistance)) return;

        if (hit.collider.CompareTag("PuzzleButton") || hit.collider.CompareTag("RotateObject"))
        {
            hit.collider.GetComponent<PuzzleButton>()?.OnHover();
            hit.collider.GetComponent<RotateObject>()?.OnHover();
            return;
        }

        if (hit.collider.CompareTag("Bloodlump"))
        {
            if (player.inventoryManager.HasItem(detergentName)) UseText.enabled = true;
            return;
        }

        if (hit.collider.CompareTag("Sink"))
        {
            if (player.inventoryManager.HasItem("Dirtykey")) UseText.enabled = true;
            return;
        }

        var door = hit.collider.GetComponentInParent<DoubleDoorController>();
        if (door != null)
        {
            string reqKey = keyObject.name.Replace("(Clone)", "").Trim();
            if (player.inventoryManager.HasItem(reqKey)) UseText.enabled = true;
        }
    }

    private IEnumerator BloodlumpSequence(GameObject bloodlumpObj)
    {
        GlobalSubtitleState.IsAnySubtitlePlaying = true;

        if (player != null)
        {
            player.canControl = false;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // ★追加：プレイヤーの音を消す
        SetPlayerAudioMute(true);

        yield return StartCoroutine(ShowImagesRoutine(startSubtitleImages));

        isChoiceMade = false;
        if (choicePanel != null) choicePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return new WaitUntil(() => isChoiceMade);

        if (choicePanel != null) choicePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!isYesChosen)
        {
            SetPlayerAudioMute(false); // 音を戻す
            if (player != null) player.canControl = true;
            GlobalSubtitleState.IsAnySubtitlePlaying = false;
            yield break;
        }

        player.PlayItemSwing();
        StartCoroutine(PlayForSeconds());
        yield return new WaitForSeconds(0.9f);

        // 暗転
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

        // ★修正：生成ではなく、シーン上の鍵を表示させる
        if (bloodlumpObj != null) Destroy(bloodlumpObj);
        if (sceneKeyObject != null) sceneKeyObject.SetActive(true);

        player.inventoryManager.RemoveItem(detergentName);
        player.UpdateItemModel();

        // 音声シーケンス
        if (eventAudioSource != null)
        {
            eventAudioSource.volume = 1f;
            if (firstSound != null)
            {
                eventAudioSource.PlayOneShot(firstSound);
                yield return new WaitForSeconds(firstSound.length);
            }

            if (secondSound != null)
            {
                eventAudioSource.clip = secondSound;
                eventAudioSource.Play();
                yield return new WaitForSeconds(secondSoundDuration);

                float fadeElapsed = 0f;
                while (fadeElapsed < audioFadeDuration)
                {
                    fadeElapsed += Time.deltaTime;
                    eventAudioSource.volume = Mathf.Lerp(1f, 0f, fadeElapsed / audioFadeDuration);
                    yield return null;
                }
                eventAudioSource.Stop();
            }
        }
        else yield return new WaitForSeconds(1.0f);

        // 明転
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

        yield return StartCoroutine(ShowImagesRoutine(endSubtitleImages));

        // 音を戻す
        SetPlayerAudioMute(false);

        if (player != null) player.canControl = true;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
    }

    private void SetPlayerAudioMute(bool isMuted)
    {
        if (player != null)
        {
            AudioSource[] audios = player.GetComponentsInChildren<AudioSource>(true);
            foreach (AudioSource audio in audios)
            {
                if (isMuted)
                {
                    if (!originalVolumes.ContainsKey(audio)) originalVolumes[audio] = audio.volume;
                    audio.Pause();
                    audio.volume = 0f;
                }
                else
                {
                    if (originalVolumes.ContainsKey(audio)) audio.volume = originalVolumes[audio];
                    audio.UnPause();
                }
            }
        }
    }

    IEnumerator ShowImagesRoutine(Image[] images)
    {
        if (images == null) yield break;
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
                currentImage.fillAmount = (characterCount > 0) ? Mathf.Floor(progress * characterCount) / characterCount : progress;
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
                    SetAlpha(currentImage, Mathf.Lerp(1f, 0f, timer / fadeDuration));
                    yield return null;
                }
            }
            SetAlpha(currentImage, 0f);
            currentImage.gameObject.SetActive(false);
            if (i < images.Length - 1) yield return new WaitForSeconds(delayBetweenSubtitles);
        }
    }

    private void SetAlpha(Image img, float alpha) { if (img != null) { Color c = img.color; c.a = alpha; img.color = c; } }
    IEnumerator PlayForSeconds() { ps.Play(); yield return new WaitForSeconds(bubble_duration); ps.Stop(false); }
}