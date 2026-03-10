using UnityEngine;
using UnityEngine.Video;

public class TVInteract : MonoBehaviour
{
    public GameObject tvVideo;
    public VideoPlayer videoPlayer;
    public MonoBehaviour playerController;

    bool playerNear = false;
    bool watching = false;

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (!watching)
            {
                watching = true;

                tvVideo.SetActive(true);
                videoPlayer.Play();
                playerController.enabled = false;
            }
            else
            {
                watching = false;

                videoPlayer.Stop();
                tvVideo.SetActive(false);
                playerController.enabled = true;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }
}