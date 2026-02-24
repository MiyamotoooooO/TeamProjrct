using UnityEngine;

public class AttackPoint : MonoBehaviour
{
    [Header("ゾンビ本体のAnimatorを指定してください")]
    public Animator zombieAnimator;

    bool hasHit = false;

    [Header("カメラポイント")]
    public Transform cameraFacePoint;

    private void OnEnable()
    {
        hasHit = false;
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player")) return;

        EnemyAI enemy = GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.StopChaseSound(); // ★ ここで止める
        }

        if (hasHit) return;
        if (zombieAnimator == null || !IsAttacking()) return;

        hasHit = true;

        PlayerHealth hp = other.GetComponent<PlayerHealth>();
        if (hp != null && CameraHijackController.Instance != null)
        {
            CameraHijackController.Instance.PlayHijack(
                cameraFacePoint,   // ← 顔前の空オブジェクト
                hp
            );
        }
    }

    bool IsAttacking()
    {
        AnimatorStateInfo stateInfo =
            zombieAnimator.GetCurrentAnimatorStateInfo(0);

        return stateInfo.IsName("Z_Attack");
    }

}
