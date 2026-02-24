using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CameraHijackController : MonoBehaviour
{
    public static CameraHijackController Instance;
    public MonoBehaviour playerCameraController; // © ’Ç‰Á



    [Header("ƒJƒƒ‰")]
    public Transform playerCamera;
    public float moveSpeed = 10f;

    [Header("—h‚ê")]
    public float shakeDuration = 0.5f;
    public float shakePosAmount = 0.05f;
    public float shakeRotAmount = 3f;

    [Header("’â~ŠÔ")]
    public float holdTime = 0.2f;

    bool isPlaying = false;

    void Awake()
    {
        Instance = this;
    }

    public void PlayHijack(Transform cameraFacePoint, PlayerHealth playerHealth)
    {
        if (isPlaying) return;

        // š ƒ]ƒ“ƒr–{‘Ì‚ğæ“¾
        NavMeshAgent agent = cameraFacePoint.GetComponentInParent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.enabled = false; // © Š®‘S’â~
        }

        Animator anim = cameraFacePoint.GetComponentInParent<Animator>();
        if (anim != null)
        {
            anim.speed = 0f;
        }

        StartCoroutine(HijackRoutine(cameraFacePoint, playerHealth));
    }


    IEnumerator HijackRoutine(Transform targetPoint, PlayerHealth playerHealth)
    {
        isPlaying = true;


        // š ƒJƒƒ‰‘€ì’â~
        if (playerCameraController != null)
            playerCameraController.enabled = false;
        // š ‚±‚±‚©‚ç’Ç‰Á -------------------------

        Rigidbody rb = playerCamera.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        //CharacterController cc = playerCamera.GetComponentInParent<CharacterController>();
        //if (cc != null)
        //{
        //    cc.enabled = false;
        //}

        Collider col = playerCamera.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // š ‚±‚±‚Ü‚Å’Ç‰Á -------------------------

        // ˆÊ’u‚Æ‰ñ“]‚ğ•Û‘¶
        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;

        float t = 0f;

        // ƒXƒ€[ƒY‚É‹z‚¢Šñ‚¹‚é
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;

            playerCamera.position = Vector3.Lerp(
                startPos,
                targetPoint.position,
                t
            );

            playerCamera.rotation = Quaternion.Slerp(
                startRot,
                targetPoint.rotation,
                t
            );

            yield return null;
        }

        // —h‚êiÅ‰‹­‚­ ¨ ã‚­j
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;
            float strength = 1f - (timer / shakeDuration);

            Vector3 posOffset = Random.insideUnitSphere * shakePosAmount * strength;
            float noiseRot = Random.Range(-1f, 1f);
            Quaternion rotOffset =
                Quaternion.Euler(0f, 0f, noiseRot * shakeRotAmount * strength);

            playerCamera.position += posOffset;
            playerCamera.rotation *= rotOffset;

            yield return null;
        }

        // ’â~
        yield return new WaitForSeconds(holdTime);

        // €–S
        playerHealth.Die();

        isPlaying = false;
    }
}
