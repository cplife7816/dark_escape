Shader "Hidden/EdgeDetection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Edge Threshold", Range(0,1)) = 0.1
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _SphereCenter ("Sphere Center", Vector) = (0,0,0,0)
        _SphereRadius ("Sphere Radius", Float) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Threshold;
            float4 _EdgeColor;
            float3 _SphereCenter;
            float _SphereRadius;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;

                // 주변 픽셀 샘플링 (Sobel Edge Detection)
                float3 sample[9];
                sample[0] = tex2D(_MainTex, i.uv + texel * float2(-1, 1)).rgb;
                sample[1] = tex2D(_MainTex, i.uv + texel * float2( 0, 1)).rgb;
                sample[2] = tex2D(_MainTex, i.uv + texel * float2( 1, 1)).rgb;
                sample[3] = tex2D(_MainTex, i.uv + texel * float2(-1, 0)).rgb;
                sample[4] = tex2D(_MainTex, i.uv).rgb;
                sample[5] = tex2D(_MainTex, i.uv + texel * float2( 1, 0)).rgb;
                sample[6] = tex2D(_MainTex, i.uv + texel * float2(-1,-1)).rgb;
                sample[7] = tex2D(_MainTex, i.uv + texel * float2( 0,-1)).rgb;
                sample[8] = tex2D(_MainTex, i.uv + texel * float2( 1,-1)).rgb;

                // Sobel 필터 적용
                float3 gx = (-sample[0] + sample[2]) + (-2.0 * sample[3] + 2.0 * sample[5]) + (-sample[6] + sample[8]);
                float3 gy = (-sample[0] - 2.0 * sample[1] - sample[2]) + (sample[6] + 2.0 * sample[7] + sample[8]);
                float edgeStrength = length(gx + gy);

                // 엣지 감지 여부
                float edge = step(_Threshold, edgeStrength);

                // 현재 픽셀의 월드 위치가 Sphere 내부인지 검사
                float dist = distance(i.worldPos, _SphereCenter);
                bool insideSphere = dist < _SphereRadius;

                // ✅ 내부일 경우 원래 색상 유지, 외부일 경우 검은색 처리
                float3 objectColor = sample[4]; 
                float3 finalColor = insideSphere ? objectColor : float3(0, 0, 0);

                return lerp(float4(0, 0, 0, 1), float4(finalColor, 1.0), edge);
            }
            ENDHLSL
        }
    }
}
