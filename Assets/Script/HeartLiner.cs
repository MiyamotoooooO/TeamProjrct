using UnityEngine;
using UnityEngine.UI;

public class HeartLiner : MonoBehaviour
{
    [SerializeField] private Image hpGuage;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Color[] waveColors = { Color.green, Color.yellow, Color.red };
    Vector3 spawnPos = new Vector3(581, 280, 0);

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Keypad0))
            hpGuage.fillAmount -= 0.001f;
    }

    public void SpawnWave(GameObject gameObject)
    {
        GameObject obj = Instantiate(gameObject, spawnPos, gameObject.transform.rotation, this.transform);
        Image img = obj.GetComponent<Image>();
        img.fillAmount = 0;
        img.fillOrigin = 0;
        //スタミナ残量によって色を変える
        //if (playerController.currentStaminaPercent <= 50)
        //    img.color = waveColors[0];
        //else if (playerController.currentStaminaPercent <= 80)
        //    img.color = waveColors[1];
        //else
        //    img.color = waveColors[2];
    }
}