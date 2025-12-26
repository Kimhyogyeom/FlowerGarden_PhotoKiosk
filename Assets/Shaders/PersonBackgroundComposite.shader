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
        _EdgeInset ("Edge Inset (테두리 제거)", Range(0, 0.05)) = 0.015
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
            float4 _MaskTex_TexelSize;
            
            float _Threshold;
            float _Smoothness;
            float _Dilate;
            float _FillHoles;
            float _EdgeInset;
            float _MirrorHorizontal;
            float4 _MainTex_TexelSize;
            
            // 마스크 샘플링 (최소 블러 - 선명도 우선)
            float SampleMaskBlurred(float2 uv, float radius)
            {
                // 3x3 경량 샘플링 (5x5 대신)
                float center = tex2D(_MaskTex, uv).r;
                float2 texel = _MaskTex_TexelSize.xy * radius * 0.5;

                float sum = center * 4.0;  // 중심 가중치 높게
                sum += tex2D(_MaskTex, uv + float2(-texel.x, 0)).r;
                sum += tex2D(_MaskTex, uv + float2(texel.x, 0)).r;
                sum += tex2D(_MaskTex, uv + float2(0, -texel.y)).r;
                sum += tex2D(_MaskTex, uv + float2(0, texel.y)).r;

                return sum / 8.0;
            }

            // 개선된 마스크 샘플링 (선명도 + 부드러운 경계)
            float GetImprovedMask(float2 uv)
            {
                // 경계 부드럽게 (도트 감소)
                float mask = SampleMaskBlurred(uv, 0.8);

                // === Dilate (팽창) - 옷깃 잘림 방지 ===
                if (_Dilate > 0.001)
                {
                    float2 offset = _MaskTex_TexelSize.xy * (_Dilate * 50.0);

                    // 8방향 샘플링 (블러 적용)
                    float m1 = SampleMaskBlurred(uv + float2(-offset.x, -offset.y), 0.5);
                    float m2 = SampleMaskBlurred(uv + float2(0, -offset.y), 0.5);
                    float m3 = SampleMaskBlurred(uv + float2(offset.x, -offset.y), 0.5);
                    float m4 = SampleMaskBlurred(uv + float2(-offset.x, 0), 0.5);
                    float m5 = SampleMaskBlurred(uv + float2(offset.x, 0), 0.5);
                    float m6 = SampleMaskBlurred(uv + float2(-offset.x, offset.y), 0.5);
                    float m7 = SampleMaskBlurred(uv + float2(0, offset.y), 0.5);
                    float m8 = SampleMaskBlurred(uv + float2(offset.x, offset.y), 0.5);

                    // 확장 (옷깃 잘림 방지 강화)
                    float maxMask = max(max(max(m1, m2), max(m3, m4)),
                                       max(max(m5, m6), max(m7, m8)));
                    mask = lerp(mask, maxMask, 0.85);
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
            
            // 경계에서 사람 안쪽 색상을 가져오기 (테두리 색상 제거)
            fixed4 SamplePersonInset(float2 uv, float insetAmount)
            {
                float2 texel = _MaskTex_TexelSize.xy * insetAmount * 100.0;

                // 8방향에서 샘플링해서 마스크가 가장 높은 방향(=사람 안쪽) 찾기
                float2 offsets[8] = {
                    float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1),
                    float2(-1, -1), float2(1, -1), float2(-1, 1), float2(1, 1)
                };

                float bestMask = 0.0;
                float2 bestOffset = float2(0, 0);

                for (int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + offsets[i] * texel;
                    float m = tex2D(_MaskTex, sampleUV).r;
                    if (m > bestMask)
                    {
                        bestMask = m;
                        bestOffset = offsets[i] * texel;
                    }
                }

                // 마스크가 높은 방향(사람 안쪽)에서 웹캠 샘플링
                return tex2D(_MainTex, uv + bestOffset);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                // === 좌우반전 적용 ===
                float2 uv = i.uv;
                if (_MirrorHorizontal > 0.5)
                {
                    uv.x = 1.0 - uv.x;
                }

                // 개선된 마스크 (반전된 UV 사용)
                float mask = GetImprovedMask(uv);

                // 부드러운 경계
                float alpha = smoothstep(_Threshold - _Smoothness, _Threshold + _Smoothness, mask);

                // 웹캠 이미지 (원본 그대로)
                fixed4 person = tex2D(_MainTex, uv);

                // 배경 이미지
                fixed4 background = tex2D(_BackgroundTex, uv);

                // 경계 보호: alpha가 낮아도 사람 색상 유지
                // 기존 S-curve 대신 더 급격한 커브로 경계 번짐 방지
                alpha = saturate(alpha * 1.15);  // 살짝 확장
                alpha = alpha * alpha * (3.0 - 2.0 * alpha);  // S-curve

                // 최종 합성
                return lerp(background, person, alpha);
            }
            ENDCG
        }
    }
}
