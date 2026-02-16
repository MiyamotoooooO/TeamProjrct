using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BugSpawner : MonoBehaviour
{
    [Header("Bugのプレハブ")]
    [SerializeField] private GameObject bugPrefab;

    [Header("生成したBugのリスト")]
    public List<GameObject> bugList;

    //[Header("Bugの数")]
    //[SerializeField] private int bugCount = 20;

    [Header("Bugの効果音")]
    [SerializeField] private AudioClip bugAudio;

    [Header("オーディオソース")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Trasform")]
    [SerializeField] private Transform _transform;

    [SerializeField] PlayerController _playerController;

    //出現場所
    private Vector3[] points =
    {
        new Vector3(1000, 0, 0),
        new Vector3(0, 600, 0),
        new Vector3(-1000, 0, 0),
        new Vector3(0, 600, 0)
    };

    //発生させる羽虫の数と自動で消えるまでの秒数を取得して実行
    //(_lifetimeはデフォルト[引数を設定していない場合]で8秒、_lifetimeを0にするとスプレー使用以外で消えなくなる)
    public void SpawnBugs(int bugCount, float _lifeTime = 8, float destroyTime = 5)
    {
        for (int i = 0; i < bugCount; i++)
        {
            //このCanvasの子として羽虫を生成→生存時間を設定→動かす関数を実行→リストに追加→音を鳴らす
            GameObject bug = Instantiate(bugPrefab, points[Random.Range(0, points.Length)], Quaternion.identity, _transform);
            bug.GetComponent<BugMove>().lifeTime = _lifeTime;
            bug.GetComponent<BugMove>()._playerController = _playerController;
            bug.GetComponent<BugMove>().StartMove();
            bugList.Add(bug);
        }
        StartCoroutine(WaitCoroutine(_lifeTime, destroyTime));
        BugAudioPlay();
    }

    private IEnumerator WaitCoroutine(float _lifeTime, float _destroyTime)
    {
        if (_lifeTime == 0) yield break;
        float timer = 0;
        while (true)
        {
            //this.gameObject.SetActive(_playerController.canControl);
            if (_playerController.canControl)
            {
                timer += Time.deltaTime;
                _audioSource.volume = 0.5f;
            }
            else
                _audioSource.volume = 0.3f;
            if (timer > _lifeTime)
                break;
            yield return null;
        }
        StartCoroutine(DestroyBugs(_destroyTime));
    }

    public IEnumerator DestroyBugs(float time)
    {
        //リストにいない状態(つまり羽虫がいない状態)でこの関数が呼ばれても実行しない
        if (bugList.Count == 0) yield break;
        //羽虫を消す時間間隔を算出
        WaitForSeconds span = new WaitForSeconds(time / bugList.Count);
        //一匹づつ消す(段々減る)
        foreach (GameObject obj in bugList)
        {
            Destroy(obj);
            yield return span;
        }
        //リストを空にする
        bugList.Clear();
        //音を止める
        _audioSource.Stop();
    }

    private void BugAudioPlay()
    {
        _audioSource.clip = bugAudio;
        _audioSource.Play();
    }
}