Shader "Custom/SimpleOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,1,1,1) // ˜g‚ÌF
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02 // ˜g‚Ì‘¾‚³
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            // — –Ê‚ğ•`‰æ‚·‚é‚±‚Æ‚ÅA–c‚ç‚Ü‚¹‚½ƒ‚ƒfƒ‹‚Ì˜g‚ğŒ©‚¹‚é
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                // ’¸“_‚ğ–@ü•ûŒüiŠO‘¤j‚É­‚µ–c‚ç‚Ü‚¹‚é
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}