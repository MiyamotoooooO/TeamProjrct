using UnityEngine;

public class TitleBGM : MonoBehaviour
{
    [Header("タイトルBGM")]
    public AudioSource bgmSource;

    void Start()
    {
        // AudioSourceが設定されていない場合は自動取得
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
        }

        // BGM再生
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    void OnDestroy()
    {
        // シーンが切り替わるとこのオブジェクトが破棄される
        // その時にBGMを停止
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }
}