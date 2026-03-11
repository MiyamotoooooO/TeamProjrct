using System.Collections;
using UnityEngine;

public class PuzzleButton : MonoBehaviour
{
    public int buttonID; // 1～4の番号
    public PuzzleManager manager;

    [Header("ボタンの動作設定")]
    [Tooltip("実際にZ軸方向に動かしたい「ボタンの見た目（モデル）」を登録してください")]
    public Transform partToMove;

    [Tooltip("押し込まれた時のZ軸のローカル座標（例: -0.0115）")]
    public float pressedZ = -0.0115f;

    [Tooltip("押し込むのにかかる時間（秒）")]
    public float pushDuration = 0.15f;

    [Tooltip("押し込んだ後、元の位置に戻るのにかかる時間（秒）")]
    public float returnDuration = 0.15f;

    [Header("音声設定")]
    [Tooltip("音を鳴らすためのAudioSource")]
    public AudioSource audioSource;
    [Tooltip("ボタンを押した時の「ポチッ」という音")]
    public AudioClip clickSound;

    // 内部変数
    private Vector3 originalLocalPos;
    private bool isMoving = false;

    private void Start()
    {
        // もしInspectorで動かすパーツが設定されていなければ、このスクリプトがついているオブジェクト自身を動かす
        if (partToMove == null)
        {
            partToMove = this.transform;
        }

        // 初期位置（Z=0などの元の場所）を記憶しておく
        originalLocalPos = partToMove.localPosition;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // 外部（ItemUse.cs）から呼ばれるが、色変更をなくしたため中身は空（エラー防止用）
    public void OnHover() { }

    public void PressButton()
    {
        // 既に動いている最中なら、連打できないようにする
        if (isMoving) return;

        // ボタンが押し込まれるアニメーションを開始
        StartCoroutine(ButtonPressRoutine());

        // マネージャーに通知する
        if (manager != null)
        {
            manager.InputButton(buttonID, this);
        }
    }

    private IEnumerator ButtonPressRoutine()
    {
        isMoving = true;

        // 1. 音を鳴らす
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // 2. 押し込む動き（現在の位置から、指定したpressedZへ）
        float elapsed = 0f;
        Vector3 targetPos = new Vector3(originalLocalPos.x, originalLocalPos.y, pressedZ);

        while (elapsed < pushDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pushDuration;
            partToMove.localPosition = Vector3.Lerp(originalLocalPos, targetPos, t);
            yield return null;
        }
        partToMove.localPosition = targetPos; // ズレ防止で最後にピッタリ合わせる

        // 3. 元に戻る動き（pressedZから、元の位置へ）
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            partToMove.localPosition = Vector3.Lerp(targetPos, originalLocalPos, t);
            yield return null;
        }
        partToMove.localPosition = originalLocalPos;

        isMoving = false;
    }

    // マネージャーから呼ばれていた色変更の関数（エラー防止のため中身を空にして残しています）
    public void GlowCorrect() { }
    public void GlowWrong() { }
    public void ResetColor() { }
}