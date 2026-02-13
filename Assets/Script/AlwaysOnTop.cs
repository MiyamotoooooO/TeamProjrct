using UnityEngine;
using UnityEngine.UI;

public class AlwaysOnTop : MonoBehaviour
{
    [Header("表示順の設定")]
    [Tooltip("この数字が大きいほど手前に来ます（最大32767）")]
    public int sortingOrder = 30000;

    void Start()
    {
        // 1. 自分自身に Canvas コンポーネントがあるか確認し、なければ追加する
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        // 2. 親のCanvasの設定を無視して、独自の並び順を持つようにする
        canvas.overrideSorting = true;

        // 3. 並び順を最強（30000）にする
        canvas.sortingOrder = sortingOrder;

        // 4. Raycast（クリック判定）を受け取るためのコンポーネントも追加
        // （Canvasを追加すると、これがないとクリックできなくなるため）
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}