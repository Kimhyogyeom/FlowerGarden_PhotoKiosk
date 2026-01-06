Shader "Hidden/WebcamSharpen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SharpenStrength ("Sharpen Strength", Range(0, 2)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _SharpenStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;

                // 중심 픽셀
                fixed4 center = tex2D(_MainTex, i.uv);

                // 주변 4픽셀 샘플링
                fixed4 left = tex2D(_MainTex, i.uv + float2(-texelSize.x, 0));
                fixed4 right = tex2D(_MainTex, i.uv + float2(texelSize.x, 0));
                fixed4 up = tex2D(_MainTex, i.uv + float2(0, texelSize.y));
                fixed4 down = tex2D(_MainTex, i.uv + float2(0, -texelSize.y));

                // Unsharp Mask: center + strength * (center - blur)
                fixed4 blur = (left + right + up + down) * 0.25;
                fixed4 sharpened = center + _SharpenStrength * (center - blur);

                return saturate(sharpened);
            }
            ENDCG
        }
    }
}
