using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
public class FlashlightSystem : MonoBehaviour
{
    [Header("ライトの設定")]
    [Tooltip("カメラの下に作ったSpot Lightを入れる")]
    public Light flashlightSpot;

    [Header("環境光の設定")]
    public Color blackoutColor = Color.black;

    [Header("ポストプロセスの設定")]
    [Tooltip("Global Volumeを入れる")]
    public PostProcessVolume postVolume;

    [Header("停電時のVolume")]
    public PostProcessVolume darkVolume;

    [Header("Vignetteの微調整")]
    [Tooltip("DarkVolume側のVignetteの濃さ")]
    [Range(0f, 1f)] public float darkVignetteIntensity = 0.5f;

    [Tooltip("（VolumeがONの時だけ有効）懐中電灯ON時のVignetteの濃さ")]
    [Range(0f, 1f)] public float onIntensity = 0.3f;

    [Range(0f, 1f)] public float offIntensity = 0f;  // 通常時の濃さ

    [Header("通常時のVolume")]
    public PostProcessVolume normalVolume;

    [Tooltip("停電モード")]
    public bool isBlackout = false;

    public bool isFlashlightOn = true;

    // private
    private bool isOn = false; // 最初はOFF
    private Color normalAmbientColor;
    private Vignette vignette;
    private Vignette darkVignette;

    void Start()
    {
        // 1. 通常時の明るさを記憶
        normalAmbientColor = RenderSettings.ambientLight;

        // 2. Volumeの取得（もし空なら探す）
        if (postVolume == null)
        {
            postVolume = Object.FindAnyObjectByType<PostProcessVolume>();
        }

        if (darkVolume != null)
        {
            darkVolume.profile.TryGetSettings(out darkVignette);
        }

        if (postVolume != null)
        {
            postVolume.isGlobal = true;

            // Vignetteの設定を取得しておく（濃さ調整のため）
            postVolume.profile.TryGetSettings(out vignette);
        }

        // 3. 初期状態の適用
        isOn = false;
        isBlackout = false;
        ApplyState();
    }

    void Update()
    {
        // Fキーで懐中電灯と環境光だけ切り替え
        if (Input.GetKeyDown(KeyCode.F))
        {
            isOn = !isOn;
            //isBlackout = !isBlackout;
            ApplyState();
        }

        if (vignette == null) return;

        if (isFlashlightOn)
        {
            vignette.intensity.value = onIntensity;
        }
        else
        {
            vignette.intensity.value = offIntensity;
        }
    }

    void OnValidate()
    {
        // ゲーム実行中のみ反映（エディタ編集中に画面がカチカチ変わるのを防ぐため）
        if (Application.isPlaying)
        {
            ApplyState();
        }
    }

    void ApplyState()
    {
        // 懐中電灯（Spot Light）の切り替え 
        if (flashlightSpot != null)
        {
            flashlightSpot.enabled = isOn;
        }

        // 環境光の切り替え
        RenderSettings.ambientLight = isOn ? blackoutColor : normalAmbientColor;

        // Vignetteの濃さ調整
        if (vignette != null)
        {
            vignette.intensity.value = isOn ? onIntensity : 0f;
        }

        // 通常Volume：停電じゃない時(true) ／ 停電の時(false)
        if (normalVolume != null)
        {
            normalVolume.isGlobal = !isBlackout;
        }

        // 停電Volume：停電の時(true) ／ 停電じゃない時(false)
        if (darkVolume != null)
        {
            darkVolume.isGlobal = isBlackout;

            // （おまけ）Vignetteの濃さをスクリプトで指定値にするなら
            if (isBlackout && darkVignette != null)
            {
                darkVignette.intensity.value = darkVignetteIntensity;
            }
        }
    }
}