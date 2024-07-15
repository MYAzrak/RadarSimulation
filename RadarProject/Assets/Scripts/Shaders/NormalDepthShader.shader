Shader "Custom/NormalDepthShader"
{
	Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CameraNearClip ("Camera Near Clip", Float) = 0.3
        _CameraFarClip ("Camera Far Clip", Float) = 1000.0
    }
    SubShader
    {
		Tags
		{
			"RenderType" = "Opaque"
		}

		ZWrite On
		
        Pass
        {
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
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float depth : TEXCOORD1;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.depth = ComputeScreenPos(o.pos).z; // Adjust this to get depth in correct range
                return o;
            }
            
            sampler2D _MainTex;
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Example: pack normal into RGB and depth into alpha
                fixed3 normal = normalize(i.normal);
                float depth = i.depth;
                return fixed4(normal, depth);
            }
            ENDCG
        
        }
    }
}

