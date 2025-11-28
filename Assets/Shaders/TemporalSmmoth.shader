Shader "Hidden/TemporalSmooth"
{
    Properties
    {
        _MainTex ("Current", 2D) = "white" {}
        _PrevTex ("Previous", 2D) = "white" {}
        _Stability ("Temporal Stability", Range(0, 1)) = 0.7
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            sampler2D _PrevTex;
            float _Stability;
            
            fixed4 frag(v2f_img i) : SV_Target
            {
                float current = tex2D(_MainTex, i.uv).r;
                float previous = tex2D(_PrevTex, i.uv).r;
                
                // 이전 프레임의 영향력을 높게 (0.7 = 70%)
                float smoothed = lerp(current, previous, _Stability);
                
                return fixed4(smoothed, smoothed, smoothed, 1.0);
            }
            ENDCG
        }
    }
}
