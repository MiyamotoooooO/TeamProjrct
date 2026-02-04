using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BugMove : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 target;

    [SerializeField] private float speed = 300f;
    [SerializeField] private float lifeTime = 3f;

    private Vector2 screenSize;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        screenSize = new Vector2(
            Screen.width / 2f,
            Screen.height / 2f
        );
    }

    public void StartMove()
    {
        rect.anchoredPosition = GetStartOutsidePos();
        target = GetRandomInsidePos();
        StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        float timer = 0f;

        while (timer < lifeTime)
        {
            rect.anchoredPosition = Vector2.MoveTowards(
                rect.anchoredPosition,
                target,
                speed * Time.deltaTime
            );

            // 目的地に近づいたら次へ
            if (Vector2.Distance(rect.anchoredPosition, target) < 20f)
            {
                target = GetRandomInsidePos();
            }

            // 羽虫特有のブレ
            rect.anchoredPosition += Random.insideUnitCircle * 5f;

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private Vector2 GetRandomInsidePos()
    {
        return new Vector2(
            Random.Range(-screenSize.x, screenSize.x),
            Random.Range(-screenSize.y, screenSize.y)
        );
    }

    private Vector2 GetStartOutsidePos()
    {
        int side = Random.Range(0, 4);
        switch (side)
        {
            case 0: return new Vector2(-screenSize.x - 100, Random.Range(-screenSize.y, screenSize.y)); // 左
            case 1: return new Vector2(screenSize.x + 100, Random.Range(-screenSize.y, screenSize.y));  // 右
            case 2: return new Vector2(Random.Range(-screenSize.x, screenSize.x), screenSize.y + 100);  // 上
            default: return new Vector2(Random.Range(-screenSize.x, screenSize.x), -screenSize.y - 100); // 下
        }
    }
}