using UnityEngine;
using UnityEngine.AI;

public class StatueEnemy : MonoBehaviour
{
    [Header("--- 設定 ---")]
    public Transform player;
    public float moveSpeed = 10.0f;
    public float detectionRadius = 20.0f;

    [Tooltip("壁レイヤー（Wallなどを指定）。Everythingにして、自分(Enemy)のレイヤーを外すのがベスト")]
    public LayerMask obstacleLayer;

    private NavMeshAgent agent;
    private Renderer myRenderer;
    private Camera playerCamera;
    private bool hasCaughtPlayer = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        myRenderer = GetComponent<Renderer>();
        agent.speed = moveSpeed;
        agent.acceleration = 100f; // 加速度を上げてすぐに動くようにする

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (hasCaughtPlayer) return;

        if (player == null || playerCamera == null) return;

        // 状態チェック
        bool canConnect = CanEnemySeePlayer();
        bool isSeen = IsVisibleToPlayer();

        if (canConnect) // 壁などの遮りがない
        {
            if (isSeen)
            {
                // 見られている → 止まる
                StopMoving("見られている！");
                myRenderer.material.color = Color.white;
            }
            else
            {
                // 見られていない → 動く！
                StartChasing("追跡中！");
                myRenderer.material.color = Color.red;
            }
        }
        else
        {
            // 壁がある or 遠い → 止まる
            StopMoving("壁がある（または自分に当たってる）");
            myRenderer.material.color = Color.gray;
        }
    }

    // --- 判定ロジック ---

    bool IsVisibleToPlayer()
    {
        // カメラの視界内に入っているか
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        if (!GeometryUtility.TestPlanesAABB(planes, myRenderer.bounds)) return false;

        // 壁判定（プレイヤーの目から敵の中心へ）
        Vector3 dir = transform.position - playerCamera.transform.position;
        if (Physics.Raycast(playerCamera.transform.position, dir, out RaycastHit hit, dir.magnitude, obstacleLayer))
        {
            if (hit.transform != transform) return false; // 手前に壁がある
        }
        return true;
    }

    bool CanEnemySeePlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRadius) return false;

        // 足元ではなく、少し上（目の高さ）からレイを飛ばす
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 target = playerCamera.transform.position;
        Vector3 direction = target - origin;

        RaycastHit hit;

        // レイを飛ばす
        if (Physics.Raycast(origin, direction, out hit, dist, obstacleLayer))
        {
            // パターン1：自分自身に当たった（無視してOK）
            if (hit.transform == transform) return true;

            // ★パターン2：プレイヤーに当たった（壁じゃないからOK！）
            // （タグがPlayer、またはプレイヤーのオブジェクトそのものなら通す）
            if (hit.transform.CompareTag("Player") || hit.transform == player || hit.transform.root == player.root)
            {
                Debug.DrawLine(origin, target, Color.green); // 緑線＝見える！
                return true;
            }

            // パターン3：それ以外に当たった（これは本当に壁だ！）
            Debug.DrawLine(origin, hit.point, Color.red); // 赤線＝壁がある
            // 何に当たって止まったかログに出す（デバッグ用）
            Debug.Log("壁判定で停止中。当たったもの: " + hit.transform.name);
            return false;
        }

        // 何にも当たらず届いた（OK）
        Debug.DrawLine(origin, target, Color.green);
        return true;
    }

    // --- 移動制御 ---

    void StartChasing(string reason)
    {
        if (agent.isStopped)
        {
            agent.isStopped = false;
            // Debug.Log(reason); // うるさいのでコメントアウト
        }
        agent.SetDestination(player.position);
    }

    void StopMoving(string reason)
    {
        if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            // Debug.Log(reason); // うるさいのでコメントアウト
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 既に捕まえているなら何もしない（何度もDieを呼ばないように）
        if (hasCaughtPlayer) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // 1. フラグをオンにする
            hasCaughtPlayer = true;

            // 2. 完全に動きを止める
            if (agent != null)
            {
                agent.isStopped = true;       // 移動禁止
                agent.velocity = Vector3.zero; // 勢いを殺す
            }

            // 3. アニメーションがあればここで止める（Cubeなら不要）
            // GetComponent<Animator>().enabled = false; 

            // 4. プレイヤーを殺す
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                Debug.Log("捕まった！停止します。");
                health.Die();
            }
        }
    }
}