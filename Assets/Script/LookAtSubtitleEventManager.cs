using System.Collections;
using System.Collections.Generic; // ★追加
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LookAtSubtitleData
{
    public Image subtitleImage;
    public float duration = 2.0f;
    public int characterCount = 8;
    public float displayTime = 3.0f;
}

public class LookAtSubtitleEventManager : MonoBehaviour
{
    public Transform lookTarget;
    public float turnSpeed = 3.0f;
    public string eventID;

    public LookAtSubtitleData[] subtitles;
    public float fadeDuration = 1.0f;
    public float delayBetweenSubtitles = 0.5f;

    public PlayerController playerController;

    private bool hasTriggered = false;
    public static List<string> clearedLookEvents = new List<string>();

    // ★追加：元の音量を記憶するリスト
    private Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

    void Start()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();

        if (!string.IsNullOrEmpty(eventID) && clearedLookEvents.Contains(eventID))
        {
            hasTriggered = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            return;
        }

        if (subtitles != null)
        {
            foreach (var data in subtitles)
            {
                if (data.subtitleImage != null)
                {
                    data.subtitleImage.type = Image.Type.Filled;
                    data.subtitleImage.fillMethod = Image.FillMethod.Horizontal;
                    data.subtitleImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                    data.subtitleImage.fillAmount = 0f;
                    data.subtitleImage.gameObject.SetActive(false);
                    SetAlpha(data.subtitleImage, 1f);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            if (GlobalSubtitleState.IsAnySubtitlePlaying) return;
            hasTriggered = true;
            StartCoroutine(PlayLookAtAndSubtitle());
        }
    }

    IEnumerator PlayLookAtAndSubtitle()
    {
        GlobalSubtitleState.IsAnySubtitlePlaying = true;
        Time.timeScale = 0f;

        if (playerController != null)
        {
            playerController.canControl = false;
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        // ★強力版ミュート実行
        SetPlayerAudioMute(true);

        if (lookTarget != null && playerController != null)
        {
            Transform playerTransform = playerController.transform;
            Transform camTransform = playerController.cam.transform;

            Vector3 direction = lookTarget.position - playerTransform.position;
            direction.y = 0;
            Quaternion targetBodyRot = playerTransform.rotation;
            if (direction != Vector3.zero) targetBodyRot = Quaternion.LookRotation(direction);

            Vector3 camDirection = lookTarget.position - camTransform.position;
            Quaternion targetCamLookRot = Quaternion.LookRotation(camDirection);
            Vector3 camEuler = targetCamLookRot.eulerAngles;
            Quaternion targetCamRot = Quaternion.Euler(camEuler.x, 0, 0);

            Quaternion startBodyRot = playerTransform.rotation;
            Quaternion startCamRot = camTransform.localRotation;

            float t = 0f;
            while (t < 1.0f)
            {
                t += Time.unscaledDeltaTime * turnSpeed;
                playerTransform.rotation = Quaternion.Slerp(startBodyRot, targetBodyRot, t);
                camTransform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
                playerController.SyncRotationToCurrent();
                yield return null;
            }

            playerTransform.rotation = targetBodyRot;
            camTransform.localRotation = targetCamRot;
            playerController.SyncRotationToCurrent();
        }

        if (subtitles != null && subtitles.Length > 0)
        {
            for (int i = 0; i < subtitles.Length; i++)
            {
                LookAtSubtitleData currentData = subtitles[i];
                Image currentImage = currentData.subtitleImage;
                if (currentImage == null) continue;

                currentImage.gameObject.SetActive(true);
                currentImage.fillAmount = 0f;
                SetAlpha(currentImage, 1f);

                float timer = 0f;
                while (timer < currentData.duration)
                {
                    timer += Time.unscaledDeltaTime;
                    float progress = timer / currentData.duration;
                    if (currentData.characterCount > 0) currentImage.fillAmount = Mathf.Floor(progress * currentData.characterCount) / currentData.characterCount;
                    else currentImage.fillAmount = progress;
                    yield return null;
                }

                currentImage.fillAmount = 1.0f;
                yield return new WaitForSecondsRealtime(currentData.displayTime);

                if (i == subtitles.Length - 1)
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

                if (i < subtitles.Length - 1) yield return new WaitForSecondsRealtime(delayBetweenSubtitles);
            }
        }

        if (!string.IsNullOrEmpty(eventID) && !clearedLookEvents.Contains(eventID))
        {
            clearedLookEvents.Add(eventID);
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Time.timeScale = 1f;

        // ★強力版ミュート解除
        SetPlayerAudioMute(false);

        if (playerController != null)
        {
            playerController.canControl = true;
        }

        GlobalSubtitleState.IsAnySubtitlePlaying = false;
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