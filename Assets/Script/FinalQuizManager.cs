using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(AudioSource))]
public class FinalQuizManager : MonoBehaviour
{
    [Header("UIとエフェクト設定")]
    public GameObject quizUIContainer;
    public PostProcessVolume blurVolume;
    public GameObject questionGroup;
    public CanvasGroup answerCanvasGroup;

    [Header("ボタン設定（運命の選択）")]
    public Button[] correctButtons;
    public Button[] wrongButtons;

    [Header("【重要】敵の演出設定")]
    public GameObject enemyModel;

    [Tooltip("クイズ中にうっすら見える位置")]
    public Transform shadowPosition;

    [Tooltip("目の前に現れる位置")]
    public Transform closePosition;

    [Tooltip("★プレイヤーの顔面に飛んでくる部位（敵の顔や手などに設置した空オブジェクト）")]
    public Transform attackPoint;

    [Header("オーディオ設定")]
    [Tooltip("不正解時、敵が現れてUIを壊す音")]
    public AudioClip uiBreakSound;
    [Tooltip("飛びかかってくる時のジャンプスケア音")]
    public AudioClip jumpScareSound;

    [Header("タイマー・エリア設定")]
    public float intervalTime = 180f;
    public BoxCollider[] activeAreas;
    public bool isTimerActive = true;

    [Header("時間・カメラ設定")]
    [Tooltip("クイズ開始時、カメラが向く上下の角度（マイナスで上向き、プラスで下向き。例：-15 で少し上を向く）")]
    public float quizCameraPitchAngle = -15f;

    [Tooltip("不正解時に敵が飛びかかってくるスピード（秒）")]
    public float jumpScareDuration = 0.2f;

    [Tooltip("敵についているAnimator")]
    public Animator enemyAnimator;

    // 内部変数
    private float timer = 0f;
    private bool isQuizTriggered = false;
    private AudioSource audioSource;

    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private WakeUpController wakeUpController;

    private Vector3 actualShadowPos;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        playerController = FindAnyObjectByType<PlayerController>();
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        wakeUpController = FindAnyObjectByType<WakeUpController>();

        if (quizUIContainer != null) quizUIContainer.SetActive(false);
        if (answerCanvasGroup != null) answerCanvasGroup.gameObject.SetActive(false);
        if (enemyModel != null) enemyModel.SetActive(false);

        foreach (Button correctBtn in correctButtons)
        {
            if (correctBtn != null) correctBtn.onClick.AddListener(OnCorrectAnswer);
        }

