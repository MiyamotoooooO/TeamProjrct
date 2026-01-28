using UnityEngine;

public class FlashlightSystem : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("ライトの本体（Spotlightなどの光源）")]
    public GameObject lightSource;

    [Tooltip("ライトのON/OFFを切り替えるキー")]
    public KeyCode toggleKey = KeyCode.F;

    // 外部（TrapEventSystemなど）から制御するための変数
    [HideInInspector] public bool isFlashlightOn = true;
    [HideInInspector] public bool canUseFlashlight = true;

    void Start()
    {
        // ゲーム開始時の状態（最初はONにしておく）
        // ※もし最初はOFFがいいなら false に変えてください
        isFlashlightOn = true;
        ApplyState();
    }

    void Update()
    {
        // ★復活：Fキーが押されたら切り替える処理
        if (Input.GetKeyDown(toggleKey))
        {
            // ライトが使用可能な状態（イベント中でない）なら切り替え
            if (canUseFlashlight)
            {
                ToggleFlashlight();
            }
        }
    }

    // ON/OFFを反転させる処理
    void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn; // trueならfalseに、falseならtrueにする
        ApplyState(); // 実際のライトに反映
    }

    // 状態を反映させる関数
    public void ApplyState()
    {
        if (lightSource != null)
        {
            lightSource.SetActive(isFlashlightOn);
        }
    }

    // --- 他のスクリプトとの互換性用 ---

    public void TurnOff()
    {
        if (!canUseFlashlight) return;
        isFlashlightOn = false;
        ApplyState();
    }

    public void TurnOn()
    {
        if (!canUseFlashlight) return;
        isFlashlightOn = true;
        ApplyState();
    }
}