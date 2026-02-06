using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    Light lightComp;
    float baseIntensity;

    void Start()
    {
        lightComp = GetComponent<Light>();
        baseIntensity = lightComp.intensity;
        InvokeRepeating(nameof(Flicker), 0f, 0.1f);
    }

    void Flicker()
    {
        lightComp.intensity = baseIntensity * Random.Range(0.0f, 2.0f);
    }
}
