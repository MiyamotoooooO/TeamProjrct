using UnityEngine;

public class GlobalLightControl : MonoBehaviour
{
    [Header("--- 絶対に固定したい暗さ設定 ---")]

    [Header("環境光（全体のベースの明るさ）")]
    [Tooltip("空の色に関係なく、この色で全体を塗りつぶします。黒に近いほど暗くなります。")]
    public Color ambientLightColor = new Color(0.02f, 0.02f, 0.02f, 1f);

    [Header("メインライト（月・太陽）の強さ")]
    [Tooltip("HierarchyにあるDirectional Light（Moonなど）をここにセットしてください")]
    public Light mainDirectionalLight;
    [Tooltip("ライトの強さをこの値で固定します")]
    [Range(0f, 2f)]
    public float fixedLightIntensity = 0.1f;

    [Header("霧（Fog）の設定")]
    [Tooltip("霧の濃度を固定するかどうか")]
    public bool lockFog = true;
    [Tooltip("霧の色（環境光と同じにすると馴染みます）")]
    public Color fogColor = new Color(0.02f, 0.02f, 0.02f, 1f);
    [Tooltip("霧の濃さ（0で霧なし、数字が大きいほど濃い）")]
    [Range(0f, 0.1f)]
    public float fogDensity = 0.02f;

    void Start()
    {
        // 環境光のモードを「色指定（Flat）」に強制変更
        // これをしないと、スカイボックス（空の絵）の明るさに影響されてしまいます
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    // LateUpdateは、他のすべてのUpdate（天候システムなど）が終わった後に呼ばれます。
    // つまり、他のスクリプトが明るさを変えた直後に、このスクリプトが即座に元に戻します。
    void LateUpdate()
    {
        // 1. 環境光を強制的に上書き
        RenderSettings.ambientLight = ambientLightColor;

        // 2. メインライトの強さを強制的に上書き
        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.intensity = fixedLightIntensity;

            // もし雷スクリプトが色を変えてくるなら、色も固定可能です（必要ならコメントアウトを外す）
            // mainDirectionalLight.color = Color.white; 
        }

        // 3. 霧の設定を強制的に上書き
        if (lockFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared; // 一般的なリアルな霧
            RenderSettings.fogDensity = fogDensity;
        }
    }
}