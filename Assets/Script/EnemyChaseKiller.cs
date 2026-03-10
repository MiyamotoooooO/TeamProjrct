using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio; // （任意）AudioMixerを使う場合
using UnityEngine.Rendering; // ShadowsOnly を使う場合

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class EnemyChaseKiller : MonoBehaviour
{
    //==============================
    // 参照
    //==============================
    [Header("参照")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator anim;
    [SerializeField] private NavMeshAgent agent;

    //==============================
    // 検知/移動
    //==============================
    [Header("検知/速度")]
    [SerializeField] private float detectRadius = 15f;
    [SerializeField] private float runRadius = 8f;
    [SerializeField] private float walkSpeed = 1.8f;
    [SerializeField] private float runSpeed = 3.6f;

    //==============================
    // Audio：追跡ループ / 攻撃ワンショット
    //==============================
    [Header("Audio: 追跡ループ / 攻撃ワンショット")]
    [Tooltip("追跡中に流すループSE")]
    [SerializeField] private AudioClip chaseLoopClip;
    [Tooltip("Kill開始時に鳴らすワンショット（任意）")]
    [SerializeField] private AudioClip killStingerClip;

    [Tooltip("追跡ループ用 AudioSource（空なら自動生成）")]
    [SerializeField] private AudioSource chaseLoopSource;
    [Tooltip("ワンショット用 AudioSource（空なら自動生成）")]
    [SerializeField] private AudioSource oneShotSource;

    [Tooltip("追跡SEのフェードイン時間")]
    [SerializeField] private float chaseFadeIn = 0.25f;
    [Tooltip("追跡SEのフェードアウト時間")]
    [SerializeField] private float chaseFadeOut = 0.25f;

    [Tooltip("3D音の最小距離（この距離内は音量MAX）")]
    [SerializeField] private float audioMinDistance = 1.5f;
    [Tooltip("3D音の最大距離（この距離を超えると0に）")]
    [SerializeField] private float audioMaxDistance = 12f;

    //==============================
    // キル演出：カメラ移動
    //==============================
    [Header("キル演出：カメラ移動")]
    [Tooltip("敵の顔の少し前に置いた Empty（カメラをここへ寄せる）")]
    [SerializeField] private Transform cameraFacePoint;
    [Tooltip("カメラがFacePointに到達するまでの時間(秒)")]
    [SerializeField] private float cameraMoveDuration = 0.1f;
    [Tooltip("FacePointに到達してから見せる時間(秒)")]
    [SerializeField] private float cameraHoldBeforeDie = 0.35f;
    [Tooltip("補間強度(位置/回転とも同じ・大きいほど素早い)")]
    [SerializeField] private float cameraLerp = 12f;

    [Tooltip("Kill中は一時的にプレイヤーからカメラを切り離す（戻り防止）")]
    [SerializeField] private bool detachCameraDuringKill = true;

    //==============================
    // キル演出：カメラシェイク
    //==============================
    [Header("キル演出：カメラシェイク（見せ時間中のみ）")]
    [SerializeField] private bool cameraShakeOnHold = true;
    [Tooltip("位置の揺れ振幅（メートル）")]
    [SerializeField] private Vector3 shakePosAmplitude = new Vector3(0.02f, 0.03f, 0.0f);
    [Tooltip("回転の揺れ振幅（度数）")]
    [SerializeField] private Vector3 shakeRotAmplitude = new Vector3(1.5f, 1.5f, 0.8f);
    [Tooltip("ノイズの周波数（Hz）")]
    [SerializeField] private float shakeFrequency = 9f;
    [Tooltip("見せ時間中にどれだけ減衰するか（0=一定, 1=最後に0まで）")]
    [Range(0f, 1f)][SerializeField] private float shakeDecay = 0.6f;

    //==============================
    // Animator速度のダンピング/ヒステリシス（壁詰まりでIdleに戻りにくく）
    //==============================
    [Header("Animator Speed Damp（壁詰まり対策）")]
    [Tooltip("Speedパラメータへの追従の速さ")]
    [SerializeField] private float speedDamp = 6f;
    [Tooltip("Idleへ落とす下限閾値")]
    [SerializeField] private float speedIdleThreshold = 0.08f;
    [Tooltip("追跡中に最低限維持する値（ガクつき防止）")]
    [SerializeField] private float speedRunBiasWhileChasing = 0.18f;
    private float speedParamCurrent = 0f;

    //==============================
    // カメラ衝突回避（壁めり込み防止）
    //==============================
    [Header("Camera Collision（壁めり込み防止）")]
    [Tooltip("壁/環境のレイヤーを設定（Player/Enemy/Trigger は除外）")]
    [SerializeField] private LayerMask cameraCollisionMask = ~0;
    [Tooltip("カメラ判定の球半径")]
    [SerializeField] private float cameraProbeRadius = 0.08f;
    [Tooltip("壁面から離す距離")]
    [SerializeField] private float cameraSurfaceClearance = 0.06f;
    [Tooltip("FacePointから前方に探査する最大距離")]
    [SerializeField] private float cameraMaxProbeDistance = 0.6f;

    //==============================
    // Kill直前の押し戻し（壁に顔が近い場合）
    //==============================
    [Header("Kill前の押し戻し（任意）")]
    [SerializeField] private bool backOffFromWallBeforeKill = true;
    [SerializeField] private float backOffCheckDistance = 0.3f;
    [SerializeField] private float backOffDistance = 0.35f;
    [SerializeField] private LayerMask wallMask = ~0;

    //==============================
    // 視線クリア確認（任意）
    //==============================
    [Header("視線クリアでのみKill（任意）")]
    [SerializeField] private bool requireLineOfSightForKill = true;
    [SerializeField] private float losWaitTimeout = 0.1f;

    //==============================
    // ★ 追加：Kill中のプレイヤー表示/衝突制御
    //==============================
    [Header("Kill中のプレイヤー表示/衝突制御")]
    [Tooltip("Kill中にプレイヤーのメッシュを隠す（最も確実）")]
    [SerializeField] private bool hidePlayerRenderersDuringKill = true;
    [Tooltip("メッシュを消す代わりに『影のみ』にする（ShadowsOnly）")]
    [SerializeField] private bool makePlayerShadowsOnly = false;
    [Tooltip("Kill中にプレイヤーと敵の衝突を無視（めり込み防止）")]
    [SerializeField] private bool ignorePlayerEnemyCollisionDuringKill = true;

    [Header("Kill直前SE（Face到達後〜Die前）")]
    [SerializeField] private AudioClip preDeathClip;
    [SerializeField] private float preDeathVolume = 1f;

    [Header("ワープポイント")]
    [SerializeField] private Transform[] warpPoints;

    [SerializeField] private float loseSightTime = 4f;

    private float lostTimer = 0f;
    // 状態
    private bool chasing;
    private bool killing;

    // Audio フェード制御
    private Coroutine chaseFadeRoutine;

    // 停止時のバックアップ
    private bool agentPrevUpdatePos, agentPrevUpdateRot;
    private bool animPrevApplyRootMotion;
    private float animPrevSpeed;

    // ★ プレイヤー可視/衝突の復帰用バックアップ
    private readonly List<Renderer> _playerRenderers = new();
    private readonly List<bool> _prevRendererEnabled = new();
    private readonly List<ShadowCastingMode> _prevShadowModes = new();
    private Collider[] _playerCollidersCache;
    private Collider[] _enemyCollidersCache;
    private readonly List<(Collider a, Collider b)> _ignoredPairs = new();

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        agent.updateRotation = true;
        agent.speed = walkSpeed;

        EnsureAudioSources();
    }

    // AudioSource の用意/初期化
    private void EnsureAudioSources()
    {
        // 追跡ループ用
        if (!chaseLoopSource)
        {
            var go = new GameObject("ChaseLoop_Audio");
            go.transform.SetParent(transform, false);
            chaseLoopSource = go.AddComponent<AudioSource>();
        }
        chaseLoopSource.playOnAwake = false;
        chaseLoopSource.loop = true;
        chaseLoopSource.spatialBlend = 1f;
        chaseLoopSource.minDistance = audioMinDistance;
        chaseLoopSource.maxDistance = audioMaxDistance;
        chaseLoopSource.rolloffMode = AudioRolloffMode.Linear;
        chaseLoopSource.volume = 0f;

        // ワンショット用
        if (!oneShotSource)
        {
            var go = new GameObject("OneShot_Audio");
            go.transform.SetParent(transform, false);
            oneShotSource = go.AddComponent<AudioSource>();
        }
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 1f;
        oneShotSource.minDistance = audioMinDistance;
        oneShotSource.maxDistance = audioMaxDistance;
        oneShotSource.rolloffMode = AudioRolloffMode.Linear;
    }

    void Update()
    {
        if (killing || !player) return;

        float d = Vector3.Distance(transform.position, player.position);

        if (CanSeePlayer())
        {
            lostTimer = 0f;
            if (!chasing)
            {
                chasing = true;
                StartChaseLoop(); // 追跡SEフェードイン
            }

            agent.speed = (d <= runRadius) ? runSpeed : walkSpeed;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            // desiredVelocity を使い、ダンピング＋ヒステリシスで安定化
            float target = agent.desiredVelocity.magnitude;
            if (chasing) target = Mathf.Max(target, speedRunBiasWhileChasing);
            if (target < speedIdleThreshold && agent.remainingDistance > agent.stoppingDistance + 0.05f)
                target = 0f;

            speedParamCurrent = Mathf.MoveTowards(speedParamCurrent, target, speedDamp * Time.deltaTime);
            if (anim) anim.SetFloat("Speed", speedParamCurrent);
        }
        else
        {
            if (chasing)
            {
                lostTimer += Time.deltaTime;

                if (lostTimer >= loseSightTime)
                {
                    WarpToRandomPoint();
                }
            }
        }
    }

    /// <summary>Kill 開始（Trigger からこれだけ呼ぶ）</summary>
    public void BeginKill()
    {
        if (killing) return;
        killing = true;

        // 追跡停止
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 追跡ループ停止
        StopChaseLoop();

        // 攻撃アニメ（任意）
        if (anim) anim.SetTrigger("Attack");

        // Kill突入のワンショット（任意）
        if (killStingerClip && oneShotSource)
            oneShotSource.PlayOneShot(killStingerClip, 1f);

        // 顔前に壁が近すぎるなら、少し押し戻す
        if (backOffFromWallBeforeKill && cameraFacePoint)
            TryBackOffFromWall();

        // 参照取得
        var health = player ? player.GetComponent<PlayerHealth>() : null;
        Camera cam = null;
        if (health && health.playerCamera) cam = health.playerCamera;
        if (!cam && player) cam = player.GetComponentInChildren<Camera>();

        StartCoroutine(KillSequence(cam, health));
    }

    private IEnumerator KillSequence(Camera playerCam, PlayerHealth health)
    {
        // 顔に到達した瞬間に鳴らす
        if (preDeathClip && oneShotSource)
        {
            oneShotSource.clip = preDeathClip;
            oneShotSource.volume = preDeathVolume;
            oneShotSource.loop = false;
            oneShotSource.Play();
        }
        // プレイヤー入力/視点停止（戻り防止）
        MonoBehaviour movementScriptToDisable = null;
        if (health && health.playerMovementScript)
        {
            movementScriptToDisable = health.playerMovementScript;
            movementScriptToDisable.enabled = false;
        }

        // ★ Kill中：プレイヤーを不可視＋衝突無効（見え込み/めり込み防止）
        if (hidePlayerRenderersDuringKill) TogglePlayerRenderers(hide: true, shadowsOnly: makePlayerShadowsOnly);
        if (ignorePlayerEnemyCollisionDuringKill) TogglePlayerEnemyCollision(ignore: true);

        // 敵完全停止（RootMotion含め）
        FreezeEnemy(true);

        // カメラ一時デタッチ（戻り防止）
        Transform originalParent = null;
        Vector3 originalLocalPos = Vector3.zero;
        Quaternion originalLocalRot = Quaternion.identity;
        if (playerCam && detachCameraDuringKill)
        {
            originalParent = playerCam.transform.parent;
            originalLocalPos = playerCam.transform.localPosition;
            originalLocalRot = playerCam.transform.localRotation;
            playerCam.transform.SetParent(null, true);
        }

        FacePlayerHard();

        // 顔前に寄せる（安全位置を解決してから移動）
        if (playerCam && cameraFacePoint)
        {
            // 視線が壁で遮られているなら少し待つ（任意）
            if (requireLineOfSightForKill)
                yield return WaitForLineOfSightOrTimeout(playerCam, losWaitTimeout);

            if (cameraMoveDuration <= 0f)
            {
                Vector3 safePos; Quaternion safeRot;
                ResolveSafeCameraPose(cameraFacePoint, playerCam.transform, out safePos, out safeRot);
                playerCam.transform.SetPositionAndRotation(safePos, safeRot);
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < cameraMoveDuration)
                {
                    elapsed += Time.deltaTime;

                    Vector3 safePos; Quaternion safeRot;
                    ResolveSafeCameraPose(cameraFacePoint, playerCam.transform, out safePos, out safeRot);

                    float k = 1f - Mathf.Exp(-cameraLerp * Time.deltaTime);
                    playerCam.transform.position = Vector3.Lerp(playerCam.transform.position, safePos, k);
                    playerCam.transform.rotation = Quaternion.Slerp(playerCam.transform.rotation, safeRot, k);

                    yield return null;
                }
                Vector3 finalPos; Quaternion finalRot;
                ResolveSafeCameraPose(cameraFacePoint, playerCam.transform, out finalPos, out finalRot);
                playerCam.transform.SetPositionAndRotation(finalPos, finalRot);
            }
           
            // 見せ時間（シェイク＋安全位置へ寄せ直し）
            if (cameraHoldBeforeDie > 0f)
            {
                float hold = cameraHoldBeforeDie;
                float elapsedHold = 0f;

                while (elapsedHold < hold)
                {
                    elapsedHold += Time.deltaTime;

                    Vector3 basePos; Quaternion baseRot;
                    ResolveSafeCameraPose(cameraFacePoint, playerCam.transform, out basePos, out baseRot);

                    if (cameraShakeOnHold)
                    {
                        float t01 = Mathf.Clamp01(elapsedHold / hold);
                        ApplyCameraShake(playerCam.transform, cameraFacePoint, t01,
                                         shakeFrequency, shakePosAmplitude, shakeRotAmplitude);

                        // めり込み抑制：シェイク後に安全姿勢へ軽く寄せる
                        float k = 1f - Mathf.Exp(-cameraLerp * Time.deltaTime);
                        playerCam.transform.position = Vector3.Lerp(playerCam.transform.position, basePos, k);
                        playerCam.transform.rotation = Quaternion.Slerp(playerCam.transform.rotation, baseRot, k);
                    }
                    else
                    {
                        playerCam.transform.SetPositionAndRotation(basePos, baseRot);
                    }

                    yield return null;
                }

                Vector3 endPos; Quaternion endRot;
                ResolveSafeCameraPose(cameraFacePoint, playerCam.transform, out endPos, out endRot);
                playerCam.transform.SetPositionAndRotation(endPos, endRot);
            }
        }
        //Die直前にSE停止
        if(oneShotSource && oneShotSource.isPlaying)
        {
            oneShotSource.Stop();
        }
        // 死亡
        if (health != null && !health.isDead)
            health.Die();

        // カメラを戻す（シーンリロードしない場合の保険）
        if (playerCam && detachCameraDuringKill && originalParent != null)
        {
            playerCam.transform.SetParent(originalParent, true);
            playerCam.transform.localPosition = originalLocalPos;
            playerCam.transform.localRotation = originalLocalRot;
        }

        // ★ 復帰（リロードしない構成でも安全）
        if (ignorePlayerEnemyCollisionDuringKill) TogglePlayerEnemyCollision(ignore: false);
        if (hidePlayerRenderersDuringKill) TogglePlayerRenderers(hide: false, shadowsOnly: makePlayerShadowsOnly);
    }

    //==============================
    // 追跡ループ制御
    //==============================
    private void StartChaseLoop()
    {
        if (!chaseLoopClip || !chaseLoopSource) return;

        if (chaseFadeRoutine != null) StopCoroutine(chaseFadeRoutine);

        if (chaseLoopSource.clip != chaseLoopClip)
            chaseLoopSource.clip = chaseLoopClip;

        if (!chaseLoopSource.isPlaying)
        {
            chaseLoopSource.volume = 0f;
            chaseLoopSource.Play();
        }
        chaseFadeRoutine = StartCoroutine(FadeAudio(chaseLoopSource, 1f, chaseFadeIn));
    }

    private void StopChaseLoop()
    {
        if (!chaseLoopSource) return;
        if (chaseFadeRoutine != null) StopCoroutine(chaseFadeRoutine);
        chaseFadeRoutine = StartCoroutine(FadeOutAndStop(chaseLoopSource, chaseFadeOut));
    }

    private IEnumerator FadeAudio(AudioSource src, float to, float duration)
    {
        if (!src) yield break;
        float t = 0f;
        float from = src.volume;
        duration = Mathf.Max(0.001f, duration);
        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        src.volume = to;
    }

    private IEnumerator FadeOutAndStop(AudioSource src, float duration)
    {
        yield return FadeAudio(src, 0f, duration);
        if (src) src.Stop();
    }

    //==============================
    // 敵を完全停止/解除（RootMotionも止める）
    //==============================
    private void FreezeEnemy(bool on)
    {
        if (!agent || !anim) return;

        if (on)
        {
            agentPrevUpdatePos = agent.updatePosition;
            agentPrevUpdateRot = agent.updateRotation;
            animPrevApplyRootMotion = anim.applyRootMotion;
            animPrevSpeed = anim.speed;

            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.velocity = Vector3.zero;

            anim.applyRootMotion = false;
            anim.SetFloat("Speed", 0f);
            anim.speed = 0f;
        }
        else
        {
            agent.updatePosition = agentPrevUpdatePos;
            agent.updateRotation = agentPrevUpdateRot;
            agent.isStopped = false;

            anim.applyRootMotion = animPrevApplyRootMotion;
            anim.speed = animPrevSpeed;
        }
    }

    //==============================
    // 見栄え用の回頭
    //==============================
    private void FacePlayerHard()
    {
        if (!player) return;
        Vector3 to = player.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.001f)
        {
            var rot = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 12f * Time.deltaTime);
        }
    }

    //==============================
    // カメラシェイク（見せ時間中のみ）
    //==============================
    private void ApplyCameraShake(Transform camTr, Transform facePoint, float t01, float freq,
                                  Vector3 posAmp, Vector3 rotAmpDeg)
    {
        float envelope = (shakeDecay <= 0f) ? 1f : Mathf.Lerp(1f, 0f, Mathf.Clamp01(t01) * shakeDecay);
        float time = Time.time * freq;

        float nx = Mathf.PerlinNoise(time, 0.37f) * 2f - 1f;
        float ny = Mathf.PerlinNoise(0.73f, time) * 2f - 1f;
        float nz = Mathf.PerlinNoise(time * 0.5f, time * 0.9f) * 2f - 1f;

        Vector3 localOffset = new Vector3(nx * posAmp.x, ny * posAmp.y, nz * posAmp.z) * envelope;
        Vector3 worldOffset =
              facePoint.right * localOffset.x
            + facePoint.up * localOffset.y
            + facePoint.forward * localOffset.z;

        Vector3 euler = new Vector3(ny * rotAmpDeg.x, nx * rotAmpDeg.y, nz * rotAmpDeg.z) * envelope;
        Quaternion rotShake = Quaternion.Euler(euler);

        camTr.position = facePoint.position + worldOffset;
        camTr.rotation = facePoint.rotation * rotShake;
    }

    //==============================
    // カメラの安全姿勢を解決（壁めり込み防止）
    //==============================
    private void ResolveSafeCameraPose(Transform facePoint, Transform camTr, out Vector3 safePos, out Quaternion safeRot)
    {
        Vector3 desiredPos = facePoint.position;
        Quaternion desiredRot = facePoint.rotation;

        // FacePoint前方に壁が近い場合、法線方向にオフセット
        if (Physics.SphereCast(desiredPos - facePoint.forward * 0.01f, cameraProbeRadius, facePoint.forward,
                               out var hitFwd, cameraMaxProbeDistance, cameraCollisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredPos = hitFwd.point - hitFwd.normal * cameraSurfaceClearance;
        }

        // その位置がなお壁内部なら、FacePoint→desired方向に再度補正
        if (Physics.CheckSphere(desiredPos, cameraProbeRadius, cameraCollisionMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 dir = (desiredPos - facePoint.position).normalized;
            if (dir.sqrMagnitude < 1e-4f) dir = -facePoint.forward;
            if (Physics.SphereCast(facePoint.position, cameraProbeRadius, dir,
                                   out var hit, cameraMaxProbeDistance, cameraCollisionMask, QueryTriggerInteraction.Ignore))
            {
                desiredPos = hit.point - hit.normal * cameraSurfaceClearance;
            }
        }

        safePos = desiredPos;
        safeRot = desiredRot;
    }

    //==============================
    // Kill前に少し押し戻し（狭所での顔ドアップ崩れ対策）
    //==============================
    private void TryBackOffFromWall()
    {
        if (!cameraFacePoint) return;

        if (Physics.SphereCast(cameraFacePoint.position, 0.06f, cameraFacePoint.forward,
                               out var hit, backOffCheckDistance, wallMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 push = hit.normal * backOffDistance;
            Vector3 dest = transform.position + push;

            if (NavMesh.SamplePosition(dest, out var nmHit, 1.0f, NavMesh.AllAreas))
                agent.Warp(nmHit.position);
            else
                agent.Warp(dest);
        }
    }

    //==============================
    // 視線クリア待機（任意）
    //==============================
    private IEnumerator WaitForLineOfSightOrTimeout(Camera playerCam, float timeout)
    {
        if (!cameraFacePoint || !playerCam) yield break;

        float t = 0f;
        while (t < timeout)
        {
            t += Time.deltaTime;
            Vector3 from = cameraFacePoint.position;
            Vector3 to = playerCam.transform.position;

            if (!Physics.Linecast(from, to, cameraCollisionMask, QueryTriggerInteraction.Ignore))
                yield break; // 視線が通った

            yield return null;
        }
    }

    //==============================
    // ★ Kill中：プレイヤーのRendererを隠す/戻す
    //==============================
    private void TogglePlayerRenderers(bool hide, bool shadowsOnly)
    {
        if (!player) return;

        if (hide)
        {
            _playerRenderers.Clear();
            _prevRendererEnabled.Clear();
            _prevShadowModes.Clear();

            var rends = player.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                // カメラ/ライト/エフェクト以外の描画を対象に（基本はRenderer全部でOK）
                _playerRenderers.Add(r);
                _prevRendererEnabled.Add(r.enabled);
                _prevShadowModes.Add(r.shadowCastingMode);

                if (shadowsOnly)
                {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly; // 影だけ残す
                }
                else
                {
                    r.enabled = false; // 完全に不可視
                }
            }
        }
        else
        {
            // 復帰
            for (int i = 0; i < _playerRenderers.Count; i++)
            {
                var r = _playerRenderers[i];
                if (!r) continue;

                if (makePlayerShadowsOnly)
                {
                    // 元のモードを戻す
                    if (i < _prevShadowModes.Count)
                        r.shadowCastingMode = _prevShadowModes[i];
                }
                else
                {
                    if (i < _prevRendererEnabled.Count)
                        r.enabled = _prevRendererEnabled[i];
                    else
                        r.enabled = true;
                }
            }
            _playerRenderers.Clear();
            _prevRendererEnabled.Clear();
            _prevShadowModes.Clear();
        }
    }

    //==============================
    // ★ Kill中：プレイヤーと敵の衝突を無視/戻す
    //==============================
    private void TogglePlayerEnemyCollision(bool ignore)
    {
        if (!player) return;

        if (ignore)
        {
            _ignoredPairs.Clear();
            _playerCollidersCache ??= player.GetComponentsInChildren<Collider>(true);
            _enemyCollidersCache ??= GetComponentsInChildren<Collider>(true);

            foreach (var pc in _playerCollidersCache)
            {
                if (!pc || !pc.enabled) continue;
                foreach (var ec in _enemyCollidersCache)
                {
                    if (!ec || !ec.enabled) continue;
                    Physics.IgnoreCollision(pc, ec, true);
                    _ignoredPairs.Add((pc, ec));
                }
            }
        }
        else
        {
            // 復帰（リロードしない構成でも安全）
            foreach (var pair in _ignoredPairs)
            {
                if (pair.a && pair.b)
                    Physics.IgnoreCollision(pair.a, pair.b, false);
            }
            _ignoredPairs.Clear();
        }
    }
    bool CanSeePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > detectRadius) return false;

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > 60f) return false;

        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up, dir, out hit, dist))
        {
            if (hit.transform != player)
                return false; // 壁に当たった
        }

        return true;
    }
    void WarpToRandomPoint()
    {
        if (warpPoints.Length == 0) return;

        int r = Random.Range(0, warpPoints.Length);

        agent.Warp(warpPoints[r].position);

        // 追跡状態リセット
        chasing = false;
        lostTimer = 0f;

        // NavMeshAgent停止
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        // AnimatorをIdleへ
        speedParamCurrent = 0f;
        if (anim) anim.SetFloat("Speed", 0f);

        StopChaseLoop();
    }
    void OnDrawGizmosSelected()
    {
        if (!player) return;

        Gizmos.color = Color.yellow;

        // 検知半径
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        // 視界角度
        Vector3 left = Quaternion.Euler(0, -60, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, 60, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, left * detectRadius);
        Gizmos.DrawRay(transform.position, right * detectRadius);

        // プレイヤー方向
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, player.position);
    }
}