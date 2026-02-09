using UnityEngine;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(BoxCollider))]
public class RoomLight : MonoBehaviour
{
    [System.Serializable]
    public class LightSettings
    {
        public Color color = Color.white;
        public float intensity = 1.0f;
        public float range = 10.0f;
    }

    [Header("ライトの設定 (Inspectorで変更可能)")]
    [SerializeField] private LightSettings normalSettings; // 通常時の設定
    [SerializeField] private LightSettings redSettings;    // 赤色時の設定

    [Header("参照")]
    [SerializeField] private PlayerHealth playerHealth;

    // 内部変数
    private Light myLight;
    private bool isRedMode = false; // 現在赤ランプモードか

    private void Awake()
    {
        myLight = GetComponent<Light>();

        // プレイヤーのHealthスクリプトを自動取得
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        // 初期状態は通常モードにする
        SetNormal();
    }

    // 通常モードにする
    public void SetNormal()
    {
        isRedMode = false;
        ApplySettings(normalSettings);
        myLight.enabled = true;
    }

    // 赤ランプモードにする（危険）
    public void SetRed()
    {
        isRedMode = true;
        ApplySettings(redSettings);
        myLight.enabled = true;
    }

    // 点滅用：ライトのON/OFFを切り替える
    public void ToggleLight(bool enable)
    {
        myLight.enabled = enable;
    }

    // 設定をLightコンポーネントに反映させる
    private void ApplySettings(LightSettings settings)
    {
        myLight.color = settings.color;
        myLight.intensity = settings.intensity;
        myLight.range = settings.range;
    }

    private void OnTriggerStay(Collider other)
    {
        // 赤モード中 かつ プレイヤーが範囲内にいる場合
        if (isRedMode && other.CompareTag("Player"))
        {
            // PlayerHealthのDie関数を呼ぶ
            if (playerHealth != null)
            {
                playerHealth.Die();
            }
        }
    }
}