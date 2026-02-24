using UnityEngine;
using UnityEngine.UI;

public class WaveController : MonoBehaviour
{
    [Header("波の動くスピード")]
    [SerializeField] private float waveSpeed;
    [Header("HeaerLinerスクリプト")]
    [SerializeField] private HeartLiner heartLiner;
    [Header("コンポーネント")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image image;
    [Header("合計移動距離")]
    [SerializeField] private float moveDistance = 0;
    Vector3 pos;

    private void Start()
    {
        pos = transform.position;
    }


    void FixedUpdate()
    {
        pos.x -= waveSpeed;
        moveDistance += waveSpeed;
        transform.position = pos;
        if (moveDistance >= 652)
        {
            moveDistance = 0;
            heartLiner.ColorChange(image, rect);
            pos = transform.position;
        }
    }
}
