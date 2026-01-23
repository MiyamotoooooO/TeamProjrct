using UnityEngine;
using System.Collections; // ★待機処理（コルーチン）に必要

public class FlashlightSystem : MonoBehaviour
{
    [Header("--- 制御設定 ---")]
    [Tooltip("最初は懐中電灯を使えないようにするか？")]
    public bool canUseFlashlight = false;

    [Header("ライトの設定")]
    [Tooltip("カメラの下に作ったSpot Lightを入れる")]
    public Light flashlightSpot;

    [Header("音声設定")]
    [Tooltip("懐中電灯についているAudioSource")]
    public AudioSource flashlightSource;
    [Tooltip("カチッというスイッチ音")]
    public AudioClip clickSound;

    [Tooltip("現在のライトの状態")]
    public bool isFlashlightOn = false;

    // 設定画面のキーを使う場合はここを書き換えてください
    private KeyCode toggleKey = KeyCode.F;

    // 連打防止用のフラグ
    private bool isToggling = false;

    void Start()
    {
        // ゲーム開始時の状態を反映
        // (音は鳴らさずに即座に反映)
        ApplyState();
    }

    void Update()
    {
        // ロックされていたら、操作させない
        if (!canUseFlashlight)
        {
            // 強制オフ処理
            if (isFlashlightOn)
            {
                isFlashlightOn = false;
                ApplyState();
            }
            return;
        }

        // Fキーでスイッチ切り替え
        // ★変更点：スイッチ操作中(isToggling)は入力を受け付けない
        if (Input.GetKeyDown(toggleKey) && !isToggling)
        {
            StartCoroutine(ToggleFlashlightSequence());
        }
    }

    // ★追加：音を鳴らして待ってからライトを切り替える処理
    IEnumerator ToggleFlashlightSequence()
    {
        isToggling = true; // 操作ロック開始

        // 1. 音を鳴らす
        if (flashlightSource != null && clickSound != null)
        {
            flashlightSource.PlayOneShot(clickSound);

            // 2. 音の長さ分だけ待機する
            // (もし音が長すぎる場合は 0.2f など秒数を直接指定してもOK)
            yield return new WaitForSeconds(clickSound.length);
        }
        else
        {
            // 音がない場合は一瞬だけ待つ（違和感防止）
            yield return new WaitForSeconds(0.1f);
        }

        // 3. フラグを反転させてライトを反映
        isFlashlightOn = !isFlashlightOn;
        ApplyState();

        isToggling = false; // 操作ロック解除
    }

    // 状態を反映させる（外部から呼ぶとき用）
    public void ApplyState()
    {
        if (flashlightSpot != null)
        {
            flashlightSpot.enabled = isFlashlightOn;
        }
    }
}