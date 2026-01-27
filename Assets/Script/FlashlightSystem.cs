using UnityEngine;

public class FlashlightSystem : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("ライトの本体（Spotlightなどの光源）")]
    public GameObject lightSource;

    // 外部（TrapEventSystemなど）から制御するための変数
    // ※Inspectorでは見えなくても良いので HideInInspector にしていますが、
    //   他のスクリプトからアクセスできるように public にしています。
    [HideInInspector] public bool isFlashlightOn = true;
    [HideInInspector] public bool canUseFlashlight = true;

    void Start()
    {
        // ★ゲーム開始時に強制的にオンにする
        isFlashlightOn = true;
        ApplyState();
    }

    void Update()
    {
        // ★以前あった「キー入力（Fキーなど）でオンオフする処理」を削除しました。
        // これにより、プレイヤーが勝手に消すことはできなくなります。

        // ※もし「常にライトをカメラの向きに追従させたい」場合は、
        // ライトをカメラの子オブジェクトにしていれば自動で追従します。
    }

    // 状態を反映させる関数（TrapEventSystemなどから呼ばれます）
    public void ApplyState()
    {
        if (lightSource != null)
        {
            // フラグの状態に合わせて表示・非表示を切り替える
            lightSource.SetActive(isFlashlightOn);
        }
    }

    // --- 以下、他のスクリプトとの互換性用（エラー防止） ---

    // TrapEventSystemなどで「消灯」命令が来た時の処理
    // ※「常にオン」と言いつつ、檻が落ちる演出などの時は消したい場合があるため、
    //   外部からの強制変更は受け入れるようにしています。
    //   もし「演出中も絶対につけっぱなしがいい」場合は、この中身も削除してください。
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