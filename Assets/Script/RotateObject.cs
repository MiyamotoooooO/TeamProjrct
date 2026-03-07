using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public PuzzleRotateManager manager;
    public int objectID; // 0,1,2の番号

    [Header("【新規】対象と判定の設定")]
    [Tooltip("クリックした時に実際に回転・色変えさせたいオブジェクトをアタッチ")]
    public Transform targetToRotate;

    [Tooltip("当たり判定に使うBoxCollider（複数登録できます）")]
    public BoxCollider[] interactColliders;

    [Header("正解の向き（0 = 前, 1 = 右, 2 = 後, 3 = 左）")]
    public int correctDirection;

    [Header("回転設定")]
    [Tooltip("1回のクリックで回転する角度。Y軸に回すなら Y を 90 や -90 に設定します")]
    public Vector3 rotationAngle = new Vector3(0, 90f, 0);

    [Header("光る色設定")]
    public Color normalColor = Color.gray;
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f); // クロスヘアが合っている時の色

    private Renderer rend;
    private int currentDirection = 0;

    private bool isHovered = false; // クロスヘアが合っているか

    public void Start()
    {
        // もしInspectorで回転対象がセットされていなければ、自分自身を対象にする
        if (targetToRotate == null)
        {
            targetToRotate = transform;
        }

        // もしInspectorでColliderがセットされていなければ、自分についているものをすべて自動取得する
        if (interactColliders == null || interactColliders.Length == 0)
        {
            interactColliders = GetComponents<BoxCollider>();
        }

        // 色を変えるためのRendererを取得（回転対象のオブジェクトから取得）
        rend = targetToRotate.GetComponent<Renderer>();
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }

        if (rend != null)
        {
            rend.material.color = normalColor;
        }

        manager.SetCorrectDirection(objectID, correctDirection);
    }

    private void Update()
    {
        if (rend == null) return;

        // クロスヘアが合っていればホバー色、外れれば通常色
        if (isHovered)
        {
            rend.material.color = hoverColor;
        }
        else
        {
            rend.material.color = normalColor;
        }

        // 毎フレーム解除（クロスヘアが合っていれば OnHover() で再びtrueになる）
        isHovered = false;
    }

    // クロスヘアが向いている時に外部（ItemUse等）から呼ばれる
    public void OnHover()
    {
        isHovered = true;
    }

    public void RotateLeft()
    {
        // 自分自身ではなく、指定した対象を回転させる
        if (targetToRotate != null)
        {
            // Space.World を指定することで、モデルがどんな風に傾いていても
            // 常に「見た目上の縦軸（Y軸）」を基準に回転するようになります
            targetToRotate.Rotate(rotationAngle, Space.World);
        }

        // 向きの番号を更新(0～3)
        currentDirection = (currentDirection + 1) % 4;

        // マネージャーに通知
        manager.UpdateDirection(objectID, currentDirection);
    }
}