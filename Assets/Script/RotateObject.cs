using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public PuzzleRotateManager manager;
    public int objectID; // 0,1,2の番号

    [Header("正解の向き（0 = 前, 1 = 右, 2 = 後, 3 = 左）")]
    public int correctDirection;

    [Header("光る色設定")]
    public Color normalColor = Color.gray;
    public Color glowColor = Color.yellow;
    public Color wrongColor = Color.red;

    private Renderer rend;
    private int currentDirection = 0;

    public void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = normalColor;

        manager.SetCorrectDirection(objectID, correctDirection);
    }

    public void RotateLeft()
    {
        // 左に90度回転
        transform.Rotate(0, -90f, 0);

        // 向きの番号を更新(0～3)
        currentDirection = (currentDirection + 1) % 4;

        GlowCorrect();

        // マネージャーに通知
        manager.UpdateDirection(objectID, currentDirection);
    }

    public void GlowCorrect()
    {
        rend.material.color = glowColor;
        Invoke(nameof(ResetColor), 0.3f);
    }

    public void GlowWrong()
    {
        rend.material.color = wrongColor;
        Invoke(nameof(ResetColor), 0.4f);
    }

    void ResetColor()
    {
        rend.material.color = normalColor;
    }
}

