using UnityEngine;
using TMPro;

public class HiddenTextReveal : MonoBehaviour
{
    [Header("--- 設定 ---")]
    [Tooltip("最初から表示しておきたい文字の番号（0から始まります）")]
    public int[] visibleCharIndexes;
    // 例：1文字目と3文字目を見せたいなら、ここに 0 と 2 を入力します

    [Tooltip("炙った時に文字が浮かび上がる速さ")]
    public float revealSpeed = 0.5f;

    // --- 内部変数 ---
    private TMP_Text myText;
    private bool[] isInitiallyVisible; // その文字が最初から見えているかフラグ
    private float[] randomOffsets;     // 炙り出しのタイミングを少しずらす用
    private float heatLevel = 0f;      // 現在の熱量
    private bool isInitialized = false;

    void Start()
    {
        myText = GetComponent<TMP_Text>();

        if (myText != null)
        {
            // 文字色を強制的に不透明(Alpha=1)にする
            Color baseColor = myText.color;
            baseColor.a = 1.0f;
            myText.color = baseColor;

            myText.ForceMeshUpdate();

            InitializeCharacters();
            UpdateTextColors();
        }
    }

    // 文字ごとの初期設定
    void InitializeCharacters()
    {
        int charCount = myText.textInfo.characterCount;
        isInitiallyVisible = new bool[charCount];
        randomOffsets = new float[charCount];

        for (int i = 0; i < charCount; i++)
        {
            if (!myText.textInfo.characterInfo[i].isVisible) continue;

            // ランダムではなく、指定されたリストに番号があるかチェック
            bool isVisible = false;
            for (int k = 0; k < visibleCharIndexes.Length; k++)
            {
                if (visibleCharIndexes[k] == i)
                {
                    isVisible = true;
                    break;
                }
            }

            if (isVisible)
            {
                isInitiallyVisible[i] = true; // 指定された文字なので最初から見せる
            }
            else
            {
                isInitiallyVisible[i] = false; // 指定されていないので隠す

                // 炙り出しの演出用（出現タイミングを少しバラけさせる）
                randomOffsets[i] = Random.Range(0.0f, 0.5f);
            }
        }
        isInitialized = true;
    }

    // ライターから毎フレーム呼ばれる関数
    public void ReceiveHeat()
    {
        if (!isInitialized) return;

        heatLevel += Time.deltaTime * revealSpeed;
        UpdateTextColors();
    }

    void UpdateTextColors()
    {
        myText.ForceMeshUpdate();
        TMP_TextInfo textInfo = myText.textInfo;
        int charCount = textInfo.characterCount;

        for (int i = 0; i < charCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Color32[] newVertexColors = textInfo.meshInfo[materialIndex].colors32;

            float alpha = 0f;

            if (isInitiallyVisible[i])
            {
                // 最初から見える設定の文字は 100% 表示
                alpha = 1.0f;
            }
            else
            {
                // それ以外は熱量に応じて表示（隠れている状態からスタート）
                float calculatedAlpha = (heatLevel - randomOffsets[i]) * 2.0f;
                alpha = Mathf.Clamp01(calculatedAlpha);
            }

            // 色を適用
            Color32 c = myText.color;
            c.a = (byte)(alpha * 255);

            newVertexColors[vertexIndex + 0] = c;
            newVertexColors[vertexIndex + 1] = c;
            newVertexColors[vertexIndex + 2] = c;
            newVertexColors[vertexIndex + 3] = c;
        }

        myText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}



/*using UnityEngine;
using TMPro; // TextMeshPro必須

public class HiddenTextReveal : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("最初に表示されている文字の割合（0.3なら30%だけ最初から見える）")]
    [Range(0f, 1f)]
    public float initialVisiblePercent = 0.3f;

    [Tooltip("炙った時に文字が浮かび上がる速さ")]
    public float revealSpeed = 0.5f;

    // 内部変数
    private TMP_Text myText;
    private float[] charAlphas; // 各文字の現在の透明度
    private bool[] isInitiallyVisible; // 最初から見えているかどうかのフラグ
    private float heatLevel = 0f; // 現在の熱量（0〜1）

    void Start()
    {
        myText = GetComponent<TMP_Text>();

        // メッシュ（文字の形状データ）を強制更新して準備する
        myText.ForceMeshUpdate();

        int charCount = myText.textInfo.characterCount;
        charAlphas = new float[charCount];
        isInitiallyVisible = new bool[charCount];

        // 1文字ずつ「最初から見せるか？」を抽選する
        for (int i = 0; i < charCount; i++)
        {
            // 空白文字などは無視
            if (!myText.textInfo.characterInfo[i].isVisible) continue;

            // 確率で「最初から見える」ことにする
            if (Random.value <= initialVisiblePercent)
            {
                isInitiallyVisible[i] = true;
                charAlphas[i] = 1.0f; // 完全に見える
            }
            else
            {
                isInitiallyVisible[i] = false;
                charAlphas[i] = 0.0f; // 隠す（透明）
            }
        }

        // 最初の見た目を反映
        UpdateTextColors();
    }

    // ライターから呼ばれる関数
    public void ReceiveHeat()
    {
        // 熱レベルを上げる
        heatLevel += Time.deltaTime * revealSpeed;
        if (heatLevel > 1.0f) heatLevel = 1.0f;

        // 色を更新する
        UpdateTextColors();
    }

    void UpdateTextColors()
    {
        // TextMeshProの文字色データ等を取得
        myText.ForceMeshUpdate();
        TMP_TextInfo textInfo = myText.textInfo;
        Color32[] newVertexColors;

        int charCount = textInfo.characterCount;

        for (int i = 0; i < charCount; i++)
        {
            // 見えない文字（スペースなど）はスキップ
            if (!textInfo.characterInfo[i].isVisible) continue;

            // マテリアルのインデックスを取得
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            newVertexColors = textInfo.meshInfo[materialIndex].colors32;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;

            // --- 透明度の計算 ---
            float alpha = charAlphas[i];

            // 最初から見えていない文字なら、熱量に応じて浮かび上がらせる
            if (!isInitiallyVisible[i])
            {
                // heatLevel(0~1) に応じてアルファを増やす
                // ランダムさを少し加えて、バラバラと出てくるようにする
                float noise = Mathf.PerlinNoise(i * 0.5f, heatLevel * 5f);
                float targetAlpha = Mathf.Clamp01(heatLevel + (noise * 0.2f));

                // 現在の値より大きければ更新
                if (targetAlpha > alpha) alpha = targetAlpha;
            }

            // 現在の値を保存
            charAlphas[i] = alpha;

            // 色を適用（元の文字色を保ちつつ、Alphaだけ変える）
            byte alphaByte = (byte)(alpha * 255);
            Color32 c = myText.color; // ベースの色
            c.a = alphaByte;

            // 1文字は4つの頂点（四角形）でできているので、4つとも色を変える
            newVertexColors[vertexIndex + 0] = c;
            newVertexColors[vertexIndex + 1] = c;
            newVertexColors[vertexIndex + 2] = c;
            newVertexColors[vertexIndex + 3] = c;
        }

        // 変更をメッシュに反映
        myText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}*/