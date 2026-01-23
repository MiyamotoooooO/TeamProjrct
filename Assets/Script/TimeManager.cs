using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    [Header("固定時間設定")]
    [Tooltip("固定する時間（22 = 夜の10時）")]
    public float fixedHour = 22.0f;

    [Header("日付・月齢設定")]
    public int currentDay = 1;
    public float moonPhaseLength = 30f;

    [Header("割り当て")]
    public Light sunLight;
    public Light moonLight;
    public Light moonSunLight;

    [Header("空のマテリアル設定")]
    public Material daySkybox;   // (使われませんがエラー防止のため残しています)
    public Material nightSkybox; // ★これを使います

    [Header("色の設定（夜の雰囲気）")]
    // 昼用の色は不要ですが、スクリプトの構造を維持するため残しています
    public Color dayFogColor = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFogColor = new Color(0.05f, 0.05f, 0.2f); // ★夜の霧

    public Color dayAmbient = new Color(0.8f, 0.8f, 0.8f);
    public Color nightAmbient = new Color(0.1f, 0.1f, 0.25f); // ★夜の環境光

    // 確認用
    [Range(0, 24)]
    public float currentHour;

    void Start()
    {
        // ★強制的に夜の時間にする
        currentHour = fixedHour;

        // 霧を有効にする
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.005f;

        // 開始時に一度だけ空と光を更新して、夜の状態にする
        UpdateLighting();
        UpdateSkybox();
        UpdateMoonPhase();
    }

    void Update()
    {
        // ★時間を進める処理を削除しました
        // 念のため、毎フレーム時間を固定値にリセット（インスペクターでいじっても戻るように）
        currentHour = fixedHour;

        // 光や空の更新は、動的な変更がないなら本来Updateになくてもいいですが、
        // ライトの色調整などをリアルタイムにしたい場合に備えて残しておきます。
        UpdateLighting();
        UpdateSkybox();

        // 月の満ち欠け（固定）
        UpdateMoonPhase();

        // セーブデータには「今は夜だ」という情報を送り続ける
        if (SaveManager.Instance != null && SaveManager.Instance.currentData != null)
        {
            SaveManager.Instance.currentData.currentHour = currentHour;
        }
    }

    void UpdateLighting()
    {
        // --- 常に「夜（22時）」としての計算が行われます ---

        // 22時なので intensityMultiplier は 0 になります
        float intensityMultiplier = 0f;

        // 太陽の回転（夜の位置に固定）
        float timePercent = currentHour / 24f;
        float sunAngle = (timePercent * 360f) - 90f;

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            sunLight.intensity = 0f; // 太陽の強さは0
        }

        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);
            moonLight.intensity = 0.5f; // 月の明るさは最大
        }

        // 環境光とフォグ（夜の色を使用）
        RenderSettings.ambientLight = nightAmbient;
        RenderSettings.fogColor = nightFogColor;
        RenderSettings.skybox.SetFloat("_Exposure", 0.2f); // 少し暗めに
    }

    void UpdateSkybox()
    {
        if (nightSkybox == null) return;

        // 常に夜空をセット
        if (RenderSettings.skybox != nightSkybox)
        {
            RenderSettings.skybox = nightSkybox;
        }
    }

    void UpdateMoonPhase()
    {
        if (moonSunLight == null) return;

        // 月齢計算（時間が止まっているので月齢も止まります）
        float phaseProgress = (currentDay + (currentHour / 24f)) % moonPhaseLength;
        float phasePercent = phaseProgress / moonPhaseLength;
        float angle = phasePercent * 360f;

        moonSunLight.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
    }

    void OnApplicationQuit()
    {
        // 色を元に戻す処理
        if (daySkybox != null) daySkybox.SetColor("_Tint", Color.white);
        if (nightSkybox != null) nightSkybox.SetColor("_Tint", Color.white);
    }
}