Shader "Custom/RVMComposite"
{
    Properties
    {
        _MainTex ("Webcam Texture", 2D) = "white" {}
        _MaskTex ("Alpha Matte (RVM)", 2D) = "white" {}
        _BackgroundTex ("Background Image", 2D) = "white" {}
        _MirrorHorizontal ("Mirror Horizontal", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _BackgroundTex;
            float _MirrorHorizontal;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // 좌우반전 적용
                if (_MirrorHorizontal > 0.5)
                {
                    uv.x = 1.0 - uv.x;
                }

                // 웹캠 이미지 (사람)
                fixed4 person = tex2D(_MainTex, uv);

                // RVM 알파 매트 (이미 부드러운 0~1 값)
                float alpha = tex2D(_MaskTex, uv).r;

                // 배경 이미지
                fixed4 background = tex2D(_BackgroundTex, uv);

                // 간단한 알파 블렌딩 (RVM 출력은 이미 고품질)
                return lerp(background, person, alpha);
            }
            ENDCG
        }
    }
}
