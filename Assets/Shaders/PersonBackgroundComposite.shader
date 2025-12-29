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
        _InvertMask ("Invert Mask (마스크 반전)", Float) = 0
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
            float _InvertMask;
            float4 _MainTex_TexelSize;
            
            // 가우시안 블러 마스크 샘플링 (도트 제거용)
            float SampleMaskBlurred(float2 uv, float radius)
            {
                float2 texel = _MaskTex_TexelSize.xy * radius;

                // 9-tap 가우시안 블러 (1 2 1 / 2 4 2 / 1 2 1)
                float sum = 0.0;
                sum += tex2D(_MaskTex, uv + float2(-texel.x, -texel.y)).r * 1.0;
                sum += tex2D(_MaskTex, uv + float2(0, -texel.y)).r * 2.0;
                sum += tex2D(_MaskTex, uv + float2(texel.x, -texel.y)).r * 1.0;
                sum += tex2D(_MaskTex, uv + float2(-texel.x, 0)).r * 2.0;
                sum += tex2D(_MaskTex, uv).r * 4.0;
                sum += tex2D(_MaskTex, uv + float2(texel.x, 0)).r * 2.0;
                sum += tex2D(_MaskTex, uv + float2(-texel.x, texel.y)).r * 1.0;
                sum += tex2D(_MaskTex, uv + float2(0, texel.y)).r * 2.0;
                sum += tex2D(_MaskTex, uv + float2(texel.x, texel.y)).r * 1.0;

                return sum / 16.0;
            }

            // 경량 블러 (도트 제거 + 선명도 유지)
            float SampleMaskLightBlur(float2 uv)
            {
                // 2단계 블러 (반경 작게)
                float blur1 = SampleMaskBlurred(uv, 1.0);
                float blur2 = SampleMaskBlurred(uv, 2.0);

                // 선명한 블러 우선
                return blur1 * 0.7 + blur2 * 0.3;
            }

            // 개선된 마스크 샘플링 (선명도 + 부드러운 경계)
            float GetImprovedMask(float2 uv)
            {
                // 경량 블러로 도트만 제거 (선명도 유지)
                float mask = SampleMaskLightBlur(uv);

                // === Dilate (팽창) - 옷깃 잘림 방지 ===
                if (_Dilate > 0.001)
                {
                    float2 offset = _MaskTex_TexelSize.xy * (_Dilate * 60.0);

                    // 8방향 샘플링 (단순 블러로 빠르게)
                    float maxMask = mask;
                    maxMask = max(maxMask, SampleMaskBlurred(uv + float2(-offset.x, 0), 1.5));
                    maxMask = max(maxMask, SampleMaskBlurred(uv + float2(offset.x, 0), 1.5));
                    maxMask = max(maxMask, SampleMaskBlurred(uv + float2(0, -offset.y), 1.5));
                    maxMask = max(maxMask, SampleMaskBlurred(uv + float2(0, offset.y), 1.5));
                    maxMask = max(maxMask, SampleMaskBlurred(uv + float2(-offset.x, -offset.y), 1.5));
                    maxMask = max(maxMask, SampleMaskBlurred(uv + float2(offset.x, -offset.y), 1.5));
                    maxMask = max(maxMask, SampleMaskBlurred(uv + float2(-offset.x, offset.y), 1.5));
                    maxMask = max(maxMask, SampleMaskBlurred(uv + float2(offset.x, offset.y), 1.5));

                    // 부드럽게 확장
                    mask = lerp(mask, maxMask, 0.6);
                }

                // === Fill Holes (구멍 메우기) - 강화 ===
                if (_FillHoles > 0.01)
                {
                    // 더 넓은 범위에서 샘플링 (큰 구멍도 메우기)
                    float2 offset1 = _MaskTex_TexelSize.xy * 4.0;
                    float2 offset2 = _MaskTex_TexelSize.xy * 8.0;

                    // 가까운 범위 (3x3)
                    float sum1 = 0.0;
                    sum1 += tex2D(_MaskTex, uv + float2(-1, -1) * offset1).r;
                    sum1 += tex2D(_MaskTex, uv + float2(0, -1) * offset1).r;
                    sum1 += tex2D(_MaskTex, uv + float2(1, -1) * offset1).r;
                    sum1 += tex2D(_MaskTex, uv + float2(-1, 0) * offset1).r;
                    sum1 += tex2D(_MaskTex, uv + float2(1, 0) * offset1).r;
                    sum1 += tex2D(_MaskTex, uv + float2(-1, 1) * offset1).r;
                    sum1 += tex2D(_MaskTex, uv + float2(0, 1) * offset1).r;
                    sum1 += tex2D(_MaskTex, uv + float2(1, 1) * offset1).r;
                    float avg1 = sum1 / 8.0;

                    // 먼 범위 (3x3)
                    float sum2 = 0.0;
                    sum2 += tex2D(_MaskTex, uv + float2(-1, -1) * offset2).r;
                    sum2 += tex2D(_MaskTex, uv + float2(0, -1) * offset2).r;
                    sum2 += tex2D(_MaskTex, uv + float2(1, -1) * offset2).r;
                    sum2 += tex2D(_MaskTex, uv + float2(-1, 0) * offset2).r;
                    sum2 += tex2D(_MaskTex, uv + float2(1, 0) * offset2).r;
                    sum2 += tex2D(_MaskTex, uv + float2(-1, 1) * offset2).r;
                    sum2 += tex2D(_MaskTex, uv + float2(0, 1) * offset2).r;
                    sum2 += tex2D(_MaskTex, uv + float2(1, 1) * offset2).r;
                    float avg2 = sum2 / 8.0;

                    // 주변 평균이 높으면 (사람 내부로 판단) 구멍 메우기
                    float surroundAvg = (avg1 + avg2) * 0.5;
                    if (surroundAvg > 0.4)
                    {
                        // 구멍 강하게 메우기
                        float fillStrength = saturate((surroundAvg - 0.4) * 2.5) * _FillHoles;
                        mask = max(mask, surroundAvg * fillStrength);
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

                // 마스크 반전 옵션 (일부 GPU/환경에서 필요)
                if (_InvertMask > 0.5)
                {
                    mask = 1.0 - mask;
                }

                // 적절한 smoothstep 범위 (선명하되 부드러운 경계)
                float lowEdge = _Threshold - _Smoothness;
                float highEdge = _Threshold + _Smoothness * 0.3;
                float alpha = smoothstep(lowEdge, highEdge, mask);

                // 웹캠 이미지 (원본 그대로)
                fixed4 person = tex2D(_MainTex, uv);

                // 배경 이미지
                fixed4 background = tex2D(_BackgroundTex, uv);

                // 최종 합성
                return lerp(background, person, alpha);
            }
            ENDCG
        }
    }
}
