using System.Collections;
using System.Collections.Generic; // ★追加
using UnityEngine;
using UnityEngine.UI;

public static class GlobalSubtitleState
{
    public static bool IsAnySubtitlePlaying = false;
}

[System.Serializable]
public class SpecialSubtitleData
{
    public Image subtitleImage;
    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
}

[System.Serializable]
public class NormalInteractData
{
    public string eventID;
    public Collider triggerArea;
    public Image[] subtitleImages;
    public bool playOnlyOnce = false;
    [HideInInspector] public bool hasPlayed = false;
}

[System.Serializable]
public class SpecialInteractData
{
    public string eventID;
    public Collider triggerArea;
    public SpecialSubtitleData[] subtitles;
    public bool playOnlyOnce = false;
    public Transform objectToMove;
    public Vector3 moveOffset;
    public float moveDuration = 2.0f;
    public Collider[] collidersToDisable;
    [HideInInspector] public bool hasPlayed = false;
}

public class InteractSubtitleManager : MonoBehaviour
{
    public static List<string> clearedInteractEvents = new List<string>();

    public NormalInteractData[] normalInteractPoints;
    public SpecialInteractData specialInteractPoint;

    public float duration = 0.8f;
    public int characterCount = 8;
    public float displayTime = 1.0f;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    public GameObject interactPromptUI;
    public PlayerController playerController;

    private NormalInteractData currentNormalData = null;
    private SpecialInteractData currentSpecialData = null;
    private bool isAnimating = false;

    // ★追加：元の音量を記憶するリスト
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

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

        if (specialInteractPoint != null && specialInteractPoint.triggerArea != null)
        {
            if (!string.IsNullOrEmpty(specialInteractPoint.eventID) && clearedInteractEvents.Contains(specialInteractPoint.eventID))
            {
                if (specialInteractPoint.objectToMove != null && specialInteractPoint.moveOffset != Vector3.zero) specialInteractPoint.objectToMove.position = specialInteractPoint.objectToMove.position + specialInteractPoint.moveOffset;
                if (specialInteractPoint.collidersToDisable != null)
                {
                    foreach (Collider col in specialInteractPoint.collidersToDisable) if (col != null) col.enabled = false;
                }
                specialInteractPoint.triggerArea.enabled = false;
                specialInteractPoint.hasPlayed = true;
                gameObject.SetActive(false);
                return;
            }
            else
            {
                specialInteractPoint.triggerArea.isTrigger = true;
                SubtitleTriggerHandler handler = specialInteractPoint.triggerArea.gameObject.AddComponent<SubtitleTriggerHandler>();
                handler.manager = this;
                handler.specialData = specialInteractPoint;

                if (specialInteractPoint.subtitles != null)
                {
                    foreach (var sub in specialInteractPoint.subtitles)
                    {
                        if (sub.subtitleImage != null)
                        {
                            sub.subtitleImage.type = Image.Type.Filled;
                            sub.subtitleImage.fillMethod = Image.FillMethod.Horizontal;
                            sub.subtitleImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                            sub.subtitleImage.fillAmount = 0f;
                            sub.subtitleImage.gameObject.SetActive(false);
                            SetAlpha(sub.subtitleImage, 1f);
                        }
                    }
                }
            }
        }
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
        if (GlobalSubtitleState.IsAnySubtitlePlaying && !isAnimating)
        {
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
            return;
        }

        if (!isAnimating && !GlobalSubtitleState.IsAnySubtitlePlaying)
        {
            bool promptShouldBeActive = false;

            if (currentNormalData != null)
            {
                if (!currentNormalData.playOnlyOnce || !currentNormalData.hasPlayed)
                {
                    promptShouldBeActive = true;
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (interactPromptUI != null) interactPromptUI.SetActive(false);
                        StartCoroutine(PlayNormalSequence(currentNormalData));
                        return;
                    }
                }
            }

