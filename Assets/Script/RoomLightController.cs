using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RoomLightController : MonoBehaviour
{
    // 部屋ごとにライトをまとめるための「グループ」機能
    [System.Serializable]
    public class LightGroup
    {
        public string roomName = "部屋の名前"; // Inspectorで分かりやすく整理するための名前
        public RoomLight[] lightsInRoom;       // この部屋に所属するライト達
    }

    [Header("管理する部屋（ライトのグループ）")]
    [SerializeField] private LightGroup[] allLightGroups; // 個別のライトではなく「グループ」を登録する

    [Header("ランダム設定")]
    [Tooltip("同時に赤くなる「部屋」の個数")]
    [SerializeField] private int activeRedRoomCount = 1;

    [Tooltip("赤ランプが維持される時間の最小値（秒）")]
    [SerializeField] private float minDuration = 60f; // 1分

    [Tooltip("赤ランプが維持される時間の最大値（秒）")]
    [SerializeField] private float maxDuration = 180f; // 3分

    [Header("演出設定")]
    [Tooltip("切り替わる前の点滅時間（秒）")]
    [SerializeField] private float flashDuration = 2.0f;

    [Tooltip("次の赤ランプに切り替わるまでの待機時間（秒）")]
    [SerializeField] private float resetDelay = 3.0f;

    [Header("オーディオ設定")]
    [Tooltip("点滅する瞬間に再生される音（カチッ、ジジッなど）")]
    [SerializeField] private AudioClip flickerSound;

    private AudioSource audioSource;

    private void Start()
    {
        // AudioSourceを取得
        audioSource = GetComponent<AudioSource>();

        // サイクルを開始
        StartCoroutine(LightCycleRoutine());
    }

    private IEnumerator LightCycleRoutine()
    {
        // 最初は全部の部屋の全部のライトをノーマルにしておく
        foreach (var group in allLightGroups)
        {
            foreach (var light in group.lightsInRoom)
            {
                light.SetNormal();
            }
        }

        bool isFirstTime = true;

        while (true)
        {
            // ★変更：個別のライトではなく、次に赤くする「部屋（グループ）」をランダムに選ぶ
            List<LightGroup> nextRedGroups = GetUniqueRandomGroups(activeRedRoomCount);

            if (!isFirstTime)
            {
                // 消灯演出
                foreach (var group in nextRedGroups)
                {
                    foreach (var light in group.lightsInRoom)
                    {
                        light.ToggleLight(false);
                    }
                }
                yield return new WaitForSeconds(resetDelay);
            }

            // 選ばれた部屋のライトをすべて赤にする
            foreach (var group in nextRedGroups)
            {
                foreach (var light in group.lightsInRoom)
                {
                    light.SetRed();
                }
            }

            isFirstTime = false;

            float waitTime = Random.Range(minDuration, maxDuration);
            yield return new WaitForSeconds(waitTime);

            // --- 点滅処理 (指定秒数) ---
            float flashTimer = 0f;
            bool isLightOn = true;

            while (flashTimer < flashDuration)
            {
                isLightOn = !isLightOn;

                // 選ばれた部屋のライトを一斉に点滅させる
                foreach (var group in nextRedGroups)
                {
                    foreach (var light in group.lightsInRoom)
                    {
                        light.ToggleLight(isLightOn);
                    }
                }

                // 音が設定されていれば再生
                if (flickerSound != null)
                {
                    audioSource.pitch = Random.Range(0.9f, 1.1f);
                    audioSource.PlayOneShot(flickerSound);
                }

                yield return new WaitForSeconds(0.1f); // 0.1秒間隔
                flashTimer += 0.1f;
            }

            // 点滅終了後、通常に戻す
            foreach (var group in nextRedGroups)
            {
                foreach (var light in group.lightsInRoom)
                {
                    light.SetNormal();
                }
            }
        }
    }

    // ★変更：指定した数の「かぶらない部屋（グループ）」を抽出する関数
    private List<LightGroup> GetUniqueRandomGroups(int count)
    {
        List<LightGroup> result = new List<LightGroup>();
        List<LightGroup> tempPool = new List<LightGroup>(allLightGroups);
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



/*using System.Collections;
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
}*/