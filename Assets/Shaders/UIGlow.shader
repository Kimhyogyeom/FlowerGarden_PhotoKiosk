Shader "UI/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Glow Settings)]
        _Brightness ("Brightness", Range(1, 5)) = 2
        _GlowPower ("Glow Power", Range(0, 1)) = 0.5

        [Header(Pulse Animation)]
        [Toggle] _EnablePulse ("Enable Pulse", Float) = 0
        _PulseSpeed ("Pulse Speed", Range(0.5, 5)) = 2
        _PulseMin ("Pulse Min Brightness", Range(0.5, 1)) = 0.7

        // UI Stencil
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One One  // Additive 블렌딩 - 빛이 합쳐져서 밝아짐
        ColorMask [_ColorMask]

        Pass
        {
            Name "GLOW_ADDITIVE"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Brightness;
            float _GlowPower;
            float _EnablePulse;
            float _PulseSpeed;
            float _PulseMin;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // Pulse 애니메이션
                float pulse = 1.0;
                if(_EnablePulse > 0.5)
                {
                    pulse = lerp(_PulseMin, 1.0, (sin(_Time.y * _PulseSpeed * 6.28318) + 1.0) * 0.5);
                }

                // 밝기 증폭 + Glow 효과
                float brightness = _Brightness * pulse;

                // 중심부가 더 밝게 (가장자리로 갈수록 약간 어둡게)
                float2 center = i.texcoord - 0.5;
                float dist = 1.0 - saturate(length(center) * _GlowPower);

                col.rgb *= brightness * (1.0 + dist * 0.5);

                // UI Clipping
                col *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

                return col;
            }
            ENDCG
        }
    }

    // Additive가 안 맞으면 일반 블렌딩 버전
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            sampler2D _MainTex;
            float _Brightness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                col.rgb *= _Brightness;
                return col;
            }
            ENDCG
        }
    }
}
