using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RoomLightController : MonoBehaviour
{
    [Header("管理する全てのライト")]
    [SerializeField] private RoomLight[] allRoomLights;

    [Header("ランダム設定")]
    [Tooltip("同時に赤くなるライトの個数")]
    [SerializeField] private int activeRedLightCount = 1;

    [Tooltip("赤ランプが維持される時間の最小値（秒）")]
    [SerializeField] private float minDuration = 60f; // 1分

    [Tooltip("赤ランプが維持される時間の最大値（秒）")]
    [SerializeField] private float maxDuration = 180f; // 3分

    [Header("演出設定")]
    [Tooltip("切り替わる前の点滅時間（秒）")]
    [SerializeField] private float flashDuration = 2.0f;

    [Tooltip("次の赤ランプに切り替わるまでの待機時間（秒）")]
    [SerializeField] private float resetDelay = 3.0f;

    [Header("オーディオ設定")] // ★ここを追加
    [Tooltip("点滅する瞬間に再生される音（カチッ、ジジッなど）")]
    [SerializeField] private AudioClip flickerSound;

    private AudioSource audioSource; // ★ここを追加

    private void Start()
    {
        // AudioSourceを取得
        audioSource = GetComponent<AudioSource>();

        // サイクルを開始
        StartCoroutine(LightCycleRoutine());
    }

    private IEnumerator LightCycleRoutine()
    {
        // 最初は全部ノーマルにしておく
        foreach (var light in allRoomLights)
        {
            light.SetNormal();
        }

        bool isFirstTime = true;

        while (true)
        {
            List<RoomLight> nextRedLights = GetUniqueRandomLights(activeRedLightCount);

            if (!isFirstTime)
            {
                // 消灯演出
                foreach (var light in nextRedLights)
                {
                    light.ToggleLight(false);
                }
                yield return new WaitForSeconds(resetDelay);
            }

            foreach (var light in nextRedLights)
            {
                light.SetRed();
            }

            isFirstTime = false;

            float waitTime = Random.Range(minDuration, maxDuration);
            yield return new WaitForSeconds(waitTime);

            // --- 点滅処理 (2秒間) ---
            float flashTimer = 0f;
            bool isLightOn = true;

            while (flashTimer < flashDuration)
            {
                isLightOn = !isLightOn;

                // ライトの切り替え
                foreach (var light in nextRedLights)
                {
                    light.ToggleLight(isLightOn);
                }

                // 音が設定されていれば再生
                if (flickerSound != null)
                {
                    // 音程を少しランダムにするとホラー感が増します
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(flickerSound);
                }

                yield return new WaitForSeconds(0.1f); // 0.1秒間隔
                flashTimer += 0.1f;
            }

            foreach (var light in nextRedLights)
            {
                light.SetNormal();
            }
        }
    }
    
    private List<RoomLight> GetUniqueRandomLights(int count)
    {
        List<RoomLight> result = new List<RoomLight>();
        List<RoomLight> tempPool = new List<RoomLight>(allRoomLights);
        int loopCount = Mathf.Min(count, tempPool.Count);

        for (int i = 0; i < loopCount; i++)
        {
            int randomIndex = Random.Range(0, tempPool.Count);
            result.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
        return result;
    }
}