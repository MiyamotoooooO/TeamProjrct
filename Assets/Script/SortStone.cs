using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortStone : MonoBehaviour
{
    [Header("石本体の配列")]
    [SerializeField] private GameObject[] stones1;
    [SerializeField] private GameObject[] stones2;

    [Header("石のMaterial")]
    [SerializeField] private Material black;
    [SerializeField] private Material white;
    [SerializeField] private Material selectBlack;
    [SerializeField] private Material selectWhite;

    [Header("石のレイヤー")]
    [SerializeField] private LayerMask stone;

    [Header("初期状態を格納する配列(確認用)")]
    [SerializeField] private Material[] default1;
    [SerializeField] private Material[] default2;

    [Header("解答を格納する配列(確認用)")]
    [SerializeField] private Material[] ans1;
    [SerializeField] private Material[] ans2;

    [Header("選択中の石を格納するリスト(確認用)")]
    [SerializeField] private List<GameObject> select;

    [Header("試行回数(確認用)")]
    [SerializeField] int count;

    [Header("試行回数上限")]
    [SerializeField] int maxCount1;
    [SerializeField] int maxCount2;

    void Start()
    {
        //配列とリストを初期化
        ans1 = new Material[stones1.Length];
        ans2 = new Material[stones2.Length];
        default1 = new Material[stones1.Length];
        default2 = new Material[stones2.Length];
        select = new List<GameObject>();

        //初期設定の色と逆の色を解答用配列"ans"に格納
        for (int i = 0; i < stones1.Length; i++)
        {
            MeshRenderer mr = stones1[i].GetComponent<MeshRenderer>();
            if (mr.sharedMaterial == black)
            {
                default1[i] = black;
                ans1[i] = white;
            }
            else if (mr.sharedMaterial == white)
            {
                default1[i] = white;
                ans1[i] = black;
            }
        }
        for (int i = 0; i < stones2.Length; i++)
        {
            MeshRenderer mr = stones2[i].GetComponent<MeshRenderer>();
            if (mr.sharedMaterial == black)
            {
                default2[i] = black;
                ans2[i] = white;
            }
            else if (mr.sharedMaterial == white)
            {
                default2[i] = white;
                ans2[i] = black;
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
        //選んだオブジェクトがstones1のものであるかどうか
        bool _bool = System.Array.Exists(stones1, obj => obj == select[0]);
        //選択中のオブジェクトを通常時の色に戻す
        foreach (GameObject obj in select)
            ColorChange(obj);
        //2つのオブジェクトの配列における配列番号を取得
        int indexA;
        int indexB;
        if (_bool)
        {
            indexA = Array.IndexOf(stones1, select[0]);
            indexB = Array.IndexOf(stones1, select[1]);
        }
        else
        {
            indexA = Array.IndexOf(stones2, select[0]);
            indexB = Array.IndexOf(stones2, select[1]);
        }
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
        foreach (GameObject obj in select)
        {
            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr.sharedMaterial == black)
                mr.material = white;
            else if (mr.sharedMaterial == white)
                mr.material = black;
        }
        select.Clear();
        //反転させたのち、色がansと一致していれば正解とする
        if (_bool)
        {
            if (CheckStone(stones1, ans1))
            {
                count = 0;
                Debug.Log("正解！");
            }
            else if (count >= maxCount1)    //この回の反転で不正解&試行回数が上限値に達していれば初期化する
                InitStone(stones1, default1);
        }
        else
        {
            if (CheckStone(stones2, ans2))
            {
                count = 0;
                Debug.Log("正解！");
            }
            else if (count >= maxCount2)    //この回の反転で不正解&試行回数が上限値に達していれば初期化する
                InitStone(stones2, default2);
        }
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