Shader "Fractal/Fractal Surface GPU" 
{

	Properties 
	{
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
	}
	
	SubShader 
	{
		CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
		#pragma surface ConfigureSurface Standard fullforwardshadows addshadow
		#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
		#pragma editor_sync_compilation

		#pragma target 4.5
		
		struct Input
		{
			float3 worldPos;
		};
		
		float _Smoothness;
		
		#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
			StructuredBuffer<float3x4> _Matrices;
		#endif

		void ConfigureProcedural ()
		{
			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
				float3x4 m = _Matrices[unity_InstanceID];
				unity_ObjectToWorld._m00_m01_m02_m03 = m._m00_m01_m02_m03;
				unity_ObjectToWorld._m10_m11_m12_m13 = m._m10_m11_m12_m13;
				unity_ObjectToWorld._m20_m21_m22_m23 = m._m20_m21_m22_m23;
				unity_ObjectToWorld._m30_m31_m32_m33 = float4(0.0, 0.0, 0.0, 1.0);
			#endif
		}


		float4 _Color1;
		float4 _Color2;
		float2 _SequenceFactors;
		
		float4 GetFractalColor()
		{
			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
				return lerp(_Color1, _Color2, frac(unity_InstanceID * _SequenceFactors.x + _SequenceFactors.y));
			#else
				return _Color1;
			#endif
		}
		
		void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
		{
			surface.Albedo = GetFractalColor().rgb;
			surface.Smoothness = _Smoothness;
		}
		ENDCG
	}

	FallBack "Diffuse"
}