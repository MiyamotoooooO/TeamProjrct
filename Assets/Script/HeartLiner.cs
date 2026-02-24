using UnityEngine;
using UnityEngine.UI;

public class HeartLiner : MonoBehaviour
{
    [Header("波形のプロパティ")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Color[] waveColors;
    [SerializeField] private float[] waveFillAmounts;
    [SerializeField] private Vector2[] waveSizeDeltas;
    [SerializeField] private Vector3[] wavePositions;

    public void ColorChange(Image img, RectTransform rect)
    {
        //スタミナ残量によって色を変える
        if (playerController.currentStaminaPercent >= 50)
        {
            Debug.Log("ColorChangeGreen");
            img.color = waveColors[0];
            img.fillAmount = waveFillAmounts[0];
            rect.sizeDelta = waveSizeDeltas[0];
            rect.anchoredPosition = wavePositions[0];
        }
        else if (playerController.currentStaminaPercent >= 25)
        {
            Debug.Log("ColorChangeYellow");
            img.color = waveColors[1];
            img.fillAmount = waveFillAmounts[1];
            rect.sizeDelta = waveSizeDeltas[1];
            rect.anchoredPosition = wavePositions[1];
        }
        else
        {
            Debug.Log("ColorChangeGreen");
            img.color = waveColors[2];
            img.fillAmount = waveFillAmounts[2];
            rect.sizeDelta = waveSizeDeltas[2];
            rect.anchoredPosition = wavePositions[2];
        }
    }
}