using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HeartLiner : MonoBehaviour
{
    [SerializeField] private Image BaseImage;
    [SerializeField] private Sprite[] Waves;
    public int index;

    private Image image;

    private void Start()
    {
        image = GetComponent<Image>();
        NewWave();
    }

    private void NewWave()
    {
        //BaseImage.sprite = image.sprite;
        image.sprite = Waves[index];
        StartCoroutine(animFillAmount());
    }

    private IEnumerator animFillAmount()
    {
        image.fillAmount = 0;
        while (image.fillAmount < 1f)
        {
            image.fillAmount += 1 * Time.deltaTime;
            yield return null;
        }
        NewWave();
    }
}