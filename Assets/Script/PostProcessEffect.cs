using UnityEngine;

[ExecuteInEditMode] // ゲーム再生しなくてもエディタ上で見た目を確認できるようにする
public class PostProcessEffect : MonoBehaviour
{
    public Material effectMaterial;

    // 画面が描画された後に呼ばれるUnityの特殊な関数
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial != null)
        {
            // カメラの映像(source)にマテリアル(effectMaterial)を通して画面(destination)に出す
            Graphics.Blit(source, destination, effectMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}