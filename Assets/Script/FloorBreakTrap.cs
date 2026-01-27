using UnityEngine;

public class FloorBreakTrap : MonoBehaviour
{
    [Header("--- 床のオブジェクト設定 ---")]
    [Tooltip("最初は表示されている「普通の床」をここに登録（複数可）")]
    public GameObject[] intactFloors;

    [Tooltip("最初は隠れている「壊れた床」をここに登録（複数可）")]
    public GameObject[] brokenFloors;

    [Header("--- 演出設定 ---")]
    [Tooltip("壊れた時に鳴らす音")]
    public AudioClip breakSound;

    [Tooltip("土煙のパーティクル（DustEffect）")]
    public ParticleSystem dustEffect;

    // ★追加：木くずのパーティクル
    [Tooltip("木くずのパーティクル（WoodChipsEffect）")]
    public ParticleSystem woodChipsEffect;

    [Tooltip("飛び散る破片のプレハブ（あれば）")]
    public GameObject debrisPrefab;
    [Tooltip("破片が出る数")]
    public int debrisCount = 5;

    // 内部変数
    private bool isBroken = false;
    private AudioSource audioSource;

    void Start()
    {
        // （省略：Startの中身は変更なし）
        foreach (var floor in intactFloors) { if (floor != null) floor.SetActive(true); }
        foreach (var floor in brokenFloors) { if (floor != null) floor.SetActive(false); }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    // （省略：OnTriggerEnterは変更なし）
    void OnTriggerEnter(Collider other)
    {
        if (isBroken) return;
        if (other.CompareTag("Player"))
        {
            BreakTheFloor();
        }
    }

    void BreakTheFloor()
    {
        isBroken = true;

        // 1. 普通の床を消す
        foreach (var floor in intactFloors) { if (floor != null) floor.SetActive(false); }

        // 2. 壊れた床を表示する
        foreach (var floor in brokenFloors) { if (floor != null) floor.SetActive(true); }

        // 3. 音を鳴らす
        if (breakSound != null) audioSource.PlayOneShot(breakSound);

        // 4. 土煙を再生
        if (dustEffect != null) dustEffect.Play();

        // 5. ★追加：木くずを再生
        if (woodChipsEffect != null) woodChipsEffect.Play();

        // 6. 破片（瓦礫）をばら撒く演出
        if (debrisPrefab != null) SpawnDebris();
    }

    // （省略：SpawnDebrisは変更なし）
    void SpawnDebris()
    {
        for (int i = 0; i < debrisCount; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 1.0f;
            GameObject debris = Instantiate(debrisPrefab, spawnPos, Quaternion.identity);
            Rigidbody rb = debris.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.down * 2.0f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 5.0f, ForceMode.Impulse);
            }
            Destroy(debris, 5.0f);
        }
    }
}