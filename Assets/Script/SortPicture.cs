using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortPicture : MonoBehaviour
{
    [Header("絵画本体の配列")]
    public GameObject[] pictures;

    [Header("解答を格納する配列")]
    [SerializeField] private Material[] ans;

    [Header("絵画のMaterial")]
    [SerializeField] private Material[] nomalMaterial;
    [SerializeField] private Material[] selectMaterial;

    [Header("初期状態を格納する配列(確認用)")]
    [SerializeField] private Material[] initial;

    [Header("選択中の絵画を格納するリスト(確認用)")]
    [SerializeField] private List<GameObject> select;

    [Header("試行回数(確認用)")]
    [SerializeField] private int count;

    [Header("試行回数上限")]
    [SerializeField] private int maxCount;
    void Start()
    {
        //配列とリストを初期化
        select = new List<GameObject>();
        initial = new Material[pictures.Length];

        for (int i = 0; i < pictures.Length; i++)
            initial[i] = pictures[i].GetComponent<MeshRenderer>().sharedMaterial;
    }

    //Playerから絵画をクリックしたときに呼ぶ関数
    public void Picture(GameObject hit)
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
        for (int i = 0; i < nomalMaterial.Length; i++)
        {
            if (mr.sharedMaterial == nomalMaterial[i])
            {
                mr.material = selectMaterial[i];
                return;
            }
            if (mr.sharedMaterial == selectMaterial[i])
            {
                mr.material = nomalMaterial[i];
                return;
            }
        }
    }

    private void swap()
    {
        //試行回数に反映する
        count++;
        //選択中のオブジェクトを通常時の色に戻す
        foreach (GameObject obj in select)
            ColorChange(obj);
        //2つのオブジェクトの色を反転させる
        MeshRenderer mr0 = select[0].GetComponent<MeshRenderer>();
        MeshRenderer mr1 = select[1].GetComponent<MeshRenderer>();
        Material temp = mr0.sharedMaterial;
        mr0.material = mr1.sharedMaterial;
        mr1.material = temp;
        select.Clear();
        //反転させたのち、色がansと一致していれば正解とする
        if (CheckPicture(pictures, ans))
        {
            count = 0;
            Debug.Log("正解！");
        }
        else if (count >= maxCount)     //この回の反転で不正解&試行回数が上限値に達していれば初期化する
        {
            for (int i = 0; i < pictures.Length; i++)
                pictures[i].GetComponent<MeshRenderer>().material = initial[i];
            count = 0;
        }
    }

    private bool CheckPicture(GameObject[] pictures, Material[] ans)
    {
        for (int i = 0; i < pictures.Length; i++)
        {
            //マテリアルが一つでもansと違っていたらfalseを返す
            if (pictures[i].GetComponent<MeshRenderer>().sharedMaterial != ans[i])
                return false;
        }
        //完全一致でtrueを返す
        return true;
    }
}