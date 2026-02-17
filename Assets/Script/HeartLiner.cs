using UnityEngine;
using UnityEngine.UI;

public class HeartLiner : MonoBehaviour
{
    [SerializeField] private Image hpGuage;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Color[] waveColors;

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Keypad0))
            hpGuage.fillAmount -= 0.001f;
    }

    public void ColorChange(Image img)
    {
        //スタミナ残量によって色を変える
        if (playerController.currentStaminaPercent >= 50)
        {
            Debug.Log("ColorChangeGreen");
            img.color = waveColors[0];
        }
        else if (playerController.currentStaminaPercent >= 25)
        {
            Debug.Log("ColorChangeYellow");
            img.color = waveColors[1];
        }
        else
        {
            Debug.Log("ColorChangeGreen");
            img.color = waveColors[2];
        }
    }
}