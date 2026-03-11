using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class ForceStartScene
{
    // このスクリプトは、Unityエディタが読み込まれた時に自動で実行されます
    static ForceStartScene()
    {
        // Build Settingsに登録されているシーンがあるかチェック
        if (EditorBuildSettings.scenes.Length > 0)
        {
            // 一番上（0番目）に登録されているシーン（＝タイトル画面）のデータを取得
            string startScenePath = EditorBuildSettings.scenes[0].path;
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(startScenePath);

            // Playボタンを押した時に、必ずそのシーンから始まるように強制セット！
            EditorSceneManager.playModeStartScene = startScene;
        }
    }
}