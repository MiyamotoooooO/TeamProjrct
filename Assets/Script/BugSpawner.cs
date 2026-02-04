using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BugSpawner : MonoBehaviour
{
    [Header("Bugのプレハブ")]
    [SerializeField] private GameObject bugPrefab;

    [Header("Bugの数")]
    [SerializeField] private int bugCount = 20;

    [Header("Bugの効果音")]
    [SerializeField] private AudioClip bugAudio;

    [Header("オーディオソース")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Trasform")]
    [SerializeField] private Transform _transform;

    //出現場所
    private Vector3[] points =
    {
        new Vector3(1000, 0, 0),
        new Vector3(0, 600, 0),
        new Vector3(-1000, 0, 0),
        new Vector3(0, 600, 0)
    };

    public void SpawnBugs()
    {
        for (int i = 0; i < bugCount; i++)
        {
            GameObject bug = Instantiate(bugPrefab, points[Random.Range(0, points.Length)], Quaternion.identity, _transform);
            bug.GetComponent<BugMove>().StartMove();
            //_audioSource.PlayOneShot(bugAudio);
            StartCoroutine(MultipleAudioCoroutine(bugAudio, 5));
        }
    }

    private IEnumerator MultipleAudioCoroutine(AudioClip audioClip, int num)
    {
        for (int i = 0; i <= num; i++)
        {
            _audioSource.PlayOneShot(audioClip);
            Debug.Log("audioPlay");
            yield return new WaitForSeconds(0.2f);
        }
    }
}