        foreach (Button wrongBtn in wrongButtons)
        {
            if (wrongBtn != null) wrongBtn.onClick.AddListener(OnWrongAnswer);
        }
    }

    void Update()
    {
        if (!isTimerActive || isQuizTriggered) return;

        // プレイヤーが死んでいる時、または寝ている時はタイマーを進めない
        if (playerHealth != null && playerHealth.isDead) return;
        if (wakeUpController != null && (wakeUpController.isSleeping || wakeUpController.isWakingUp)) return;

        if (playerController != null && IsPlayerInAnyArea())
        {
            timer += Time.deltaTime;
            if (timer >= intervalTime)
            {
                StartCoroutine(QuizStartSequence());
            }
        }
    }

    private bool IsPlayerInAnyArea()
    {
        if (activeAreas == null || activeAreas.Length == 0) return false;
        foreach (var area in activeAreas)
        {
            if (area != null && area.bounds.Contains(playerController.transform.position)) return true;
        }
        return false;
    }

    private Vector3 GetSafePosition(Vector3 startPos, Vector3 targetPos)
    {
        Vector3 direction = targetPos - startPos;
        float distance = direction.magnitude;
        RaycastHit hit;

        if (Physics.Raycast(startPos, direction.normalized, out hit, distance))
        {
            return hit.point - (direction.normalized * 0.5f);
        }
        return targetPos;
    }

    private Vector3 GetGroundedPosition(Vector3 pos)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(pos.x, pos.y + 2f, pos.z), Vector3.down, out hit, 10f))
        {
            return hit.point;
        }
        return pos;
    }

    private IEnumerator QuizStartSequence()
    {
        isQuizTriggered = true;

        if (playerController != null) playerController.canControl = false;

        if (playerController != null && playerController.cam != null)
        {
            Quaternion startCamRot = playerController.cam.transform.localRotation;
            Quaternion targetCamRot = Quaternion.Euler(quizCameraPitchAngle, 0f, 0f);

            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                playerController.cam.transform.localRotation = Quaternion.Slerp(startCamRot, targetCamRot, t);
                playerController.SyncRotationToCurrent();
                yield return null;
            }
            playerController.cam.transform.localRotation = targetCamRot;
            playerController.SyncRotationToCurrent();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        if (quizUIContainer != null) quizUIContainer.SetActive(true);
        if (questionGroup != null) questionGroup.SetActive(true);

        if (blurVolume != null)
        {
            blurVolume.gameObject.SetActive(true);
            blurVolume.weight = 1f;
        }

        if (enemyModel != null && shadowPosition != null && playerController != null && playerController.cam != null)
        {
            Transform camTransform = playerController.cam.transform;

            actualShadowPos = GetSafePosition(camTransform.position, shadowPosition.position);
            actualShadowPos = GetGroundedPosition(actualShadowPos);

            enemyModel.transform.position = actualShadowPos;

            Vector3 lookTarget = new Vector3(camTransform.position.x, enemyModel.transform.position.y, camTransform.position.z);
            enemyModel.transform.LookAt(lookTarget);

            enemyModel.SetActive(true);
        }
    }

    public void OnCorrectAnswer()
    {
        CorrectSequenceImmediate();
    }

    private void CorrectSequenceImmediate()
    {
        if (quizUIContainer != null) quizUIContainer.SetActive(false);
        if (blurVolume != null) blurVolume.weight = 0f;
        if (answerCanvasGroup != null) answerCanvasGroup.alpha = 0f;

        Time.timeScale = 1f;
        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.UpdateCursorLock();
        }

        // ★追加：正解した瞬間、敵のアニメーターを完全にオフ（無効化）にして微動だにさせない
        if (enemyAnimator != null)
        {
            enemyAnimator.enabled = false;
        }

        if (enemyModel != null && closePosition != null && playerController != null)
        {
            Vector3 safeClosePos = GetSafePosition(playerController.cam.transform.position, closePosition.position);
            safeClosePos = GetGroundedPosition(safeClosePos);
            enemyModel.transform.position = safeClosePos;

            Vector3 lookTarget = new Vector3(playerController.cam.transform.position.x, enemyModel.transform.position.y, playerController.cam.transform.position.z);
            enemyModel.transform.LookAt(lookTarget);
        }
    }

    public void OnWrongAnswer()
    {
        StartCoroutine(WrongSequence());
    }

    private IEnumerator WrongSequence()
    {
        Time.timeScale = 1f;

        Vector3 safeClosePos = enemyModel.transform.position;
        if (enemyModel != null && closePosition != null && playerController != null)
        {
            safeClosePos = GetSafePosition(playerController.cam.transform.position, closePosition.position);
            safeClosePos = GetGroundedPosition(safeClosePos);
            enemyModel.transform.position = safeClosePos;

            Vector3 lookTarget = new Vector3(playerController.cam.transform.position.x, enemyModel.transform.position.y, playerController.cam.transform.position.z);
            enemyModel.transform.LookAt(lookTarget);

            enemyModel.SetActive(true);

            if (enemyAnimator != null)
            {
                enemyAnimator.SetTrigger("Punch");
            }
        }

        if (audioSource != null && uiBreakSound != null) audioSource.PlayOneShot(uiBreakSound);

        List<RectTransform> uiElements = new List<RectTransform>();
        if (questionGroup != null) uiElements.Add(questionGroup.GetComponent<RectTransform>());
        foreach (var btn in correctButtons) if (btn != null) uiElements.Add(btn.GetComponent<RectTransform>());
        foreach (var btn in wrongButtons) if (btn != null) uiElements.Add(btn.GetComponent<RectTransform>());

        Vector3[] randomDirs = new Vector3[uiElements.Count];
        Vector3[] startPos = new Vector3[uiElements.Count];
        for (int i = 0; i < uiElements.Count; i++)
        {
            randomDirs[i] = new Vector3(Random.Range(-1500f, 1500f), Random.Range(-1500f, 1500f), 0f);
            if (uiElements[i] != null) startPos[i] = uiElements[i].anchoredPosition;
        }

        Vector3 enemyStartPos = safeClosePos;
        Quaternion enemyStartRot = enemyModel.transform.rotation;

        Vector3 windupPos = enemyStartPos + (enemyModel.transform.forward * -0.5f) + (Vector3.up * 0.2f);
        Quaternion windupRot = enemyStartRot * Quaternion.Euler(20f, 0, 0);

        float blowDuration = 0.4f;
        float elapsed = 0f;

        while (elapsed < blowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / blowDuration;

            for (int i = 0; i < uiElements.Count; i++)
            {
                if (uiElements[i] != null)
                {
                    uiElements[i].anchoredPosition = Vector3.Lerp(startPos[i], startPos[i] + randomDirs[i], t);
                    uiElements[i].Rotate(0, 0, 1000f * Time.deltaTime);
                }
            }

            if (enemyModel != null)
            {
                enemyModel.transform.position = Vector3.Lerp(enemyStartPos, windupPos, t);
                enemyModel.transform.rotation = Quaternion.Slerp(enemyStartRot, windupRot, t);
            }

            yield return null;
        }

        if (quizUIContainer != null) quizUIContainer.SetActive(false);
        if (blurVolume != null) blurVolume.weight = 0f;

        if (audioSource != null && jumpScareSound != null) audioSource.PlayOneShot(jumpScareSound);

        float attackDuration = jumpScareDuration;
        elapsed = 0f;

        if (enemyModel != null && playerController != null)
        {
            Vector3 targetFacePos = playerController.cam.transform.position;
            Quaternion attackRot = enemyStartRot * Quaternion.Euler(-30f, 0, 0);

            Vector3 targetRootPos = targetFacePos;
            if (attackPoint != null)
            {
                Vector3 localOffset = enemyModel.transform.InverseTransformPoint(attackPoint.position);
                targetRootPos = targetFacePos - (attackRot * localOffset);
            }

            while (elapsed < attackDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / attackDuration;

                t = t * t * t;

                enemyModel.transform.position = Vector3.Lerp(windupPos, targetRootPos, t);
                enemyModel.transform.rotation = Quaternion.Slerp(windupRot, attackRot, t);

                yield return null;
            }
        }

        if (playerHealth != null)
        {
            playerHealth.Die();
        }
    }
}