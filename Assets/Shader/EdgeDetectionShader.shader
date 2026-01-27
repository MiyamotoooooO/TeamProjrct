Shader "Custom/EdgeDetectionShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Thickness ("Line Thickness", Range(0.0001, 0.005)) = 0.001
        _Sensitivity ("Sensitivity", Range(0, 100)) = 10
        _BackgroundColor ("Background Color", Color) = (0,0,0,1) // 黒背景
        _EdgeColor ("Edge Color", Color) = (1,1,1,1) // 白い線
    }
    SubShader
    {
        // ポストプロセス用の設定
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _Thickness;
            float _Sensitivity;
            fixed4 _BackgroundColor;
            fixed4 _EdgeColor;

            // 明度（明るさ）を計算する関数
            float luminance(fixed4 color)
            {
                return dot(color.rgb, float3(0.299, 0.587, 0.114));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 周囲のピクセルの明るさを取得（ソーベルフィルタ的な処理）
                float2 offsetX = float2(_Thickness, 0);
                float2 offsetY = float2(0, _Thickness);

                float left = luminance(tex2D(_MainTex, i.uv - offsetX));
                float right = luminance(tex2D(_MainTex, i.uv + offsetX));
                float down = luminance(tex2D(_MainTex, i.uv - offsetY));
                float up = luminance(tex2D(_MainTex, i.uv + offsetY));

                // 上下左右の明るさの差を計算
                float edgeX = right - left;
                float edgeY = up - down;

                // 差が大きいほど「エッジ」とみなす
                float edge = sqrt(edgeX * edgeX + edgeY * edgeY);
                
                // 感度を適用
                edge *= _Sensitivity;

                // 背景色と線の色を合成（線がないところは背景色、あるところは線色）
                return lerp(_BackgroundColor, _EdgeColor, saturate(edge));
            }
            ENDCG
        }
    }
}