using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("ゲーム内の1日は、現実時間で何秒？")]
    public float dayDuration = 60f;

    [Tooltip("雨が降る確率（0.0 〜 1.0）")]
    [Range(0f, 1f)]
    public float rainProbability = 0.2f; // 20%

    [Tooltip("【追加】雨が降った時に、雷も発生する確率")]
    [Range(0f, 1f)]
    public float thunderProbability = 0.35f; // ★35%

    [Header("参照")]
    public ParticleSystem rainParticle;
    public AudioSource rainAudio;

    [Tooltip("【追加】雷マネージャーをここに入れる")]
    public LightningManager lightningManager; // ★ここが追加

    // 内部変数
    private float timer;
    private bool isRaining = false;

    void Start()
    {
        timer = 0f;
        // 開始時はすべてOFFにする
        StopRain();
        CheckWeather();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= dayDuration)
        {
            timer = 0f;
            CheckWeather();
        }
    }

    void CheckWeather()
    {
        // 1. まず雨が降るか抽選 (20%)
        if (Random.value < rainProbability)
        {
            StartRain();
            Debug.Log("天気：雨が降ります。");

            // 2. 雨なら、さらに雷の抽選を行う (35%)
            if (Random.value < thunderProbability)
            {
                if (lightningManager != null)
                {
                    lightningManager.isThunderActive = true;
                    Debug.Log("天気：雷も鳴ります！(35%当選)");
                }
            }
            else
            {
                // 雷はハズレ（ただの雨）
                if (lightningManager != null)
                {
                    lightningManager.isThunderActive = false;
                }
            }
        }
        else
        {
            StopRain();
            Debug.Log("天気：晴れです。");
        }
    }

    void StartRain()
    {
        isRaining = true;
        if (rainParticle != null && !rainParticle.isPlaying) rainParticle.Play();
        if (rainAudio != null && !rainAudio.isPlaying) rainAudio.Play();
    }

    void StopRain()
    {
        isRaining = false;
        if (rainParticle != null && rainParticle.isPlaying) rainParticle.Stop();
        if (rainAudio != null && rainAudio.isPlaying) rainAudio.Stop();

        // ★雨が止んだら雷も絶対に止める
        if (lightningManager != null)
        {
            lightningManager.isThunderActive = false;
        }
    }
}