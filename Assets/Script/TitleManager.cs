using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image blood;
    [SerializeField] private Transform[] button;
    [SerializeField] private int index = -1;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (index == -1)
            {
                index = button.Length - 1;
            }
            else
            {
                index--;
                if (index <= 0)
                    index = 0;
            }
            SetBack(button[index]);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (index == -1)
            {
                index = 0;
            }
            else
            {
                index++;
                if (index >= button.Length - 1)
                    index = button.Length - 1;
            }
            SetBack(button[index]);
        }
        if (Input.GetKeyDown(KeyCode.Return))
            Title();
    }

    private void SetBack(Transform _transform)
    {
        blood.gameObject.transform.position = _transform.position;
        StartCoroutine(animBlood());
    }

    private IEnumerator animBlood()
    {
        blood.fillAmount = 0;
        while (blood.fillAmount < 1f)
        {
            blood.fillAmount += 5 * Time.deltaTime;
            yield return null;
        }
    }

    public void Title()
    {
        switch (button[index].gameObject.name)
        {
            case "Start":
                SceneManager.LoadScene("Kodoku");
                break;
            case "Load":
                Debug.Log("LoadGame");
                break;
            case "Option":
                Debug.Log("Option");
                break;
        }
    }
}
