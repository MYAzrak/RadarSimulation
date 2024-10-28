Shader "Custom/NormalDepthShader"
{
    SubShader
    {
        LOD 100

        Pass
        {
            Cull Off
            ZTest LEqual
            ZWrite On
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
                float3 viewNormal : TEXCOORD1;
                float4 projPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Transform normal to view space (camera space)
                o.viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                
                o.projPos = ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.projPos.z);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize the view space normal and convert to 0-1 range
                float3 normalColor = normalize(i.viewNormal) * 0.5 + 0.5;
                float depth = i.projPos.z;
                return fixed4(normalColor, depth);
            }
            ENDCG
        }
    }
}
