using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class FlashlightSystem : MonoBehaviour
{
    [Header("ライトの設定")]
    [Tooltip("カメラの下に作ったSpot Lightを入れる")]
    public Light flashlightSpot;

    [Header("環境光の設定")]
    [Tooltip("懐中電灯ON時の周りの明るさ（真っ黒がおすすめ）")]
    public Color blackoutColor = Color.black;

    [Header("ポストプロセスの設定")]
    [Tooltip("Global Volumeを入れる")]
    public PostProcessVolume postVolume;

    // private
    private bool isOn = false; // 最初はOFF
    private Color normalAmbientColor;

    void Start()
    {
        // 1. 通常時の明るさを記憶
        normalAmbientColor = RenderSettings.ambientLight;

        // 2. Volumeの取得（もし空なら探す）
        if (postVolume == null)
        {
            postVolume = Object.FindAnyObjectByType<PostProcessVolume>();
        }

        // ゲーム開始時に「isGlobal」をOFFにする
        if (postVolume != null)
        {
            postVolume.isGlobal = false;
            // Volume自体(enabled)はONのまま維持されます
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
    }

    void ApplyState()
    {
        // --- 1. 懐中電灯（Spot Light）の切り替え ---
        if (flashlightSpot != null)
        {
            flashlightSpot.enabled = isOn;
        }

        // --- 2. 環境光の切り替え ---
        RenderSettings.ambientLight = isOn ? blackoutColor : normalAmbientColor;
    }
}