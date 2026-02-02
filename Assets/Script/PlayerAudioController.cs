using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour
{
    [Header("--- 足音設定 ---")]
    public AudioSource footstepAudioSource;

    [Header("歩く際のAudio")]
    public AudioClip walkSoundLoop;

    [Header("走る際のAudio")]
    public AudioClip runSoundLoop;

    [Header("--- 吐息設定 ---")]
    public AudioSource breathingAudioSource;

    [Header("吐息のAudio")]
    public AudioClip breathingSoundLoop;

    [Header("歩いてるときの吐息音量")]
    [Range(0f, 1f)]
    public float breathingWalkVolume = 0.3f;

    [Header("走ってるときの吐息音量")]
    [Range(0f, 1f)]
    public float breathingRunVolume = 0.5f;

    [Header("共通オーディオ設定")]
    public float audioFadeSpeed = 5.0f;

    void Start()
    {
        // 足音の初期化
        if (footstepAudioSource == null)
            footstepAudioSource = GetComponent<AudioSource>();

        footstepAudioSource.loop = true;
        footstepAudioSource.volume = 0;

        // 吐息の初期化
        if (breathingAudioSource != null && breathingSoundLoop != null)
        {
            breathingAudioSource.clip = breathingSoundLoop;
            breathingAudioSource.loop = true;
            breathingAudioSource.volume = 0;
            breathingAudioSource.Play();
        }
    }

    void OnDisable()
    {
        if (footstepAudioSource != null) footstepAudioSource.Stop();
        if (breathingAudioSource != null) breathingAudioSource.Stop();
    }

    // PlayerControllerから毎フレーム呼ばれる更新処理
    public void UpdateAudio(bool isMoving, bool isRunning)
    {
        HandleFootsteps(isMoving, isRunning);
        HandleBreathing(isMoving, isRunning);
    }

    // 足音の処理
    void HandleFootsteps(bool isMoving, bool isRunning)
    {
        if (isMoving)
        {
            AudioClip targetClip = isRunning ? runSoundLoop : walkSoundLoop;

            if (footstepAudioSource.clip != targetClip)
            {
                footstepAudioSource.clip = targetClip;
                footstepAudioSource.time = 0;
                footstepAudioSource.Play();
            }
            else
            {
                if (!footstepAudioSource.isPlaying) footstepAudioSource.Play();
            }
            footstepAudioSource.volume = Mathf.Lerp(footstepAudioSource.volume, 1.0f, Time.deltaTime * audioFadeSpeed);
        }
        else
        {
            if (footstepAudioSource.isPlaying)
            {
                footstepAudioSource.volume = Mathf.Lerp(footstepAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
                if (footstepAudioSource.volume < 0.01f)
                {
                    footstepAudioSource.Pause();
                    footstepAudioSource.volume = 0;
                }
            }
        }
    }

    // 吐息の処理
    void HandleBreathing(bool isMoving, bool isRunning)
    {
        if (breathingAudioSource == null) return;

        if (isMoving)
        {
            if (!breathingAudioSource.isPlaying) breathingAudioSource.Play();

            float targetVolume = isRunning ? breathingRunVolume : breathingWalkVolume;
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, targetVolume, Time.deltaTime * audioFadeSpeed);
        }
        else
        {
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
        }
    }

    // 操作不能時などに音をフェードアウトさせる
    public void FadeOutAudio()
    {
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.volume = Mathf.Lerp(footstepAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
            if (footstepAudioSource.volume < 0.01f)
            {
                footstepAudioSource.Pause();
                footstepAudioSource.volume = 0;
            }
        }
        if (breathingAudioSource != null && breathingAudioSource.isPlaying)
        {
            breathingAudioSource.volume = Mathf.Lerp(breathingAudioSource.volume, 0.0f, Time.deltaTime * audioFadeSpeed);
        }
    }
}