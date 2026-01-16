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

    [Tooltip("（VolumeがONの時だけ有効）懐中電灯ON時のVignetteの濃さ")]
    [Range(0f, 1f)] public float onIntensity = 0.3f;

    [Range(0f, 1f)] public float offIntensity = 0f;  // 通常時の濃さ
    public bool isFlashlightOn = true;

    // private
    private bool isOn = false; // 最初はOFF
    private Color normalAmbientColor;
    private Vignette vignette;

    void Start()
    {
        // 1. 通常時の明るさを記憶
        normalAmbientColor = RenderSettings.ambientLight;

        // 2. Volumeの取得（もし空なら探す）
        if (postVolume == null)
        {
            postVolume = Object.FindAnyObjectByType<PostProcessVolume>();
        }

        if (postVolume != null)
        {
            postVolume.isGlobal = false;

            // Vignetteの設定を取得しておく（濃さ調整のため）
            postVolume.profile.TryGetSettings(out vignette);
        }

        // 3. 初期状態の適用
        isOn = false;
        ApplyState();
    }

    void Update()
    {
        // Fキーで懐中電灯と環境光だけ切り替え
        if (Input.GetKeyDown(KeyCode.F))
        {
            isOn = !isOn;
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
    }
}