using TMPro;
using UnityEngine;
using UnityEngine.UI; // UIに時間を表示したい場合に必要

public class TimeManager : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("現実の何秒で、ゲーム内の1時間が経過するか（今回は60秒に設定）")]
    public float realSecondsPerGameHour = 60f;

    [Tooltip("ゲーム開始時の時間（例：8 = 朝8時）")]
    public float startHour = 8f;

    [Header("日付・月齢設定")]
    [Tooltip("現在の経過日数")]
    public int currentDay = 1;
    [Tooltip("月の満ち欠けの周期（ゲーム内日数で何日で一周するか）")]
    public float moonPhaseLength = 30f; // 30日で一周

    [Header("割り当て")]
    [Tooltip("太陽となるDirectional Light")]
    public Light sunLight;
    public Light moonLight;
    public Light moonSunLight;

    [Header("空のマテリアル設定")]
    public Material daySkybox;   // 昼用の空
    public Material nightSkybox; // 夜用の空

    [Header("色の設定（昼と夜の雰囲気）")]
    public Color dayFogColor = new Color(0.5f, 0.6f, 0.7f); // 昼の霧（空色）
    public Color nightFogColor = new Color(0.05f, 0.05f, 0.2f); // 夜の霧（深い紺色）

    public Color dayAmbient = new Color(0.8f, 0.8f, 0.8f); // 昼の環境光（明るい）
    public Color nightAmbient = new Color(0.1f, 0.1f, 0.25f); // 夜の環境光（暗い青）

    [Header("昼夜サイクル停止")]
    public bool stopTime = false;

    [Header("確認用（自動で動きます）")]
    [Range(0, 24)]
    public float currentHour; // 現在のゲーム内時間（0.0 〜 24.0）

    void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            // セーブデータ内の時間を使う
            currentHour = SaveManager.Instance.currentData.currentHour;
            Debug.Log("セーブデータから時間を復元しました: " + currentHour);
        }
        else
        {
            // データがなければデフォルト時間を使う
            currentHour = startHour;
        }

        // 霧を有効にする
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.005f;
    }

    void Update()
    {
        if (!stopTime)
        {
            currentHour += Time.deltaTime / realSecondsPerGameHour;

            if (currentHour >= 24f)
            {
                currentHour = 0f;
                currentDay++; // 日付を進める
            }

            //currentHour %= 24; // 24時を超えたら0時に戻す
        }

        UpdateLighting(); // 光の計算
        UpdateSkybox();
        UpdateMoonPhase();

        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            SaveManager.Instance.currentData.currentHour = currentHour;
        }

        // セーブ連携：現在の時間を常にSaveManagerに送る
        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            SaveManager.Instance.currentData.currentHour = currentHour;
        }
    }

    void UpdateLighting()
    {
        // ※太陽が真上に来る時間を調整するための計算
        float timePercent = currentHour / 24f;

        // --- 1. 太陽と月の回転 ---
        // 太陽：朝6時に出て、夜18時に沈む動き
        float sunAngle = (timePercent * 360f) - 90f;

        // 太陽の回転
        if (sunLight != null)
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // 月の回転（太陽の真逆 180度ズラす）
        if (moonLight != null)
            moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);


        // 明るさ（Intensity）の調整
        // 昼間(6時〜18時)かどうか
        bool isDay = (currentHour >= 6f && currentHour < 18f);

        // 滑らかに切り替えるための「フェード値」を計算
        float intensityMultiplier = 1f;
        if (currentHour <= 6f || currentHour >= 18f) intensityMultiplier = 0f; // 夜は0
        else if (currentHour < 7f) intensityMultiplier = (currentHour - 6f); // 6-7時で徐々に明るく
        else if (currentHour > 17f) intensityMultiplier = (18f - currentHour); // 17-18時で徐々に暗く

        // 太陽の強さ
        if (sunLight != null) sunLight.intensity = intensityMultiplier;

        // 月の強さ（太陽と逆）
        if (moonLight != null) moonLight.intensity = (1f - intensityMultiplier) * 0.5f; // 月は最大0.5の強さ


        // 環境光とフォグの色を混ぜる

        // 環境光（影の色）
        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, intensityMultiplier);

        // フォグ（遠くの色）
        RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, intensityMultiplier);

        // Skyboxの色味も少し変える（オプション）
        RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(0.2f, 1f, intensityMultiplier));
    }

    void UpdateSkybox()
    {
        if (daySkybox == null || nightSkybox == null) return;

        // 色の濃さ（Exposure/Tint）を計算する変数
        float blendFactor = 0f;

        // 時間帯による切り替えロジック

        // 朝 (5:00 - 7:00) : 夜空を消して、昼空を出す
        if (currentHour >= 5f && currentHour < 7f)
        {
            if (RenderSettings.skybox != daySkybox) RenderSettings.skybox = daySkybox; // 昼空セット

            // 5時〜6時はまだ暗い、6時〜7時で明るくする
            if (currentHour > 6f) blendFactor = (currentHour - 6f); // 0 -> 1
            else blendFactor = 0f; // まだ真っ黒
        }
        // 昼 (7:00 - 17:00) : ずっと昼空
        else if (currentHour >= 7f && currentHour < 17f)
        {
            if (RenderSettings.skybox != daySkybox) RenderSettings.skybox = daySkybox;
            blendFactor = 1f;
        }
        // 夕方 (17:00 - 19:00) : 昼空を暗くする
        else if (currentHour >= 17f && currentHour < 19f)
        {
            if (RenderSettings.skybox != daySkybox) RenderSettings.skybox = daySkybox;

            // 17時〜18時で暗くしていく
            if (currentHour < 18f) blendFactor = (18f - currentHour); // 1 -> 0
            else blendFactor = 0f; // 18時以降は真っ黒
        }
        // 夜 (19:00 - 5:00) : 夜空を出す
        else
        {
            if (RenderSettings.skybox != nightSkybox) RenderSettings.skybox = nightSkybox; // 夜空セット

            // 夜のフェードイン・アウト
            // 19:00 - 20:00 : 星空をフェードイン
            if (currentHour >= 19f && currentHour < 20f) blendFactor = (currentHour - 19f);
            // 4:00 - 5:00 : 星空をフェードアウト
            else if (currentHour >= 4f && currentHour < 5f) blendFactor = (5f - currentHour);
            // それ以外の深夜帯
            else blendFactor = 1f;
        }
    }

    void UpdateMoonPhase()
    {
        if (moonSunLight == null) return;

        // 1. 現在の「月齢（0〜1）」を計算
        // (現在の日数 + 今日の進み具合) ÷ 30日周期
        float phaseProgress = (currentDay + (currentHour / 24f)) % moonPhaseLength;
        float phasePercent = phaseProgress / moonPhaseLength;

        // 2. ライトの角度を計算（360度回す）
        float angle = phasePercent * 360f;

        // 3. ライトに適用
        // 親の回転（空の回転）に加えて、この角度を足す
        moonSunLight.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
    }

    void OnApplicationQuit()
    {
        if (daySkybox != null) daySkybox.SetColor("_Tint", Color.white);
        if (nightSkybox != null) nightSkybox.SetColor("_Tint", Color.white);
    }
}