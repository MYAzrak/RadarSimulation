Shader "Custom/NormalDepthShader"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float depth : TEXCOORD1;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.depth = o.pos.z / o.pos.w;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half3 normal = normalize(i.normal) * 0.5 + 0.5;
                half depth = i.depth;
                return half4(normal, depth);
            }
            ENDCG
        }
    }
}

