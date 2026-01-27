using UnityEngine;

public class LighterSystem : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("ライターの火（Lightコンポーネントがついているオブジェクト）")]
    public GameObject lightSource;

    [Tooltip("この名前のレイヤーが付いたオブジェクトを持っていないと使えない")]
    public string requiredLayerName = "Lighter";

    [Tooltip("点灯/消灯するキー")]
    public KeyCode toggleKey = KeyCode.L;

    [Header("音の設定")]
    public AudioClip igniteSound; // カチッ（点火）
    public AudioClip offSound;    // シュッ（消火）

    // 外部から制御するための変数
    [HideInInspector] public bool isLighterOn = false;
    [HideInInspector] public bool canUseLighter = true;

    private AudioSource audioSource;
    private int lighterLayerIndex; // レイヤー番号記憶用

    void Start()
    {
        // レイヤーの名前から番号を取得しておく
        lighterLayerIndex = LayerMask.NameToLayer(requiredLayerName);

        // もしレイヤー設定を忘れていたら警告を出す
        if (lighterLayerIndex == -1)
        {
            Debug.LogError("エラー：UnityのLayers設定に '" + requiredLayerName + "' というレイヤーが見つかりません！");
        }

        // 最初は消しておく
        isLighterOn = false;
        if (lightSource != null) lightSource.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        // 外部から使用禁止にされていたら何もしない
        if (!canUseLighter) return;

        // キー入力があったら切り替え
        if (Input.GetKeyDown(toggleKey))
        {
            // ★重要：ライターアイテムを持っているかチェック
            if (CheckHasLighterItem())
            {
                ToggleLighter();
            }
            else
            {
                Debug.Log("ライターを持っていません（" + requiredLayerName + "レイヤーのアイテムが必要です）");
                // ここに「ライターがない」という音を鳴らしてもいいですね
            }
        }

        // （オプション）もし点灯中にアイテムを失ったら（捨てたら）消す処理
        if (isLighterOn && !CheckHasLighterItem())
        {
            TurnOff();
        }
    }

    // ★プレイヤーがライターレイヤーの付いたオブジェクトを持っているか探す関数
    bool CheckHasLighterItem()
    {
        // 自分（プレイヤー）の子オブジェクトをすべて探す
        // (true)を入れることで、非表示になっているオブジェクトも含めて探せます
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child.gameObject.layer == lighterLayerIndex)
            {
                return true; // 見つかった！
            }
        }
        return false; // 最後まで見つからなかった
    }

    // ON/OFF切り替え
    void ToggleLighter()
    {
        isLighterOn = !isLighterOn;

        if (isLighterOn)
        {
            if (lightSource != null) lightSource.SetActive(true);
            if (igniteSound != null) audioSource.PlayOneShot(igniteSound);
        }
        else
        {
            if (lightSource != null) lightSource.SetActive(false);
            if (offSound != null) audioSource.PlayOneShot(offSound);
        }
    }

    // --- 他のスクリプト（イベントなど）からの制御用 ---

    public void TurnOff()
    {
        isLighterOn = false;
        if (lightSource != null) lightSource.SetActive(false);
    }

    public void TurnOn()
    {
        // 強制点灯の場合も、アイテムチェックを入れるならここにも判定が必要ですが、
        // 演出で強制的に付けたい場合はチェックなしでOKとします
        isLighterOn = true;
        if (lightSource != null) lightSource.SetActive(true);
    }
}