using UnityEngine;

public class AutoMirror : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("プレイヤーのメインカメラ（空欄なら自動で探します）")]
    public Transform mainCamera;

    [Tooltip("鏡の解像度（高いほど綺麗だが重い。256か512推奨）")]
    public int textureSize = 512;

    [Tooltip("鏡が機能する距離（これ以上離れると映らなくなる）")]
    public float renderDistance = 10.0f;

    [Tooltip("鏡自体のレイヤー（鏡が自分自身を映さないように設定）")]
    public LayerMask mirrorLayer;

    // 内部変数
    private Camera reflectionCamera;
    private RenderTexture mirrorTexture;
    private Renderer mirrorRenderer;
    private Material mirrorMaterial;

    void Start()
    {
        // 1. プレイヤーのカメラを自動で見つける
        if (mainCamera == null)
        {
            if (Camera.main != null) mainCamera = Camera.main.transform;
            else Debug.LogError("MainCameraが見つかりません！PlayerのカメラにタグMainCameraをつけてください。");
        }

        // 2. 鏡のRendererを取得
        mirrorRenderer = GetComponent<Renderer>();

        // 3. 自分専用のマテリアルを複製（他の鏡と映像が被らないようにするため）
        mirrorMaterial = new Material(mirrorRenderer.sharedMaterial);
        mirrorRenderer.material = mirrorMaterial;

        // 4. 自分専用のRenderTextureを生成
        mirrorTexture = new RenderTexture(textureSize, textureSize, 16);
        mirrorTexture.name = "MirrorTex_" + gameObject.name;

        // 5. マテリアルに生成したテクスチャをセット
        // Shaderによってプロパティ名が違うため、代表的なものを指定
        mirrorMaterial.SetTexture("_MainTex", mirrorTexture);
        mirrorMaterial.SetTexture("_BaseMap", mirrorTexture);

        // 6. 撮影用カメラを子オブジェクトとして作成
        GameObject camObj = new GameObject("MirrorCam_" + gameObject.name);
        camObj.transform.SetParent(transform);
        reflectionCamera = camObj.AddComponent<Camera>();
        reflectionCamera.targetTexture = mirrorTexture;
        reflectionCamera.enabled = false; // Update内で制御するため最初はOFF

        // 余計な設定を最適化
        reflectionCamera.clearFlags = CameraClearFlags.Skybox;
        reflectionCamera.fieldOfView = 60;

        // 鏡自体が映り込むのを防ぐ（指定したレイヤーを無視）
        // ※「Nothing」だと全て映すので、mirrorLayerが設定されている時だけ適用
        if (mirrorLayer.value != 0)
        {
            reflectionCamera.cullingMask = ~(mirrorLayer);
        }
    }

    void Update()
    {
        if (mainCamera == null || reflectionCamera == null) return;

        // プレイヤーが遠すぎるなら処理しない（軽量化）
        float dist = Vector3.Distance(transform.position, mainCamera.position);
        if (dist > renderDistance)
        {
            // 遠いので描画しない
            return;
        }

        // カメラの位置合わせ
        UpdateCameraPosition();

        // 1フレームだけ手動で撮影（これが一番軽い）
        reflectionCamera.Render();
    }

    void UpdateCameraPosition()
    {
        Vector3 directionToPlayer = mainCamera.position - transform.position;
        Vector3 reflectionDir = Vector3.Reflect(directionToPlayer, transform.forward);

        reflectionCamera.transform.position = transform.position + reflectionDir;

        Vector3 viewDirection = Vector3.Reflect(mainCamera.forward, transform.forward);
        reflectionCamera.transform.rotation = Quaternion.LookRotation(viewDirection);

        reflectionCamera.nearClipPlane = 0.1f;
    }


    // ゲーム終了時や破壊時にメモリをお掃除
    void OnDestroy()
    {
        if (mirrorTexture != null)
        {
            mirrorTexture.Release();
            Destroy(mirrorTexture);
        }
        if (mirrorMaterial != null)
        {
            Destroy(mirrorMaterial);
        }
    }
}