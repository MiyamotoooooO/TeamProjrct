using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class BlackoutManager : MonoBehaviour
{
    [Header("現在の状態")]
    [Tooltip("チェックを入れると停電モード")]
    public bool isBlackout = false;

    [Header("環境光の設定")]
    public Color blackoutColor = Color.black;

    [Header("ポストプロセスの設定")]
    [Tooltip("通常時のVolume (Global Volume)")]
    public PostProcessVolume normalVolume;

    [Tooltip("停電時のVolume (DarkPostVolume)")]
    public PostProcessVolume darkVolume;

    [Header("停電時の見た目調整")]
    [Tooltip("停電時のVignetteの濃さをここで固定")]
    [Range(0f, 1f)] public float darkVignetteIntensity = 0.5f;

    // 内部変数
    private Color normalAmbientColor;
    private Vignette darkVignette;

    void Start()
    {
        // 通常時の明るさを記憶（停電が終わったら戻すため）
        normalAmbientColor = RenderSettings.ambientLight;

        // DarkVolumeからVignette設定を取得しておく（濃さを強制するため）
        if (darkVolume != null)
        {
            darkVolume.profile.TryGetSettings(out darkVignette);
        }

        // 3. 初期状態の適用
        ApplyState();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyState();
        }
    }

    // 状態を切り替えるメイン処理
    public void ApplyState()
    {
        if (isBlackout)
        {
            // 停電モード

            // 環境光を真っ暗にする
            RenderSettings.ambientLight = blackoutColor;

            // 通常の画作り(NormalVolume)をOFFにする
            if (normalVolume != null) normalVolume.isGlobal = false;

            // 停電用の画作り(DarkVolume)をONにする
            if (darkVolume != null)
            {
                darkVolume.isGlobal = true;

                if (darkVignette != null)
                {
                    darkVignette.intensity.value = darkVignetteIntensity;
                }
            }
        }
        else
        {
            // 通常モード（電気復旧）

            // 環境光を元の明るさに戻す
            RenderSettings.ambientLight = normalAmbientColor;

            // 停電用の画作りをOFF
            if (darkVolume != null) darkVolume.isGlobal = false;

            // 通常の画作りをON
            if (normalVolume != null) normalVolume.isGlobal = true;
        }
    }
}