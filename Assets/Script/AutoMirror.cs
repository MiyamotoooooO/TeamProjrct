using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class RealMirror : MonoBehaviour
{
    [Header("Basic Settings")]
    public int textureSize = 512;
    public float renderDistance = 25f;
    public float clipPlaneOffset = 0.05f;
    public LayerMask mirrorLayer;

    [Header("Optimization")]
    public bool disablePixelLights = true;

    private Camera reflectionCamera;
    private RenderTexture mirrorTexture;
    private static bool isRenderingMirror = false;

    void OnWillRenderObject()
    {
        if (isRenderingMirror) return;
        isRenderingMirror = true;

        var renderer = GetComponent<Renderer>();
        if (!enabled || !renderer || !renderer.sharedMaterial || !renderer.isVisible)
        {
            isRenderingMirror = false;
            return;
        }

        Camera cam = Camera.current;
        if (!cam)
        {
            isRenderingMirror = false;
            return;
        }

        // 1. Setup Camera & Texture
        CreateMirrorObjects(cam);

        // 2. Calculate Reflection Matrix
        Vector3 pos = transform.position;
        Vector3 normal = transform.forward;

        float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
        Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflectionMatrix = Matrix4x4.zero;
        CalculateReflectionMatrix(ref reflectionMatrix, reflectionPlane);

        // 3. Apply Transformation
        reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflectionMatrix;

        // 4. Setup Oblique Projection Plane (Crucial for performance and artifacts)
        Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
        reflectionCamera.projectionMatrix = cam.CalculateObliqueMatrix(clipPlane);

        // 5. Render
        GL.invertCulling = true;
        reflectionCamera.cullingMask = ~(mirrorLayer) & cam.cullingMask;
        reflectionCamera.Render();
        GL.invertCulling = false;

        renderer.sharedMaterial.SetTexture("_MainTex", mirrorTexture);
        renderer.sharedMaterial.SetTexture("_BaseMap", mirrorTexture);

        isRenderingMirror = false;
    }

    private void CreateMirrorObjects(Camera currentCamera)
    {
        // 鏡自体の縦横比を計算
        // ※鏡がPlaneの場合はlocalScale.xとzを見る必要がありますが、Cubeならxとy、またはxとzです。
        // ここでは一般的な「垂直に立ったQuadやCube」を想定して X(幅) と Y(高さ) を見ます。
        float aspectRatio = transform.lossyScale.x / transform.lossyScale.y;

        // 基本サイズを縦幅として、横幅を比率に合わせて計算
        int height = textureSize;
        int width = Mathf.RoundToInt(textureSize * aspectRatio);

        // テクスチャが存在しない、またはサイズが変わった場合に作り直す
        if (!mirrorTexture || mirrorTexture.width != width || mirrorTexture.height != height)
        {
            if (mirrorTexture) DestroyImmediate(mirrorTexture);

            // 計算した width と height で作成
            mirrorTexture = new RenderTexture(width, height, 16);
            mirrorTexture.name = "__MirrorReflection" + GetHashCode();
            mirrorTexture.isPowerOfTwo = true;
            mirrorTexture.hideFlags = HideFlags.DontSave;
        }

        if (!reflectionCamera)
        {
            GameObject go = new GameObject("Mirror Reflection Camera id" + GetHashCode(), typeof(Camera), typeof(Skybox));
            reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.enabled = false;
            reflectionCamera.transform.position = transform.position;
            reflectionCamera.transform.rotation = transform.rotation;
            go.hideFlags = HideFlags.HideAndDontSave;
        }

        reflectionCamera.CopyFrom(currentCamera);
        reflectionCamera.targetTexture = mirrorTexture;
        if (disablePixelLights) QualitySettings.pixelLightCount = 0;
    }

    private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMatrix, Vector4 plane)
    {
        reflectionMatrix.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMatrix.m01 = (-2F * plane[0] * plane[1]);
        reflectionMatrix.m02 = (-2F * plane[0] * plane[2]);
        reflectionMatrix.m03 = (-2F * plane[3] * plane[0]);

        reflectionMatrix.m10 = (-2F * plane[1] * plane[0]);
        reflectionMatrix.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMatrix.m12 = (-2F * plane[1] * plane[2]);
        reflectionMatrix.m13 = (-2F * plane[3] * plane[1]);

        reflectionMatrix.m20 = (-2F * plane[2] * plane[0]);
        reflectionMatrix.m21 = (-2F * plane[2] * plane[1]);
        reflectionMatrix.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMatrix.m23 = (-2F * plane[3] * plane[2]);

        reflectionMatrix.m30 = 0F;
        reflectionMatrix.m31 = 0F;
        reflectionMatrix.m32 = 0F;
        reflectionMatrix.m33 = 1F;
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    void OnDisable()
    {
        if (mirrorTexture)
        {
            DestroyImmediate(mirrorTexture);
            mirrorTexture = null;
        }
        if (reflectionCamera)
        {
            DestroyImmediate(reflectionCamera.gameObject);
            reflectionCamera = null;
        }
    }
}


/*using UnityEngine;

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
}*/