            if (currentSpecialData != null)
            {
                if (!currentSpecialData.playOnlyOnce || !currentSpecialData.hasPlayed)
                {
                    promptShouldBeActive = true;
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (interactPromptUI != null) interactPromptUI.SetActive(false);
                        StartCoroutine(PlaySpecialSequence(currentSpecialData));
                        return;
                    }
                }
            }

            if (interactPromptUI != null)
            {
                if (promptShouldBeActive && !interactPromptUI.activeSelf) interactPromptUI.SetActive(true);
                else if (!promptShouldBeActive && interactPromptUI.activeSelf) interactPromptUI.SetActive(false);
            }
        }
    }

    public void OnPlayerEnterNormal(NormalInteractData data) { if (data.playOnlyOnce && data.hasPlayed) return; currentNormalData = data; }
    public void OnPlayerExitNormal(NormalInteractData data) { if (currentNormalData == data) { currentNormalData = null; if (interactPromptUI != null && currentSpecialData == null) interactPromptUI.SetActive(false); } }
    public void OnPlayerEnterSpecial(SpecialInteractData data) { if (data.playOnlyOnce && data.hasPlayed) return; currentSpecialData = data; }
    public void OnPlayerExitSpecial(SpecialInteractData data) { if (currentSpecialData == data) { currentSpecialData = null; if (interactPromptUI != null && currentNormalData == null) interactPromptUI.SetActive(false); } }

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

        SetPlayerAudioMute(true); // ★音を消す

        yield return StartCoroutine(ShowImagesRoutine(data.subtitleImages));

        data.hasPlayed = true;
        if (data.playOnlyOnce && !string.IsNullOrEmpty(data.eventID))
        {
            if (!clearedInteractEvents.Contains(data.eventID)) clearedInteractEvents.Add(data.eventID);
            if (data.triggerArea != null) data.triggerArea.enabled = false;
        }

        SetPlayerAudioMute(false); // ★音を戻す

        if (playerController != null) playerController.canControl = true;

        isAnimating = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
        if (currentNormalData == data && data.playOnlyOnce) currentNormalData = null;
    }

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

        SetPlayerAudioMute(true); // ★音を消す

        yield return StartCoroutine(ShowSpecialImagesRoutine(data.subtitles));

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

        if (data.collidersToDisable != null)
        {
            foreach (Collider col in data.collidersToDisable) if (col != null) col.enabled = false;
        }

        data.hasPlayed = true;
        if (data.playOnlyOnce && !string.IsNullOrEmpty(data.eventID))
        {
            if (!clearedInteractEvents.Contains(data.eventID)) clearedInteractEvents.Add(data.eventID);
            if (data.triggerArea != null) data.triggerArea.enabled = false;
        }

        SetPlayerAudioMute(false); // ★音を戻す

        if (playerController != null) playerController.canControl = true;

        isAnimating = false;
        GlobalSubtitleState.IsAnySubtitlePlaying = false;
        if (currentSpecialData == data && data.playOnlyOnce) currentSpecialData = null;

        gameObject.SetActive(false);
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
                if (i < images.Length - 1) yield return new WaitForSeconds(delayBetweenSubtitles);
            }
        }
    }

    IEnumerator ShowSpecialImagesRoutine(SpecialSubtitleData[] subs)
    {
        if (subs != null && subs.Length > 0)
        {
            for (int i = 0; i < subs.Length; i++)
            {
                SpecialSubtitleData currentData = subs[i];
                Image currentImage = currentData.subtitleImage;
                if (currentImage == null) continue;

                currentImage.gameObject.SetActive(true);
                currentImage.fillAmount = 0f;
                SetAlpha(currentImage, 1f);

                float timer = 0f;
                while (timer < currentData.duration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / currentData.duration;
                    if (currentData.characterCount > 0) currentImage.fillAmount = Mathf.Floor(progress * currentData.characterCount) / currentData.characterCount;
                    else currentImage.fillAmount = progress;
                    yield return null;
                }
                currentImage.fillAmount = 1.0f;
                yield return new WaitForSeconds(currentData.displayTime);

                if (i == subs.Length - 1)
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
                if (i < subs.Length - 1) yield return new WaitForSeconds(delayBetweenSubtitles);
            }
        }
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img != null) { Color c = img.color; c.a = alpha; img.color = c; }
    }

    // ★超強力版ミュート関数
    private void SetPlayerAudioMute(bool isMuted)
    {
        if (playerController != null)
        {
            AudioSource[] audios = playerController.GetComponentsInChildren<AudioSource>(true);
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
}

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