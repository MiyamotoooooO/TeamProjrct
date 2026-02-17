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

        // Startの時点でもインベントリへの反映などを念の為行う
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
        if (inventoryManager == null)
        {
            inventoryManager = Object.FindAnyObjectByType<InventoryManager>();
        }
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
                Debug.Log("セーブデータをディスクから読み込みました");
            }
            catch
            {
                currentData = new SaveData();
                Debug.LogWarning("セーブデータの読み込みに失敗、新規作成します");
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
            Debug.Log("セーブデータを削除しました。");
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



/*using System.IO;
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
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // シーン読み込み時に呼び出されるように登録
        SceneManager.sceneLoaded += OnSceneLoaded;
        // ゲーム開始時にロードする
        LoadGame();
    }

    void Update()
    {
        // 左コントロールキーが押されたら削除
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            DeleteSaveData();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (inventoryManager == null)
        {
            inventoryManager = Object.FindAnyObjectByType<InventoryManager>();
        }

        // ロードデータがあるなら反映
        if (currentData != null)
        {
            // インベントリ復元
            if (inventoryManager != null)
            {
                inventoryManager.LoadItemData(currentData.collectedItems);
            }
        }
    }

    // セーブ実行
    public void SaveGame()
    {
        if (currentData == null) currentData = new SaveData();

        // 1. インベントリの中身を保存データに入れる
        if (inventoryManager != null)
        {
            currentData.collectedItems = inventoryManager.GetItemDataForSave();
        }

        // 2. 現在のシーン名とプレイヤー情報を保存
        currentData.sceneName = SceneManager.GetActiveScene().name;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentData.playerPosition = player.transform.position;
            currentData.playerRotation = player.transform.rotation;
            Debug.Log($"位置を保存しました: {currentData.playerPosition}");
        }

        // 3. ファイルに書き込む
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(savePath, json);

        Debug.Log("セーブしました！");
    }

    // ロード実行
    public void LoadGame()
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

        // ロード直後のインベントリ復元
        if (inventoryManager != null && currentData != null)
        {
            inventoryManager.LoadItemData(currentData.collectedItems);
        }
    }

    // セーブデータを削除する関数
    public void DeleteSaveData()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("【デバッグ】セーブデータを削除しました。");
        }
        else
        {
            Debug.Log("【デバッグ】削除するセーブデータがありません。");
        }

        // メモリ上のデータもリセット
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

    // イベントが終わっているか確認する関数
    public bool IsEventCompleted(string eventID)
    {
        if (currentData == null) return false;
        return currentData.completedEventIDs.Contains(eventID);
    }

    public bool HasItem(string itemName)
    {
        // データがまだない、またはリストが空なら「持っていない」
        if (currentData == null || currentData.collectedItems == null)
        {
            return false;
        }

        // リストの中に名前が含まれているかチェック
        return currentData.collectedItems.Contains(itemName);
    }
}*/