using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    [Header("正解の順番")]
    public int[] answer = { 2, 4, 1, 3 };
    private int progress = 0;

    [Header("正解時に出す鍵")]
    public GameObject KeyPrefab;
    public Transform spawnPoint;

    public void InputButton(int id, PuzzleButton button)
    {
        // 正しいボタンか
        if (id == answer[progress])
        {
            progress++;

            // 全問正解した
            if (progress >= answer.Length)
            {
                Debug.Log("Puzzle Clear");
                Instantiate(KeyPrefab, spawnPoint.position, Quaternion.identity);
                progress = 0;
            }
        }
        else
        {
            Debug.Log("Miss リセット");
            button.GlowWrong();
            progress = 0;
        }
    }
}
