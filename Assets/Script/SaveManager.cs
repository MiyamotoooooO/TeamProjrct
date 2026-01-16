using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement; // シーン移動検知用
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("InventrySystemを参照")]
    public InventoryManager inventoryManager;

    [Header("現在のセーブデータ")]
    public SaveData currentData;
    private string savePath;
    private string saveFilePath;
    void Awake()
    {
        // --- シングルトン化 & DontDestroyOnLoad ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーン移動しても破壊されない
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
        LoadGame(); // 起動時にデータを読み込む
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        if (inventoryManager != null)
        {
            data.collectedItems = inventoryManager.GetItemDataForSave();
        }
    }

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

        if (inventoryManager != null && currentData != null)
    {
        // "currentData" の中にあるアイテムリストを渡して、シーンに反映させる
        inventoryManager.LoadItemData(currentData.collectedItems);
        
        Debug.Log("アイテムデータをロードしました");
    }
    }

    // ゲーム終了時・中断時にファイルへ書き込む
    private void OnApplicationQuit()
    {
        SaveToJSON();
    }
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveToJSON();
    }
    // 手動セーブなどをしたい場合用
    public void SaveToJSON()
    {
        if (currentData == null) return;
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("セーブ完了(ファイル書き込み): " + savePath);
    }
}