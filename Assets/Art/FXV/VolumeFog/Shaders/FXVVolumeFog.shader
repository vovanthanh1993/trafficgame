Shader "FXV/FXVVolumeFog" 
{
	Properties 
	{
		[HideInInspector] _FogType("__fogtype", Float) = 0.0
		[HideInInspector] _FogFalloffType("__fogfallofftype", Float) = 0.0
		[HideInInspector] _FogFalloff("__fogfalloff", Float) = 1.0
		[HideInInspector] _FogMeshType("__fogmeshtype", Float) = 0.0
		[HideInInspector] _FogClipping("__fogclipping", Float) = 0.0
		[HideInInspector][Toggle] _InAirEnabled("__inairenabled", Int) = 0
		[HideInInspector] _BlendMode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
		[HideInInspector][Toggle] _AxisFadeEnabled("__axisfade", Float) = 0.0
	}

	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 300

		Blend[_SrcBlend][_DstBlend]
		ZWrite[_ZWrite]

		Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

			#pragma target 3.5

			#pragma shader_feature_local FXV_FOGTYPE_VIEWALIGNED FXV_FOGTYPE_SPHERICALPOS FXV_FOGTYPE_SPHERICALDIST FXV_FOGTYPE_BOXPOS FXV_FOGTYPE_BOXDIST FXV_FOGTYPE_BOXEXPERIMENTAL FXV_FOGTYPE_HEIGHT FXV_FOGTYPE_HEIGHTXBOX FXV_FOGTYPE_INVERTEDSPHERICAL FXV_FOGTYPE_INVERTEDSPHERICALXHEIGHT FXV_FOGTYPE_HEIGHTXVIEW FXV_FOGTYPE_BOXXVIEW
			#pragma shader_feature_local FXV_IN_AIR_FOG __
			#pragma shader_feature_local FXV_LINEAR_FALLOFF FXV_SMOOTHED_FALLOFF FXV_EXP_FALLOFF FXV_EXP2_FALLOFF
			#pragma shader_feature_local FXV_FOG_CUSTOM_MESH __
			#pragma shader_feature_local FXV_FOG_CLIP_SKYBOX FXV_FOG_CLIP_BOUNDS __

			#pragma multi_compile_fog
            #pragma multi_compile_instancing 

			#define FXV_VOLUMEFOG_URP 

#if defined(FXV_VOLUMEFOG_URP)
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#else
            #include "UnityCG.cginc"	
#endif

			#include "FXVVolumeFog.cginc"

			struct appdata
			{
				FXV_VOLUMETRIC_FOG_APPDATA
			};

			struct v2f
			{
				FXV_VOLUMETRIC_FOG_V2F_COORDS
			};

			v2f vert(appdata v)
			{
				v2f o;

				FXV_VOLUMETRIC_FOG_VERTEX_DEFAULT(v, o);

				return o;
			}

			struct fragOutput
			{
				float4 color0 : SV_Target;
			};

			fragOutput frag(v2f i)
			{
				FXV_VOLUMETRIC_FOG_FRAGMENT_DEFAULT(i);

				float3 viewDirWS;
				float3 viewDirOriginWS;
				_FXV_GetViewRayOriginAndDirWS_fromPositionWS(i.positionWS, viewDirOriginWS, viewDirWS);

				fxvFogData fogData = _FXV_CalcVolumetricFog(i.positionWS, viewDirOriginWS, viewDirWS, i.depth, i.screenPosition);

				fragOutput o;

				float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

				float4 finalColor;
				finalColor.rgb = color;
				finalColor.a = color.a * fogData.fogT;
#if defined(FXV_VOLUMEFOG_DEBUG)
				finalColor = _FXV_FogDebug(finalColor, i.positionWS, viewDirOriginWS, viewDirWS, i.depth, i.screenPosition);
#endif

				o.color0.rgb = finalColor.rgb;  
				o.color0.a = finalColor.a;


				return o;
			}
			ENDHLSL
        }
		
	}

	FallBack "Diffuse"
	CustomEditor "FXV.fxvVolumeFogShaderInspector"
}
