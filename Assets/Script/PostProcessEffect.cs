using UnityEngine;

[ExecuteInEditMode]
public class PostProcessEffect : MonoBehaviour
{
    [Header("EdgeMaterial‚ğQÆ")]
    public Material effectMaterial;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial != null)
        {
            // ƒJƒƒ‰‚Ì‰f‘œ‚ÉeffectMaterial‚ğ’Ê‚µ‚Ä‰æ–Ê‚Éo‚·
            Graphics.Blit(source, destination, effectMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}