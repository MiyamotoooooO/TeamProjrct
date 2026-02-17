using UnityEngine;
using UnityEngine.UI;

public class WaveController : MonoBehaviour
{
    [SerializeField] private float waveSpeed;
    [SerializeField] private HeartLiner heartLiner;
    [SerializeField] private Transform canvas;
    private Image image;
    Vector3 pos;

    private void Start()
    {
        pos = transform.position;
        image = GetComponent<Image>();
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
                heartLiner.SpawnWave(this.gameObject);
                Destroy(this.gameObject);
            }
        }
        else
            image.fillAmount += waveSpeed * 0.003f;
        //}
    }
}
