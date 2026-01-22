using UnityEngine;
using System.Collections.Generic; // ★リストを使うために追加

public class LighterSystem : MonoBehaviour
{
    [Header("--- 制御設定 ---")]
    [Tooltip("最初はライターを使えないようにするか？")]
    public bool canUseLighter = false;

    [Header("--- 基本設定 ---")]
    public ParticleSystem fireParticle;
    public Light fireLight;
    public AudioSource lighterSound;

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

    void Start()
    {
        targetTransform = this.transform;
        initialLocalPos = targetTransform.localPosition;
        initialLocalRot = targetTransform.localRotation;
        isLighterOn = false;
        TurnOff();
    }

    void Update()
    {
        if (!canUseLighter)
        {
            // もし火がついていたら強制的に消す安全策
            if (isLighterOn)
            {
                isLighterOn = false;
                TurnOff();
            }
            return;
        }

        // Tキーで点火
        if (Input.GetKeyDown(KeyCode.T))
        {
            isLighterOn = !isLighterOn;
            if (isLighterOn)
            {
                TurnOn();
                if (lighterSound != null) lighterSound.Play();
            }
            else
            {
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

        // デバッグ線
        Debug.DrawRay(startPoint, direction * drawDistance, Color.red, 0.1f);

        if (Physics.Raycast(startPoint, direction, out hit, drawDistance, drawingLayer))
        {
            // 生成した焦げ跡を変数に入れる
            GameObject newMark = Instantiate(scorchPrefab, hit.point + (hit.normal * 0.05f), Quaternion.LookRotation(hit.normal));

            // リストに追加して覚えておく
            drawnMarks.Add(newMark);
        }
    }

    public void RevealText()
    {
        Vector3 startPoint = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 direction = (firePoint != null) ? firePoint.forward : transform.forward;
        RaycastHit hit;

        // ビームの太さ（半径）
        float beamRadius = 0.3f; // 30cmくらいの太さにする

        // シーンビューで線だけでなく、当たった場所がわかるようにする
        Debug.DrawRay(startPoint, direction * drawDistance, Color.red, 0.1f);

        // Raycast ではなく SphereCast を使う
        // これで「細い線」ではなく「太い円柱」が飛んでいくので、多少ズレてても当たります
        if (Physics.SphereCast(startPoint, beamRadius, direction, out hit, drawDistance, hiddenTextLayer))
        {
            HiddenTextReveal targetText = hit.collider.GetComponent<HiddenTextReveal>();
            if (targetText != null)
            {
                targetText.ReceiveHeat();
            }
        }
    }

    // 全部消す機能
    void ClearAllMarks()
    {
        // リストにあるものをひとつずつ破壊する
        foreach (GameObject mark in drawnMarks)
        {
            if (mark != null) Destroy(mark);
        }

        // リストを空にする
        drawnMarks.Clear();
        Debug.Log("焦げ跡をリセットしました");
    }

    public void TurnOn()
    {
        if (fireParticle != null) fireParticle.Play();
        if (fireLight != null) fireLight.enabled = true;
    }

    public void TurnOff()
    {
        if (fireParticle != null) fireParticle.Stop();
        if (fireLight != null) fireLight.enabled = false;
    }
}