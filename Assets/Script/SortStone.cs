using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortStone : MonoBehaviour
{
    [Header("石本体の配列")]
    public GameObject[] stones;

    [Header("石のMaterial")]
    [SerializeField] private Material black;
    [SerializeField] private Material white;
    [SerializeField] private Material selectBlack;
    [SerializeField] private Material selectWhite;

    [Header("初期状態を格納する配列(確認用)")]
    [SerializeField] private Material[] initial;

    [Header("解答を格納する配列(確認用)")]
    [SerializeField] private Material[] ans;

    [Header("選択中の石を格納するリスト(確認用)")]
    [SerializeField] private List<GameObject> select;

    [Header("試行回数(確認用)")]
    [SerializeField] private int count;

    [Header("試行回数上限")]
    [SerializeField] private int maxCount;

    void Start()
    {
        //配列とリストを初期化
        ans = new Material[stones.Length];
        initial = new Material[stones.Length];
        select = new List<GameObject>();

        //初期設定の色と逆の色を解答用配列"ans"に格納
        for (int i = 0; i < stones.Length; i++)
        {
            MeshRenderer mr = stones[i].GetComponent<MeshRenderer>();
            if (mr.sharedMaterial == black)
            {
                initial[i] = black;
                ans[i] = white;
            }
            else if (mr.sharedMaterial == white)
            {
                initial[i] = white;
                ans[i] = black;
            }
        }
    }

    //Playerから石をクリックしたときに呼ぶ関数
    public void Stone(GameObject hit)
    {
        //選択中の色に変える関数を呼ぶ
        ColorChange(hit);
        //選択中のリストに入れる
        select.Add(hit);
        //選択中のリストの中で2つ目だったらswapを呼ぶ
        if (select.Count >= 2)
            swap();
    }

    private void ColorChange(GameObject obj)
    {
        //オブジェクトのレンダラーを格納
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        //現在の色に応じて対応した色に変える
        if (mr.sharedMaterial == black)
            mr.material = selectBlack;
        else if (mr.sharedMaterial == white)
            mr.material = selectWhite;
        else if (mr.sharedMaterial == selectBlack)
            mr.material = black;
        else if (mr.sharedMaterial == selectWhite)
            mr.material = white;
    }

    private void swap()
    {
        //選択中のオブジェクトを通常時の色に戻す
        foreach (GameObject obj in select)
            ColorChange(obj);
        //2つのオブジェクトの配列における配列番号を取得
        int indexA = Array.IndexOf(stones, select[0]);
        int indexB = Array.IndexOf(stones, select[1]);
        //番号が隣り合っていなければselectを空にして関数を抜ける
        if (Mathf.Abs(indexA - indexB) != 1)
        {
            foreach (GameObject obj in select)
            {
                select.Clear();
                return;
            }
        }

        //試行回数に反映する
        count++;

        //2つのオブジェクトの色を反転させる
        MeshRenderer mr0 = select[0].GetComponent<MeshRenderer>();
        MeshRenderer mr1 = select[1].GetComponent<MeshRenderer>();
        Material temp = mr0.sharedMaterial;
        mr0.material = mr1.sharedMaterial;
        mr1.material = temp;
        select.Clear();
        //反転させたのち、色がansと一致していれば正解とする
        if (CheckStone(stones, ans))
        {
            count = 0;
            Debug.Log("正解！");
        }
        else if (count >= maxCount)    //この回の反転で不正解&試行回数が上限値に達していれば初期化する
            InitStone(stones, initial);
    }

    private void InitStone(GameObject[] stones, Material[] defaults)
    {
        for (int i = 0; i < stones.Length; i++)
            stones[i].GetComponent<MeshRenderer>().material = defaults[i];
        count = 0;
    }

    private bool CheckStone(GameObject[] stone, Material[] ans)
    {
        for (int i = 0; i < stone.Length; i++)
        {
            //マテリアルが一つでもansと違っていたらfalseを返す
            if (stone[i].GetComponent<MeshRenderer>().sharedMaterial != ans[i])
                return false;
        }
        //完全一致でtrueを返す
        return true;
    }
}