using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseCursor : MonoBehaviour
{
    private void Start()
    {
        Cursor.visible = true; // カーソルを表示
        Cursor.lockState = CursorLockMode.None; // ロック解除
    }
}
