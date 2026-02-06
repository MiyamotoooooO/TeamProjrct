using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleButton : MonoBehaviour
{
    public int buttonID; // ÇPÅ`ÇSÇÃî‘çÜ
    public PuzzleManager manager;

    [Header("åıÇÈêFê›íË")]
    public Color normalColor = Color.gray;
    public Color glowColor = Color.yellow;
    public Color wrongColor = Color.red;

    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = normalColor;
    }

    public void PressButton()
    {
        GlowCorrect();

        manager.InputButton(buttonID, this);
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

    public void ResetColor()
    {
        rend.material.color = normalColor;
    }
}
