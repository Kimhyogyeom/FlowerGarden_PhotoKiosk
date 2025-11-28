Shader "Custom/PersonBackgroundComposite"
{
    Properties
    {
        _MainTex ("Webcam Texture", 2D) = "white" {}
        _MaskTex ("Segmentation Mask", 2D) = "white" {}
        _BackgroundTex ("Background Image", 2D) = "white" {}
        _Threshold ("Mask Threshold", Range(0, 1)) = 0.6
        _Smoothness ("Edge Smoothness", Range(0, 0.2)) = 0.05
        _Dilate ("Dilate (Expand Person)", Range(0, 0.2)) = 0.05
        _FillHoles ("Fill Small Holes", Range(0, 1)) = 0.8
        _MirrorHorizontal ("Mirror Horizontal", Float) = 1  // ← 추가!
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
            float4 _MaskTex_TexelSize;
            
            float _Threshold;
            float _Smoothness;
            float _Dilate;
            float _FillHoles;
            float _MirrorHorizontal;  // ← 추가!
            
            // 개선된 마스크 샘플링 (구멍 메우기 + 확장)
            float GetImprovedMask(float2 uv)
            {
                float mask = tex2D(_MaskTex, uv).r;
                
                // === Dilate (팽창) - 옷 잘림 방지 + 구멍 메우기 ===
                if (_Dilate > 0.001)
                {
                    float2 offset = _MaskTex_TexelSize.xy * (_Dilate * 50.0);
                    
                    // 8방향 샘플링 (더 정밀)
                    float m1 = tex2D(_MaskTex, uv + float2(-offset.x, -offset.y)).r;
                    float m2 = tex2D(_MaskTex, uv + float2(0, -offset.y)).r;
                    float m3 = tex2D(_MaskTex, uv + float2(offset.x, -offset.y)).r;
                    float m4 = tex2D(_MaskTex, uv + float2(-offset.x, 0)).r;
                    float m5 = tex2D(_MaskTex, uv + float2(offset.x, 0)).r;
                    float m6 = tex2D(_MaskTex, uv + float2(-offset.x, offset.y)).r;
                    float m7 = tex2D(_MaskTex, uv + float2(0, offset.y)).r;
                    float m8 = tex2D(_MaskTex, uv + float2(offset.x, offset.y)).r;
                    
                    // 주변에 사람이 있으면 현재도 사람으로
                    float maxMask = max(max(max(m1, m2), max(m3, m4)), 
                                       max(max(m5, m6), max(m7, m8)));
                    mask = max(mask, maxMask);
                }
                
                // === Fill Holes (구멍 메우기) ===
                if (_FillHoles > 0.5)
                {
                    float2 smallOffset = _MaskTex_TexelSize.xy * 2.0;
                    
                    // 주변 9칸 평균
                    float sum = 0.0;
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            sum += tex2D(_MaskTex, uv + float2(x, y) * smallOffset).r;
                        }
                    }
                    float avg = sum / 9.0;
                    
                    // 주변이 대부분 사람이면 구멍도 메우기
                    if (avg > 0.6)
                    {
                        mask = max(mask, avg * _FillHoles);
                    }
                }
                
                return mask;
            }
            
            fixed4 frag(v2f_img i) : SV_Target
            {
                // === 좌우반전 적용 ===
                float2 uv = i.uv;
                if (_MirrorHorizontal > 0.5)
                {
                    uv.x = 1.0 - uv.x;
                }
                
                // 웹캠 이미지 (반전된 UV 사용)
                fixed4 person = tex2D(_MainTex, uv);
                
                // 개선된 마스크 (반전된 UV 사용)
                float mask = GetImprovedMask(uv);
                
                // 배경 이미지 (반전된 UV 사용)
                fixed4 background = tex2D(_BackgroundTex, uv);
                
                // 부드러운 경계
                float alpha = smoothstep(_Threshold - _Smoothness, _Threshold + _Smoothness, mask);
                
                // 중간톤 제거 (회색 테두리 감소)
                alpha = pow(alpha, 1.2);
                
                // 최종 합성
                return lerp(background, person, alpha);
            }
            ENDCG
        }
    }
}
