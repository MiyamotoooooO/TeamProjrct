using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public void Title(string command)
    {
        switch (command)
        {
            case "Start":
                SceneManager.LoadScene("SampleScene");
                break;
            case "Load":
                Debug.Log("LoadGame");
                break;
        }
    }
}
