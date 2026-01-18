using UnityEngine;
using System.Collections;

public class LightningManager : MonoBehaviour
{
    [Header("状態確認（自動で変わります）")]
    public bool isThunderActive = false; // これがONの時だけ雷が落ちる

    [Header("設定")]
    [Tooltip("最小発生間隔（秒）")] public float minDelay = 10f;
    [Tooltip("最大発生間隔（秒）")] public float maxDelay = 30f;

    [Header("参照")]
    public Light flashLight;
    public ParticleSystem boltParticle;
    public AudioSource thunderAudioSource;
    public AudioClip[] thunderSounds;

    private float nextLightningTime;

    void Start()
    {
        // ゲーム開始時に、閃光ライトを強制的にOFFにする
        if (flashLight != null)
        {
            flashLight.enabled = false;
        }

        // 雷システム自体もOFFからスタート
        isThunderActive = false;
        ScheduleNextLightning();
    }

    void Update()
    {
        // スイッチがOFFなら何もしない
        if (!isThunderActive) return;

        // 時間が来たら雷を発生させる
        if (Time.time >= nextLightningTime)
        {
            StartCoroutine(Strike());
            ScheduleNextLightning();
        }

        // テスト用：Lキー
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(Strike());
        }
    }

    void ScheduleNextLightning()
    {
        nextLightningTime = Time.time + Random.Range(minDelay, maxDelay);
    }

    IEnumerator Strike()
    {
        if (boltParticle != null)
        {
            boltParticle.transform.rotation = Quaternion.Euler(Random.Range(-30, 30), Random.Range(0, 360), 0);
            boltParticle.Play();
        }

        // ピカッと光る処理
        if (flashLight != null)
        {
            flashLight.enabled = true;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
            flashLight.enabled = false;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));

            flashLight.intensity /= 2;
            flashLight.enabled = true;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
            flashLight.enabled = false;
            flashLight.intensity *= 2;
        }

        // 音の処理
        if (thunderAudioSource != null && thunderSounds.Length > 0)
        {
            float delay = Random.Range(0.5f, 2.0f);
            yield return new WaitForSeconds(delay);
            AudioClip clip = thunderSounds[Random.Range(0, thunderSounds.Length)];
            thunderAudioSource.PlayOneShot(clip);
        }
    }
}