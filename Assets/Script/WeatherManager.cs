using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("時間設定（秒）")]
    [Tooltip("次の雨が降るまでの最小時間（例: 300秒 = 5分）")]
    public float minRainInterval = 300f; // 5分
    [Tooltip("次の雨が降るまでの最大時間（例: 600秒 = 10分）")]
    public float maxRainInterval = 600f; // 10分

    [Tooltip("雨が降り続く最小時間（例: 180秒 = 3分）")]
    public float minRainDuration = 180f; // 3分
    [Tooltip("雨が降り続く最大時間（例: 300秒 = 5分）")]
    public float maxRainDuration = 300f; // 5分

    [Header("雷の設定")]
    [Tooltip("雨が降った時に、雷も発生する確率（0.0 〜 1.0）")]
    [Range(0f, 1f)]
    public float thunderProbability = 0.35f; // 35%

    [Header("参照")]
    public ParticleSystem rainParticle;
    public AudioSource rainAudio;
    public LightningManager lightningManager;

    // 内部変数
    private float stateTimer;      // 現在の状態の残り時間
    private bool isRaining = false; // 現在雨が降っているか

    void Start()
    {
        // ゲーム開始時は晴れからスタートし、次の雨までの時間をセット
        isRaining = false;
        StopRain();
        SetNextRainTime();
    }

    void Update()
    {
        // タイマーを進める（残り時間を減らす）
        stateTimer -= Time.deltaTime;

        // 時間が来たら状態を切り替える
        if (stateTimer <= 0f)
        {
            if (isRaining)
            {
                // 雨が降っていた場合 -> 雨を止めて、次の晴れ時間をセット
                Debug.Log("雨が止みました。次の雨まで待機します。");
                StopRain();
                SetNextRainTime();
            }
            else
            {
                // 晴れていた場合 -> 雨を降らせて、降る時間をセット
                Debug.Log("雨が降り始めました！");
                StartRain();
                SetRainDuration();
            }
        }
    }

    // 次の雨までの待機時間（晴れの時間）をセット
    void SetNextRainTime()
    {
        isRaining = false;
        // 5分〜10分の間でランダムな時間を設定
        stateTimer = Random.Range(minRainInterval, maxRainInterval);
        Debug.Log($"次の雨まであと {stateTimer} 秒（約 {stateTimer / 60:F1} 分）");
    }

    // 雨が降り続く時間をセット
    void SetRainDuration()
    {
        isRaining = true;
        // 3分〜5分の間でランダムな時間を設定
        stateTimer = Random.Range(minRainDuration, maxRainDuration);
        Debug.Log($"この雨は {stateTimer} 秒間続きます（約 {stateTimer / 60:F1} 分）");
    }

    void StartRain()
    {
        // パーティクルと音を再生
        if (rainParticle != null && !rainParticle.isPlaying) rainParticle.Play();
        if (rainAudio != null && !rainAudio.isPlaying) rainAudio.Play();

        // 雷の抽選（今まで通りの確率）
        if (Random.value < thunderProbability)
        {
            if (lightningManager != null)
            {
                lightningManager.isThunderActive = true;
                Debug.Log("天気：雷も発生します！");
            }
        }
        else
        {
            // 雷はなし（念のためOFF）
            if (lightningManager != null)
            {
                lightningManager.isThunderActive = false;
            }
        }
    }

    void StopRain()
    {
        // パーティクルと音を停止
        if (rainParticle != null && rainParticle.isPlaying) rainParticle.Stop();
        if (rainAudio != null && rainAudio.isPlaying) rainAudio.Stop();

        // 雨が止んだら雷も確実に止める
        if (lightningManager != null)
        {
            lightningManager.isThunderActive = false;
        }
    }
}