using UnityEngine;
using UnityEngine.UI;

public class AlwaysOnTop : MonoBehaviour
{
    [Header("表示順の設定")]
    [Tooltip("この数字が大きいほど手前に来る")]
    public int sortingOrder = 30000;

    void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        // 親のCanvasの設定を無視して、独自の並び順を持つようにする
        canvas.overrideSorting = true;

        // 並び順を最強にする
        canvas.sortingOrder = sortingOrder;

        // 4. Raycastを受け取るためのコンポーネントを追加
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}