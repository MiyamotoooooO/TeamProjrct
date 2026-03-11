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

        if (inventoryManager == null)
        {
            inventoryManager = Object.FindAnyObjectByType<InventoryManager>();
        }

        LoadDataFromDisk();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyLoadData();
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
        // ① 【最重要】シーンが切り替わったら「古い参照」を捨てて必ず最新を探し直す！
        inventoryManager = Object.FindAnyObjectByType<InventoryManager>();

        // ② 【最重要】イベント中に死ぬと「字幕再生中」のロックが一生残るため、強制解除！
        GlobalSubtitleState.IsAnySubtitlePlaying = false;

        // ③ 念のためプレイヤーの操作ロックも確実に解除
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.canControl = true;

        ApplyLoadData();
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

        Debug.Log("セーブしました！ 位置: " + currentData.playerPosition);
    }

    public void DeleteSaveData()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        currentData = new SaveData();
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