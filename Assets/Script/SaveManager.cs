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
        // ゲーム開始時にロードする
        LoadGame();
    }

    // ★追加：毎フレームキー入力を監視する
    void Update()
    {
        // 左コントロールキーが押されたら
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
            // インベントリ復元はそのままOK
            if (inventoryManager != null)
            {
                inventoryManager.LoadItemData(currentData.collectedItems);
            }
        }
    }

    // ★修正：1フレーム待ってから移動させるコルーチン
    IEnumerator ApplyPlayerStateDelay()
    {
        // 1フレーム待つ（これが重要！物理演算の準備完了を待つ）
        yield return null;

        if (currentData == null || string.IsNullOrEmpty(currentData.sceneName)) yield break;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();

            // 念のため、さらにもう1フレーム待つ場合もあるが、通常はこれでOK
            // もし直らなければ yield return new WaitForEndOfFrame(); に変えてみてください

            // 移動の邪魔になるのでコンポーネントを一時停止
            if (cc != null) cc.enabled = false;

            // 位置と向きを強制適用
            player.transform.position = currentData.playerPosition;
            player.transform.rotation = currentData.playerRotation;

            // 物理演算の位置合わせ
            Physics.SyncTransforms();

            // コンポーネント再開
            if (cc != null) cc.enabled = true;

            Debug.Log($"【成功】プレイヤーをロード位置へ移動: {currentData.playerPosition}");
        }
    }

    // ★セーブ実行
    public void SaveGame()
    {
        if (currentData == null) currentData = new SaveData();

        // 1. インベントリの中身を保存データに入れる
        if (inventoryManager != null)
        {
            currentData.collectedItems = inventoryManager.GetItemDataForSave();
        }

        // 2. 現在のシーン名を保存
        currentData.sceneName = SceneManager.GetActiveScene().name;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentData.playerPosition = player.transform.position;
            currentData.playerRotation = player.transform.rotation;
            currentData.sceneName = SceneManager.GetActiveScene().name;

            Debug.Log($"位置を保存しました: {currentData.playerPosition}");
        }

        // 3. ファイルに書き込む
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(savePath, json);

        Debug.Log("セーブしました！");
    }

    // ★ロード実行
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

        // インベントリを復元する
        if (inventoryManager != null && currentData != null)
        {
            inventoryManager.LoadItemData(currentData.collectedItems);
        }
    }

    // ★追加：セーブデータを削除する関数
    public void DeleteSaveData()
    {
        // ファイルが存在するか確認して削除
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("【デバッグ】セーブデータを削除しました。");
        }
        else
        {
            Debug.Log("【デバッグ】削除するセーブデータがありません。");
        }

        // メモリ上のデータもリセットする
        currentData = new SaveData();

        // (任意) インベントリも空っぽにするならここに追加
        // if (inventoryManager != null) inventoryManager.ClearAllItems(); 

        // (任意) わかりやすくシーンを再読み込みするならこれを使う
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ★イベントが終わったことを記録する関数
    public void MarkEventAsCompleted(string eventID)
    {
        if (currentData == null) return;

        if (!currentData.completedEventIDs.Contains(eventID))
        {
            currentData.completedEventIDs.Add(eventID);
        }
    }

    // ★イベントが終わっているか確認する関数
    public bool IsEventCompleted(string eventID)
    {
        if (currentData == null) return false;
        return currentData.completedEventIDs.Contains(eventID);
    }
}