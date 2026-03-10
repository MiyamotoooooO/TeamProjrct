using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public PuzzleRotateManager manager;
    public int objectID; // 0,1,2の番号

    [Header("【新規】対象と判定の設定")]
    public Transform targetToRotate;
    public BoxCollider[] interactColliders;

    [Header("正解の角度設定")]
    [Tooltip("正解となるY軸の角度を入力してください（例: 30, 90, 180 など）")]
    public float correctAngleY = 0f;

    [Header("回転設定")]
    public Vector3 rotationAngle = new Vector3(0, 90f, 0);

    [Header("光る色設定")]
    public Color normalColor = Color.gray;
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f);

    private Renderer rend;
    private bool isHovered = false;

    public void Start()
    {
        if (manager == null) manager = FindAnyObjectByType<PuzzleRotateManager>();
        if (manager == null) return;

        if (targetToRotate == null) targetToRotate = transform;
        if (interactColliders == null || interactColliders.Length == 0) interactColliders = GetComponents<BoxCollider>();

        rend = targetToRotate.GetComponent<Renderer>();
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = normalColor;

        manager.SetCorrectDirection(objectID, 1);

        // ★追加：ゲーム開始時にも詳細なログを出すようにしました
        CheckAngleAndNotify(true);
    }

    private void Update()
    {
        if (rend == null) return;

        if (isHovered) rend.material.color = hoverColor;
        else rend.material.color = normalColor;

        isHovered = false;
    }

    public void OnHover()
    {
        isHovered = true;
    }

    public void RotateLeft()
    {
        if (manager == null) return;

        if (targetToRotate != null)
        {
            targetToRotate.Rotate(rotationAngle, Space.World);
        }

        float currentY = targetToRotate.eulerAngles.y;
        float diffY = Mathf.DeltaAngle(currentY, correctAngleY);

        Debug.Log($"【回転中】ID[{objectID}] 現在のWorld_Y: {currentY:F1} (目標: {correctAngleY}) / ズレ: {diffY:F1}度");

        CheckAngleAndNotify(false);
    }

    // ★変更：開始時か回転中かを判定する isStart を追加
    private void CheckAngleAndNotify(bool isStart = false)
    {
        if (targetToRotate == null || manager == null) return;

        float currentY = targetToRotate.eulerAngles.y;
        float diffY = Mathf.DeltaAngle(currentY, correctAngleY);

        // ★追加：ゲーム開始時に、どうしてその判定になったかをログに出す
        if (isStart)
        {
            Debug.Log($"【開始時チェック】ID[{objectID}] 現在のWorld_Y: {currentY:F1} (目標: {correctAngleY}) / ズレ: {diffY:F1}度");
        }

        if (Mathf.Abs(diffY) < 0.1f)
        {
            if (!isStart) Debug.Log($"★★★ オブジェクトID[{objectID}] が正解の角度に到達しました！ ★★★");
            manager.UpdateDirection(objectID, 1);
        }
        else
        {
            manager.UpdateDirection(objectID, 0);
        }
    }
}