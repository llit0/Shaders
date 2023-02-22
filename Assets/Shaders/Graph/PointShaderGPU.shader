Shader "Graph/PointShaderGPU"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation
        #pragma target 4.5

        sampler2D _MainTex;

        struct Input
        {
            float3 worldPos;
        };

        half _Glossiness;
        float _Step;
        
        #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
			StructuredBuffer<float3> _Positions;
		#endif
        
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void ConfigureProcedural ()
        {
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                float3 position = _Positions[unity_InstanceID];
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
                unity_ObjectToWorld._m00_m11_m22 = _Step;
			#endif
        }
            
        void surf (Input input, inout SurfaceOutputStandard surface)
        {
            surface.Albedo.rb = saturate(input.worldPos.xy * 0.5 + 0.5);
            surface.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
