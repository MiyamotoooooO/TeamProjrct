using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class FlashlightController : MonoBehaviour
{
    [Header("モード切替")]
    public bool isFlashlightOn = true;

    [Header("明るさの設定")]
    [Range(0f, 1f)] public float onIntensity = 0.3f; // 暗い時の濃さ
    [Range(0f, 1f)] public float offIntensity = 0f;  // 通常時の濃さ

    [Header("設定")]
    [Tooltip("Post-process Volumeがついているオブジェクトをドラッグ")]
    public PostProcessVolume postVolume;

    // private
    private Vignette vignette;

    void Start()
    {
        if (postVolume == null)
        {
            postVolume = Object.FindAnyObjectByType<PostProcessVolume>();
        }

        if (postVolume != null && postVolume.profile != null)
        {
            // Vignetteの設定を取得する
            if (postVolume.profile.TryGetSettings(out vignette))
            {
                Debug.Log("Vignetteを取得しました");
            }
            else
            {
                Debug.LogError("ProfileにVignetteが追加されていません！Add effectしてください。");
            }
        }
    }

    void Update()
    {
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
}