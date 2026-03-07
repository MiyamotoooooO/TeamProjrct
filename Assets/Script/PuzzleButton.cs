using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleButton : MonoBehaviour
{
    public int buttonID; // １～４の番号
    public PuzzleManager manager;

    [Header("光る色設定")]
    public Color normalColor = Color.gray;
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f); // ★追加：クロスヘアが合っている時の色（明るいグレー）
    public Color glowColor = Color.yellow;
    public Color wrongColor = Color.red;

    private Renderer rend;
    private bool isHovered = false; // ★追加：今見つめられているか
    private bool isFlashing = false; // 正解/不正解で光っている最中か

    private void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = normalColor;
    }

    private void Update()
    {
        // ピカッと光っている最中は色を戻さない
        if (isFlashing) return;

        // クロスヘアが合っていればホバー色、外れれば通常色にする
        if (isHovered)
        {
            rend.material.color = hoverColor;
        }
        else
        {
            rend.material.color = normalColor;
        }

        // 毎フレーム解除する（クロスヘアが合っていればすぐ下の関数で再びtrueになる）
        isHovered = false;
    }

    // ★追加：クロスヘアが合っている時に外部から毎フレーム呼ばれる
    public void OnHover()
    {
        isHovered = true;
    }

    public void PressButton()
    {
        GlowCorrect();
        manager.InputButton(buttonID, this);
    }

    public void GlowCorrect()
    {
        isFlashing = true;
        rend.material.color = glowColor;
        Invoke(nameof(ResetColor), 0.3f);
    }

    public void GlowWrong()
    {
        isFlashing = true;
        rend.material.color = wrongColor;
        Invoke(nameof(ResetColor), 0.4f);
    }

    public void ResetColor()
    {
        isFlashing = false;
        // 色はUpdateメソッドで自動的に戻ります
    }
}