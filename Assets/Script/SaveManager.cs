using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("InventorySystemを参照")]
    public InventoryManager inventoryManager;

    [Header("現在のセーブデータ")]
    public SaveData currentData;

    [Header("saveするための経路")]
    private string savePath;

    void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        savePath = Path.Combine(Application.persistentDataPath, "save.json");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            DeleteSaveData();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // タイトルシーンでは何もしない
        if (scene.name == "TitleScene") return;

        // ① 【最重要】シーンが切り替わったら「古い参照」を捨てて必ず最新を探し直す！
        inventoryManager = Object.FindAnyObjectByType<InventoryManager>();

        // ② 【最重要】イベント中に死ぬと「字幕再生中」のロックが一生残るため、強制解除！
        GlobalSubtitleState.IsAnySubtitlePlaying = false;

        // ③ 念のためプレイヤーの操作ロックも確実に解除
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.canControl = true;

        // =========================================================
        // ★追加：タイトル画面からの指示（新規かロードか）を受け取る
        // =========================================================
        int isLoadGame = PlayerPrefs.GetInt("IsLoadGame", 0);

        if (isLoadGame == 1) // 「LOAD GAME」を選んだ場合
        {
            Debug.Log("ロードゲームとして開始します");
            LoadDataFromDisk();
            ApplyLoadData();

            // プレイヤーの位置と向きをセーブデータから復元
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && currentData != null && currentData.playerPosition != Vector3.zero)
            {
                // プレイヤーの物理演算による暴走を防ぐため、一時的に無効化
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = currentData.playerPosition;
                player.transform.rotation = currentData.playerRotation;

                if (cc != null) cc.enabled = true;

                // 視点の同期
                if (pc != null) pc.SyncRotationToCurrent();
            }
        }
        else // 「START GAME」を選んだ場合、または直接シーンを再生した場合
        {
            Debug.Log("新規ゲームとして開始します");
            DeleteSaveData(); // 古いデータを消してまっさらにする
            LoadDataFromDisk();
            ApplyLoadData();
        }
    }

    // ファイル読み込み専用関数
    private void LoadDataFromDisk()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                currentData = JsonUtility.FromJson<SaveData>(json);
            }
            catch
            {
                currentData = new SaveData();
            }
        }
        else
        {
            currentData = new SaveData();
        }
    }

    // 読み込んだデータをゲーム世界に反映する関数
    private void ApplyLoadData()
    {
        if (currentData != null && inventoryManager != null)
        {
            inventoryManager.LoadItemData(currentData.collectedItems);
        }
    }

    // 公開用のロード関数（手動でロードしたい時用）
    public void LoadGame()
    {
        LoadDataFromDisk();
        ApplyLoadData();
    }

    // セーブ実行
    public void SaveGame()
    {
        if (currentData == null) currentData = new SaveData();

        if (inventoryManager != null)
        {
            currentData.collectedItems = inventoryManager.GetItemDataForSave();
        }

        currentData.sceneName = SceneManager.GetActiveScene().name;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentData.playerPosition = player.transform.position;
            currentData.playerRotation = player.transform.rotation;
        }

        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(savePath, json);

        // =========================================================
        // ★追加：セーブデータが存在するという証拠をシステムに刻む
        // =========================================================
        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.Save();

        Debug.Log("セーブしました！ 位置: " + currentData.playerPosition);
    }

    public void DeleteSaveData()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        currentData = new SaveData();

        // セーブデータが存在しない状態に戻す
        PlayerPrefs.SetInt("HasSaveData", 0);
        PlayerPrefs.Save();
        Debug.Log("セーブデータを削除しました");
    }

    public void MarkEventAsCompleted(string eventID)
    {
        if (currentData == null) return;
        if (!currentData.completedEventIDs.Contains(eventID))
        {
            currentData.completedEventIDs.Add(eventID);
        }
    }

    public bool IsEventCompleted(string eventID)
    {
        if (currentData == null) return false;
        return currentData.completedEventIDs.Contains(eventID);
    }

    public bool HasItem(string itemName)
    {
        if (currentData == null || currentData.collectedItems == null) return false;
        return currentData.collectedItems.Contains(itemName);
    }
}