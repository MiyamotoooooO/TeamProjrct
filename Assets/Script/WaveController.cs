using UnityEngine;
using UnityEngine.UI;

public class WaveController : MonoBehaviour
{
    [SerializeField] private float waveSpeed;
    [SerializeField] private HeartLiner heartLiner;
    [SerializeField] private Transform canvas;
    [SerializeField] private Image image;
    Vector3 pos;

    private void Start()
    {
        pos = transform.position;
    }


    void FixedUpdate()
    {
        //if (Input.GetKey(KeyCode.Keypad0))
        //{
        pos.x -= waveSpeed;
        transform.position = pos;
        if (pos.x <= 250)
        {
            image.fillOrigin = 1;
            image.fillAmount -= waveSpeed * 0.003f;
            if (image.fillAmount == 0)
            {
                pos.x = 581;
                image.fillAmount = 0;
                image.fillOrigin = 0;
                heartLiner.ColorChange(image);
            }
        }
        else
            image.fillAmount += waveSpeed * 0.003f;
        //}
    }
}
