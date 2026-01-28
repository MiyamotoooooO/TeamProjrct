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

    // 毎フレームキー入力を監視
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

            // ※プレイヤーの移動は WakeUpController に任せているのでここでは何もしない
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

    // ★セーブデータを削除する関数
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

    // ★イベントが終わったことを記録する関数
    // FallingCageもここを使って保存されます
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
}