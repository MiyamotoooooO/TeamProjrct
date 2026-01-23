using UnityEngine;
using System.Collections; // ★コルーチン(待機処理)のために必要
using System.Collections.Generic;

public class LighterSystem : MonoBehaviour
{
    [Header("--- 制御設定 ---")]
    [Tooltip("最初はライターを使えないようにするか？")]
    public bool canUseLighter = false;

    [Header("--- 基本設定 ---")]
    public ParticleSystem fireParticle;
    public Light fireLight;
    public AudioSource lighterSound;

    [Header("--- 音声設定(追加) ---")]
    [Tooltip("カチッという点火音")]
    public AudioClip ignitionClip;
    [Tooltip("燃えている間のループ音")]
    public AudioClip burningClip;

    [Header("--- 描画設定 ---")]
    public GameObject scorchPrefab;
    public Transform firePoint;
    public LayerMask drawingLayer;
    public float drawDistance = 1.5f;
    public float drawRate = 0.05f;

    [Header("--- アニメーション設定 ---")]
    public Vector3 drawingPosOffset = new Vector3(0.1f, -0.1f, 0.2f);
    public Vector3 drawingRotOffset = new Vector3(15f, -10f, 0f);

    [Header("--- 炙り出し設定 ---")]
    public LayerMask hiddenTextLayer;
    public float smoothSpeed = 10f;
    public bool isLighterOn = false;

    // private
    private float nextDrawTime = 0f;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Transform targetTransform;
    private List<GameObject> drawnMarks = new List<GameObject>();
    private bool isIgniting = false; // 点火モーション中かどうかのフラグ

    void Start()
    {
        targetTransform = this.transform;
        initialLocalPos = targetTransform.localPosition;
        initialLocalRot = targetTransform.localRotation;

        // AudioSourceのループ設定を初期化（燃焼音はループさせるため）
        if (lighterSound != null) lighterSound.loop = false;

        isLighterOn = false;
        TurnOff();
    }

    void Update()
    {
        if (!canUseLighter)
        {
            if (isLighterOn)
            {
                TurnOff();
            }
            return;
        }

        // Tキーで点火・消火
        // ※ isIgniting(点火動作中)なら操作を受け付けないようにする
        if (Input.GetKeyDown(KeyCode.T) && !isIgniting)
        {
            if (!isLighterOn)
            {
                // まだ付いてないなら、点火プロセス開始
                StartCoroutine(IgniteProcess());
            }
            else
            {
                // もう付いてるなら消す
                TurnOff();
            }
        }

        // Mキーでリセット（全部消す）
        if (Input.GetKeyDown(KeyCode.M))
        {
            ClearAllMarks();
        }

        HandleDrawingAndAnimation();
    }

    // ★追加：時間差で火をつけるコルーチン
    IEnumerator IgniteProcess()
    {
        isIgniting = true; // 操作ロック開始

        // 1. カチッという音を鳴らす
        if (lighterSound != null && ignitionClip != null)
        {
            lighterSound.loop = false;
            lighterSound.clip = ignitionClip;
            lighterSound.Play();

            // 音の長さ分だけ待機する
            yield return new WaitForSeconds(ignitionClip.length);
        }
        else
        {
            // 音がない場合は一瞬だけ待つ（即時着火だと違和感がある場合）
            yield return new WaitForSeconds(0.1f);
        }

        // 2. 待機が終わったら火をつける
        isLighterOn = true;
        if (fireParticle != null) fireParticle.Play();
        if (fireLight != null) fireLight.enabled = true;

        // 3. 燃焼音に切り替えてループ再生する
        if (lighterSound != null && burningClip != null)
        {
            lighterSound.clip = burningClip;
            lighterSound.loop = true; // ループ有効化
            lighterSound.Play();
        }

        isIgniting = false; // 操作ロック解除
    }

    void HandleDrawingAndAnimation()
    {
        Vector3 targetPos = initialLocalPos;
        Quaternion targetRot = initialLocalRot;

        if (isLighterOn && Input.GetMouseButton(1))
        {
            targetPos = initialLocalPos + drawingPosOffset;
            targetRot = initialLocalRot * Quaternion.Euler(drawingRotOffset);

            if (Time.time >= nextDrawTime)
            {
                DrawScorchMark();
                nextDrawTime = Time.time + drawRate;
            }

            // 隠し文字の炙り出し処理
            RevealText();
        }

        targetTransform.localPosition = Vector3.Lerp(targetTransform.localPosition, targetPos, Time.deltaTime * smoothSpeed);
        targetTransform.localRotation = Quaternion.Slerp(targetTransform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
    }

    void DrawScorchMark()
    {
        Vector3 startPoint = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 direction = (firePoint != null) ? firePoint.forward : transform.forward;

        RaycastHit hit;

        Debug.DrawRay(startPoint, direction * drawDistance, Color.red, 0.1f);

        if (Physics.Raycast(startPoint, direction, out hit, drawDistance, drawingLayer))
        {
            GameObject newMark = Instantiate(scorchPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal));
            drawnMarks.Add(newMark);
        }
    }

    public void RevealText()
    {
        Vector3 startPoint = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 direction = (firePoint != null) ? firePoint.forward : transform.forward;
        RaycastHit hit;
        float beamRadius = 0.3f;

        Debug.DrawRay(startPoint, direction * drawDistance, Color.red, 0.1f);

        if (Physics.SphereCast(startPoint, beamRadius, direction, out hit, drawDistance, hiddenTextLayer))
        {
            HiddenTextReveal targetText = hit.collider.GetComponent<HiddenTextReveal>();
            if (targetText != null)
            {
                targetText.ReceiveHeat();
            }
        }
    }

    void ClearAllMarks()
    {
        foreach (GameObject mark in drawnMarks)
        {
            if (mark != null) Destroy(mark);
        }
        drawnMarks.Clear();
        Debug.Log("焦げ跡をリセットしました");
    }

    // publicだが、内部処理ではIgniteProcessを使うため、これは主にTurnOff用や外部強制ON用
    public void TurnOn()
    {
        // 外部から強制的に呼ばれた場合、音の待機なしでつける
        isLighterOn = true;
        if (fireParticle != null) fireParticle.Play();
        if (fireLight != null) fireLight.enabled = true;
    }

    public void TurnOff()
    {
        // もし点火待ちの最中に消されたら、待機処理を強制停止する
        StopAllCoroutines();
        isIgniting = false;
        isLighterOn = false;

        if (fireParticle != null) fireParticle.Stop();
        if (fireLight != null) fireLight.enabled = false;

        // 音を止める（ループしている燃焼音を止める）
        if (lighterSound != null)
        {
            lighterSound.Stop();
            lighterSound.loop = false;
        }
    }
}