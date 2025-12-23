#ifndef FXV_VOLUME_FOG_INCLUDED
#define FXV_VOLUME_FOG_INCLUDED

#define FXV_VOLUMETRIC_FOG_APPDATA	float3 positionOS : POSITION; \
									UNITY_VERTEX_INPUT_INSTANCE_ID

#define FXV_VOLUMETRIC_FOG_LIT_APPDATA    float4 positionOS : POSITION; \
										  float4 tangent : TANGENT; \
										  float3 normal : NORMAL; \
										  float4 texcoord : TEXCOORD0; \
										  UNITY_VERTEX_INPUT_INSTANCE_ID

#if defined(FXV_VOLUMEFOG_URP)
	#define FXV_VOLUMETRIC_FOG_V2F_COORDS	float4 positionCS : SV_POSITION; \
											float depth : TEXCOORD0; \
											float4 screenPosition : TEXCOORD1; \
											float3 positionOS : TEXCOORD2; \
											float3 positionWS : TEXCOORD3; \
											UNITY_VERTEX_INPUT_INSTANCE_ID \
											UNITY_VERTEX_OUTPUT_STEREO 
#else
	#define FXV_VOLUMETRIC_FOG_V2F_COORDS	float4 positionCS : SV_POSITION; \
											float depth : TEXCOORD0; \
											float4 screenPosition : TEXCOORD1; \
											float3 positionOS : TEXCOORD2; \
											float3 positionWS : TEXCOORD3; \
											UNITY_VERTEX_INPUT_INSTANCE_ID \
											UNITY_VERTEX_OUTPUT_STEREO 
#endif


#define FXV_VOLUMETRIC_FOG_PARTICLE_COORDS 	float depth : TEXCOORD2; \
											float4 vertexPos : TEXCOORD3; \
											float3 positionWS : TEXCOORD4; \
											float4 projPos : TEXCOORD5;

#if defined(FXV_VOLUMEFOG_URP)
	#define FXV_VOLUMETRIC_FOG_VERTEX_DEFAULT(v, o)	UNITY_SETUP_INSTANCE_ID(v); \
													UNITY_TRANSFER_INSTANCE_ID(v, o); \
													UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); \
													half isOrtho = unity_OrthoParams.w; \
													VertexPositionInputs vertInputs = GetVertexPositionInputs (v.positionOS.xyz); \
													o.positionCS = vertInputs.positionCS; \
													o.screenPosition = vertInputs.positionNDC; \
													o.positionOS = v.positionOS; \
													o.positionWS = _FXV_ObjectToWorldPos(v.positionOS); \
													o.depth = _FXV_ComputeVertexDepth(o.positionWS);

	#define FXV_VOLUMETRIC_FOG_VERTEX_DEFAULT_LIT(v, o)	half isOrtho = unity_OrthoParams.w; \
														VertexPositionInputs vertInputs = GetVertexPositionInputs (v.positionOS.xyz); \
														o.positionCS = vertInputs.positionCS; \
														o.screenPosition = vertInputs.positionNDC; \
														o.positionOS = v.positionOS; \
														o.positionWS = _FXV_ObjectToWorldPos(v.positionOS); \
														o.depth = _FXV_ComputeVertexDepth(o.positionWS);
#else
	#define FXV_VOLUMETRIC_FOG_VERTEX_DEFAULT(v, o)	UNITY_SETUP_INSTANCE_ID(v); \
													UNITY_TRANSFER_INSTANCE_ID(v, o); \
													UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); \
													half isOrtho = unity_OrthoParams.w; \
													o.positionCS = UnityObjectToClipPos(v.positionOS.xyz); \
													o.screenPosition = ComputeNonStereoScreenPos(o.positionCS); \
													float3 p = UnityObjectToViewPos(v.positionOS); \
													o.positionOS = v.positionOS; \
													o.positionWS = _FXV_ObjectToWorldPos(v.positionOS); \
													o.depth = _FXV_ComputeVertexDepth(o.positionWS); \

	#define FXV_VOLUMETRIC_FOG_VERTEX_DEFAULT_LIT(v, o)	half isOrtho = unity_OrthoParams.w; \
														appdata vv = (appdata)v; \
														o.positionCS = UnityObjectToClipPos(vv.positionOS.xyz); \
														o.screenPosition = ComputeNonStereoScreenPos(o.positionCS); \
														float3 p = UnityObjectToViewPos(vv.positionOS); \
														o.positionOS = vv.positionOS; \
														o.positionWS = _FXV_ObjectToWorldPos(vv.positionOS); \
														o.depth = _FXV_ComputeVertexDepth(o.positionWS);
#endif

float4x4 _fxv_ObjectToWorldCustom;
int _FogDebugMode;

#define FXV_VOLUMETRIC_FOG_FRAGMENT_DEFAULT(i)  UNITY_SETUP_INSTANCE_ID(i) \
												UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#if !defined(FXV_VOLUMEFOG_HDRP)
#ifdef FXV_VOLUMEFOG_URP
    TEXTURE2D_X_FLOAT(_CameraDepthTexture); 
    SAMPLER(sampler_CameraDepthTexture);
#else
	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
#endif
#endif

UNITY_INSTANCING_BUFFER_START(Props)

	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
	UNITY_DEFINE_INSTANCED_PROP(float4, _WorldSize)
#if defined(FXV_FOG_CUSTOM_MESH)
	UNITY_DEFINE_INSTANCED_PROP(float4, _LocalSize)
	UNITY_DEFINE_INSTANCED_PROP(float4, _LocalOffset)
#endif

	UNITY_DEFINE_INSTANCED_PROP(half, _FogMin)
	UNITY_DEFINE_INSTANCED_PROP(half, _FogMax)
	UNITY_DEFINE_INSTANCED_PROP(half, _FogFalloff)
	UNITY_DEFINE_INSTANCED_PROP(half, _InAirSmooth)
	UNITY_DEFINE_INSTANCED_PROP(half, _LightScatteringFactor)
	UNITY_DEFINE_INSTANCED_PROP(half, _LightReflectivity)
	UNITY_DEFINE_INSTANCED_PROP(half, _LightTransmission)

#if defined(FXV_FOGTYPE_HEIGHTXBOX) || defined(FXV_FOGTYPE_HEIGHTXVIEW) || defined(FXV_FOGTYPE_BOXXVIEW) || defined(FXV_FOGTYPE_INVERTEDSPHERICALXHEIGHT)
	UNITY_DEFINE_INSTANCED_PROP(half, _SecFogMin)
	UNITY_DEFINE_INSTANCED_PROP(half, _SecFogMax)
#endif

UNITY_INSTANCING_BUFFER_END(Props)

inline float3 _FXV_WorldToViewPos(in float3 pos)
{
#if defined(FXV_VOLUMEFOG_URP)
	return TransformWorldToView(pos);
#else
	return mul(UNITY_MATRIX_V, float4(pos, 1.0)).xyz;
#endif
}

inline float3 _FXV_WorldToViewPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return _FXV_WorldToViewPos(pos.xyz);
}

inline float3 _FXV_WorldToViewDir(in float3 dir)
{
#if defined(FXV_VOLUMEFOG_URP)
	return TransformWorldToViewDir(dir);
#else
    return mul(UNITY_MATRIX_V, float4(dir, 0.0)).xyz;
#endif
}

inline float3 _FXV_WorldToViewDir(float4 dir) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return _FXV_WorldToViewDir(dir.xyz);
}

inline float3 _FXV_WorldToObjectPos(in float3 pos)
{
#if defined(FXV_VOLUMEFOG_URP)
	return TransformWorldToObject(pos);
#else
	return mul(unity_WorldToObject, float4(pos, 1)).xyz;
#endif
}

inline float3 _FXV_WorldToObjectPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return _FXV_WorldToObjectPos(pos.xyz);
}

inline float3 _FXV_WorldToObjectDir(in float3 dir)
{
#if defined(FXV_VOLUMEFOG_URP)
	return mul(GetWorldToObjectMatrix(), float4(dir, 0)).xyz;
#else
	return mul(unity_WorldToObject, float4(dir, 0)).xyz;
#endif
}

inline float3 _FXV_WorldToObjectDir(float4 dir) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return _FXV_WorldToObjectDir(dir.xyz);
}

inline float3 _FXV_ObjectToWorldPos(in float3 pos)
{
#if defined(FXV_VOLUMEFOG_URP)
	return TransformObjectToWorld(pos);
#else
	return mul(unity_ObjectToWorld, float4(pos, 1)).xyz;
#endif
}

inline float3 _FXV_ObjectToWorldPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return _FXV_ObjectToWorldPos(pos.xyz);
}

inline float3 _FXV_ObjectToWorldDir(in float3 dir)
{
#if defined(FXV_VOLUMEFOG_URP)
	return mul(GetObjectToWorldMatrix(), float4(dir, 0)).xyz;
#else
	return mul(unity_ObjectToWorld, float4(dir, 0)).xyz;
#endif
}

inline float3 _FXV_ObjectToWorldDir(float4 dir) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return _FXV_ObjectToWorldDir(dir.xyz);
}

inline float3 _FXV_ObjectToViewPos(in float3 pos)
{
#if defined(FXV_VOLUMEFOG_URP)
	return TransformWorldToView(TransformObjectToWorld(pos));
#else
	return mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, float4(pos, 1.0))).xyz;
#endif
}

inline float3 _FXV_ObjectToViewPos(float4 pos) // overload for float4; avoids "implicit truncation" warning for existing shaders
{
    return _FXV_ObjectToViewPos(pos.xyz);
}


// Z buffer to linear depth.
// Works in all cases.
// Typically, this is the cheapest variant, provided you've already computed 'positionWS'.
// Assumes that the 'positionWS' is in front of the camera.
float _FXV_ComputeVertexDepth(float3 positionWS)
{
#if defined(FXV_VOLUMEFOG_URP)
	return LinearEyeDepth(positionWS, GetWorldToViewMatrix());
#elif defined(FXV_VOLUMEFOG_HDRP)
	return LinearEyeDepth(positionWS, GetWorldToViewMatrix());
#else
	// calculated as in Library\PackageCache\com.unity.render-pipelines.core@14.0.11\ShaderLibrary\Common.hlsl
    float viewSpaceZ = mul(UNITY_MATRIX_V, float4(positionWS, 1.0)).z;
    // If the matrix is right-handed, we have to flip the Z axis to get a positive value.
    return abs(viewSpaceZ);
#endif
}

half _FXV_GetRawDepth(half4 screenPos)
{
#if defined(FXV_VOLUMEFOG_URP)
	float z = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(screenPos.xy / screenPos.w)).r;
#elif defined(FXV_VOLUMEFOG_HDRP)
	float z = SampleCameraDepth( screenPos.xy / screenPos.w );
#else
    float z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(screenPos.xy / screenPos.w));
    //float z = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(screenPos));
#endif
	return z;
}

half _FXV_GetLinearEyeDepth(float z)
{
    if (unity_OrthoParams.w == 0) // perspective
    {
		// Perspective linear depth
#if defined(FXV_VOLUMEFOG_URP)
		return LinearEyeDepth(z, _ZBufferParams);
#elif defined(FXV_VOLUMEFOG_HDRP)
		return LinearEyeDepth(z, _ZBufferParams);
#else
        return LinearEyeDepth(z);
#endif
    }
	else
    {
		// Orthographic linear depth
		// near = _ProjectionParams.y;
		// far = _ProjectionParams.z;
		// calculated as in Library\PackageCache\com.unity.render-pipelines.universal@14.0.11\ShaderLibrary\ShaderVariablesFunctions.hlsl
#if UNITY_REVERSED_Z
		return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * z;
#else
        return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * z;
#endif
    }
}

half _FXV_GetLinear01Depth(float z)
{
    if (unity_OrthoParams.w == 0) // perspective
    {
		// Perspective linear depth
#if defined(FXV_VOLUMEFOG_URP)
		return Linear01Depth(z, _ZBufferParams);
#elif defined(FXV_VOLUMEFOG_HDRP)
		return Linear01Depth(z, _ZBufferParams);
#else
        return Linear01Depth(z);
#endif
    }
    else
    {
		//Not Implemented
        return 0;
    }
}

/*float3 _FXV_CalcWorldRay(float3 viewPos)
{
	float isOrtho = unity_OrthoParams.w; // 0 - perspective, 1 - ortho

	// Perspective camera: View space normalized ray directing vertex
	float3 rayPers = normalize(viewPos.xyz);
	// This line is equivalent to:
	// rayPers /= dot(rayPers, float3(0.0, 0.0, -1.0));
	rayPers /= -rayPers.z;

	// Orthographic camera: view space vertex position
	float3 rayOrtho = float3(viewPos.xy, 0.0);

	return lerp(mul(UNITY_MATRIX_I_V, rayPers), rayOrtho, isOrtho);
}*/

float3 _FXV_GetViewForwardDir()
{
#if defined(FXV_VOLUMEFOG_URP)
    float4x4 viewMat = GetWorldToViewMatrix();
#else
	float4x4 viewMat = UNITY_MATRIX_V;
#endif

    return -viewMat[2].xyz;
}

float3 _FXV_GetViewRayOriginWS_fromPositionOS(float3 positionOS)
{
	float isOrtho = unity_OrthoParams.w; // 0 - perspective, 1 - ortho

	if (isOrtho == 0)
	{
		return _WorldSpaceCameraPos;
	}
	else
	{
		float3 p = _FXV_ObjectToViewPos(positionOS);
		return mul(UNITY_MATRIX_I_V, float4(p.xy, 0, 0)).xyz + _WorldSpaceCameraPos;
	}
}

float3 _FXV_GetViewRayOriginWS_fromPositionWS(float3 positionWS)
{
	float isOrtho = unity_OrthoParams.w; // 0 - perspective, 1 - ortho

	if (isOrtho == 0)
	{
		return _WorldSpaceCameraPos;
	}
	else
	{
		float3 p = _FXV_WorldToViewPos(positionWS);
        return mul(UNITY_MATRIX_I_V, float4(p.xy, 0, 0)).xyz + _WorldSpaceCameraPos;
    }
}

float3 _FXV_GetViewRayDirWS_fromPositionOS(float3 positionOS)
{
	float isOrtho = unity_OrthoParams.w; // 0 - perspective, 1 - ortho

	if (isOrtho == 0)
	{
		float3 p = _FXV_ObjectToViewPos(positionOS);

		// Perspective camera: View space normalized ray directing vertex
		float3 rayPers = normalize(p.xyz);
		// This line is equivalent to:
		// rayPers /= dot(rayPers, float3(0.0, 0.0, -1.0));
		rayPers /= -rayPers.z;

        return mul(UNITY_MATRIX_I_V, float4(rayPers, 0)).xyz;
    }
	else
	{
		return _FXV_GetViewForwardDir();
	}
}

float3 _FXV_GetViewRayDirWS_fromPositionWS(float3 positionWS)
{
	float isOrtho = unity_OrthoParams.w; // 0 - perspective, 1 - ortho

	if (isOrtho == 0)
	{
		float3 p = _FXV_WorldToViewPos(positionWS);

		// Perspective camera: View space normalized ray directing vertex
		float3 rayPers = normalize(p.xyz);
		// This line is equivalent to:
		// rayPers /= dot(rayPers, float3(0.0, 0.0, -1.0));
		rayPers /= -rayPers.z;

        return mul(UNITY_MATRIX_I_V, float4(rayPers, 0)).xyz;
    }
	else
	{
		return _FXV_GetViewForwardDir();
	}
}

void _FXV_GetViewRayOriginAndDirWS_fromPositionOS(float3 positionOS, out float3 rayOriginWS, out float3 rayDirWS)
{
	float isOrtho = unity_OrthoParams.w; // 0 - perspective, 1 - ortho
	
	float3 p = _FXV_ObjectToViewPos(positionOS);

	if (isOrtho == 0)
	{
		// Perspective camera: View space normalized ray directing vertex
		float3 rayPers = normalize(p.xyz);
		// This line is equivalent to:
		// rayPers /= dot(rayPers, float3(0.0, 0.0, -1.0));
		rayPers /= -rayPers.z;

        rayDirWS = mul(UNITY_MATRIX_I_V, float4(rayPers, 0)).xyz;
		rayOriginWS = _WorldSpaceCameraPos;
	}
	else
	{
		rayDirWS = _FXV_GetViewForwardDir();
		rayOriginWS = mul(UNITY_MATRIX_I_V, float4(p.xy, 0, 0)).xyz + _WorldSpaceCameraPos;
	}
}

void _FXV_GetViewRayOriginAndDirWS_fromPositionWS(float3 positionWS, out float3 rayOriginWS, out float3 rayDirWS)
{
	float isOrtho = unity_OrthoParams.w; // 0 - perspective, 1 - ortho

	float3 p = _FXV_WorldToViewPos(positionWS);

	if (isOrtho == 0)
	{
		// Perspective camera: View space normalized ray directing vertex
		float3 rayPers = normalize(p.xyz);
		// This line is equivalent to:
		// rayPers /= dot(rayPers, float3(0.0, 0.0, -1.0));
		rayPers /= -rayPers.z;

        rayDirWS = mul(UNITY_MATRIX_I_V, float4(rayPers, 0)).xyz;
		rayOriginWS = _WorldSpaceCameraPos;
	}
	else
	{
		rayDirWS = _FXV_GetViewForwardDir();
        rayOriginWS = mul(UNITY_MATRIX_I_V, float4(p.xy, 0, 0)).xyz + _WorldSpaceCameraPos;
    }
}

struct fxvFogData
{
	float fogT;
	float fogDist;
	float tNear;
	float tFar;
	float3 pNear;
	float3 pFar;
	float3 debugRGB;
};

struct fxvIntersection
{
	float tNear;
	float tFar;
	float3 pNear;
	float3 pFar;
	float isIntersection;
};

struct fxvRay
{
	float3 rayOrigin;
	float3 rayDir;
	float3 invRayDir;
};

float _FXV_FogT_Default(float fogDist, float fogMin, float fogMax)
{
	return saturate((fogDist - fogMin) / (fogMax - fogMin));
}

fxvRay _FXV_GetWSRay(float3 rayOriginWS, float3 rayDirWS)
{
	fxvRay r = (fxvRay)0;

	r.rayOrigin = rayOriginWS;
	r.rayDir = rayDirWS;

	return r;
}

fxvRay _FXV_GetOSRay(float3 rayOriginWS, float3 rayDirWS)
{
	fxvRay r = (fxvRay)0;

	r.rayOrigin = _FXV_WorldToObjectPos(rayOriginWS);
	r.rayDir = normalize(_FXV_WorldToObjectDir(rayDirWS));

	return r;
}

fxvRay _FXV_GetOSRay_I(float3 rayOriginWS, float3 rayDirWS)
{
	fxvRay r = (fxvRay)0;

	r.rayOrigin = _FXV_WorldToObjectPos(rayOriginWS);
	r.rayDir = normalize(_FXV_WorldToObjectDir(rayDirWS));

	r.invRayDir = 1.0 / r.rayDir;

	return r;
}

fxvIntersection _FXV_BoxIntersectionOS_T(fxvRay rayOS)
{
	fxvIntersection i = (fxvIntersection)0;

#if defined(FXV_FOG_CUSTOM_MESH)
	float3 boxSizeHalfOS = UNITY_ACCESS_INSTANCED_PROP(Props, _LocalSize).xyz * 0.5;
	float3 boundsOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _LocalOffset).xyz;
#else
	float3 boxSizeHalfOS = float3(0.5, 0.5, 0.5);
	float3 boundsOffset = float3(0.0, 0.0, 0.0);
#endif

	float3 tMin = (-boxSizeHalfOS - rayOS.rayOrigin + boundsOffset) * rayOS.invRayDir;
	float3 tMax = (boxSizeHalfOS - rayOS.rayOrigin + boundsOffset) * rayOS.invRayDir;
	float3 t1 = min(tMin, tMax);
	float3 t2 = max(tMin, tMax);
	i.tNear = max(max(max(t1.x, t1.y), t1.z), 0.0);
	i.tFar = min(min(t2.x, t2.y), t2.z);

	i.isIntersection = step(i.tNear, i.tFar);

	return i;
}

fxvIntersection _FXV_BoxIntersectionOS_TP(fxvRay rayOS)
{
	fxvIntersection i = _FXV_BoxIntersectionOS_T(rayOS);

	i.pNear = rayOS.rayOrigin + rayOS.rayDir * i.tNear;
	i.pFar = rayOS.rayOrigin + rayOS.rayDir * i.tFar;

	return i;
}

fxvIntersection _FXV_SphereIntersectionWS_T(fxvRay rayWS, float3 spherePosWS, float sphereRadius)
{
	fxvIntersection i = (fxvIntersection)0;

	// dist to edge inside of sphere calculation
	// https://www.lighthouse3d.com/tutorials/maths/ray-sphere-intersection/
	float3 p = rayWS.rayOrigin;
	float3 c = spherePosWS;
	float3 vpc = spherePosWS - rayWS.rayOrigin;
	float3 d = rayWS.rayDir;
	float3 pc = -vpc - d * dot(d, -vpc); // projected c onto d
	float3 pcc = pc + c; // the same projected but worldspace
	float pc2 = dot(pc, pc);  //squared length
	float pcit = sqrt(sphereRadius * sphereRadius - pc2); //dist from pc to intersection
	float3 pccp = pcc - p; // vector from projected to camera position
	float tp = dot(pccp, d);

	i.tNear = max(0.0, tp - pcit); // dist from p to the near intersection point
	i.tFar = tp + pcit; // dist from p to the far intersection point

	i.isIntersection = step(i.tNear, i.tFar);

	return i;
}

fxvIntersection _FXV_SphereIntersectionWS_TP(fxvRay rayWS, float3 spherePosWS, float sphereRadius)
{
	fxvIntersection i = _FXV_SphereIntersectionWS_T(rayWS, spherePosWS, sphereRadius);

	i.pNear = rayWS.rayOrigin + rayWS.rayDir * i.tNear;
	i.pFar = rayWS.rayOrigin + rayWS.rayDir * i.tFar;

	return i;
}

fxvFogData _FXV_ViewAlignedFog(float depth, float inputDepth, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float3 worldObjectPos, float fogMin, float fogMax, float isOrtho)
{
	float3 viewDirNormWS = normalize(viewDirWS); // viewDirWS is not normalized to reconstruct position from depth
	float3 depthPoint = viewDirWS * depth;

	float tDepthWS = dot(viewDirNormWS, viewDirWS * depth);

	float3 boxSizeHalfWS = UNITY_ACCESS_INSTANCED_PROP(Props, _WorldSize).xyz * 0.5;

	float tNearWS = 0.0;//dot(viewDirNormWS, worldObjectPos - viewDirOriginWS);

	float3 boxSizeHalfOS = float3(0.5,0.5,0.5);

	float dist = tDepthWS - tNearWS;

	fxvFogData fogData = (fxvFogData)0;

	fogData.tNear = tNearWS;

#ifdef FXV_IN_AIR_FOG

	fxvRay rayOS = _FXV_GetOSRay_I(viewDirOriginWS, viewDirWS);
	fxvIntersection i = _FXV_BoxIntersectionOS_TP(rayOS);

	float3 pFarWS = _FXV_ObjectToWorldPos(i.pFar);

	float tFarWS = dot(viewDirNormWS, pFarWS - viewDirOriginWS);

	fogData.tFar = tFarWS;

	dist = min(dist, tFarWS - tNearWS);

#endif

	fogData.fogDist = dist;
	fogData.fogT = _FXV_FogT_Default(fogData.fogDist, fogMin, fogMax);

#ifdef FXV_IN_AIR_FOG
	fogData.fogT *= i.isIntersection;

	fogData.tNear = i.tNear;
	fogData.tFar = i.tFar;
	fogData.pNear = _FXV_ObjectToWorldPos(i.pNear);
	fogData.pFar = pFarWS;
#endif

	return fogData;
}

fxvFogData _FXV_SphericalFog_RayDist(float depth, float inputDepth, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float3 worldObjectPos, float fogMin, float fogMax, float isOrtho)
{
	float3 viewDirNormWS = normalize(viewDirWS); // viewDirWS is not normalized to recnstruc position from depth
	float distToDepth = dot(viewDirNormWS, viewDirWS * depth); //reconstruct position from depth and get it's distance from view origin

	float3 pNear = worldPosition;

#ifdef FXV_IN_AIR_FOG

	fxvRay rayWS = _FXV_GetWSRay(viewDirOriginWS, viewDirNormWS);
	fxvIntersection i = _FXV_SphereIntersectionWS_TP(rayWS, worldObjectPos, fogMax);

	pNear = i.pNear;

	distToDepth = min(distToDepth, i.tFar); //select what is closer - dist to reconstructed depth, or dist to far intersection with sphere

#endif

	float fogIntersectionDepth = distToDepth - dot(viewDirNormWS, pNear - viewDirOriginWS); //distance ray is travelling inside fog

	fxvFogData fogData = (fxvFogData)0;
	fogData.fogDist = fogMax - 0.5 * fogIntersectionDepth;
	fogData.fogT = _FXV_FogT_Default(fogData.fogDist, fogMin, fogMax);
	fogData.fogT = 1.0 - fogData.fogT;

#ifdef FXV_IN_AIR_FOG

	fogData.fogT *= i.isIntersection;

#endif

#ifdef FXV_IN_AIR_FOG
	fogData.tNear = i.tNear;
	fogData.tFar = i.tFar;
	fogData.pNear = i.pNear;
	fogData.pFar = i.pFar;
#endif

	return fogData;
}

fxvFogData _FXV_SphericalFog(float depth, float inputDepth, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float3 worldObjectPos, float fogMin, float fogMax, float isOrtho)
{
	float3 viewDirNormWS = normalize(viewDirWS); // viewDirWS is not normalized to recnstruc position from depth
	float distToDepth = dot(viewDirNormWS, viewDirWS * depth); //reconstruct position from depth and get it's distance from view origin

#ifdef FXV_IN_AIR_FOG

	float airZ;

	if (isOrtho == 1)
	{
		airZ = -_FXV_WorldToViewPos(worldObjectPos).z;
	}
	else
	{
		float3 pointDiff = worldPosition - worldObjectPos;
		float3 camToObj = worldObjectPos - viewDirOriginWS;
		float3 camDirToObj = normalize(camToObj);
		float3 camDirToPoint = normalize(viewDirWS);

		float3 viewPerp = pointDiff - camDirToObj * dot(camDirToObj, pointDiff);
		float3 viewPerpLocal = _FXV_WorldToObjectDir(viewPerp);
		float3 pv = _FXV_ObjectToViewPos(viewPerpLocal);

		float dd = dot(camDirToObj, camDirToPoint);
		airZ = length(pv) * dd * dd;
	}

	distToDepth = min(distToDepth, airZ);

#endif

	float3 worldspace = viewDirOriginWS + viewDirNormWS * distToDepth;

	float3 diff = worldObjectPos - worldspace;
	float dist = length(diff);

	fxvFogData fogData = (fxvFogData)0;
	fogData.fogDist = dist;
	fogData.fogT = _FXV_FogT_Default(fogData.fogDist, fogMin, fogMax);
	fogData.fogT = 1.0 - fogData.fogT;

	return fogData;
}

fxvFogData _FXV_BoxFog_RayDist(float depth, float inputDepth, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float3 worldObjectPos, float fogMin, float fogMax, float isOrtho)
{
#if defined(FXV_FOG_CUSTOM_MESH)
	float3 boxLocalOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _LocalOffset).xyz;
	worldObjectPos += _FXV_ObjectToWorldDir(boxLocalOffset);
#endif

	float3 viewDirNormWS = normalize(viewDirWS); // viewDirWS is not normalized to recnstruct position from depth
	float3 depthPoint = viewDirWS * depth;

	float tDepth = dot(viewDirNormWS, viewDirWS * depth);

	float tNearWS = dot(viewDirNormWS, worldPosition - viewDirOriginWS); // this will not work if camera view plane is partialy intersecting box

	float dist = tDepth;

	fxvFogData fogData = (fxvFogData)0;

	fogData.tNear = tNearWS;

#ifdef FXV_IN_AIR_FOG

	fxvRay rayOS = _FXV_GetOSRay_I(viewDirOriginWS, viewDirWS);
	fxvIntersection i = _FXV_BoxIntersectionOS_TP(rayOS);

	float3 pNearWS = _FXV_ObjectToWorldPos(i.pNear);
	float3 pFarWS = _FXV_ObjectToWorldPos(i.pFar);

	tNearWS = dot(viewDirNormWS, pNearWS - viewDirOriginWS);
	float tFarWS = dot(viewDirNormWS, pFarWS - viewDirOriginWS);

	fogData.tNear = tNearWS;
	fogData.tFar = tFarWS;

	dist = min(dist, tFarWS);

#endif

	fogData.fogDist = dist - tNearWS;
	fogData.fogT = _FXV_FogT_Default(fogData.fogDist, fogMin, fogMax);

#ifdef FXV_IN_AIR_FOG
	fogData.tNear = i.tNear;
	fogData.tFar = i.tFar;
	fogData.pNear = pNearWS;
	fogData.pFar = pFarWS;
#endif

	return fogData;
}

fxvFogData _FXV_BoxFog(float depth, float inputDepth, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float3 worldObjectPos, float fogMin, float fogMax, float isOrtho)
{
#if defined(FXV_FOG_CUSTOM_MESH)
	float3 boxLocalOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _LocalOffset).xyz;
	worldObjectPos += _FXV_ObjectToWorldDir(boxLocalOffset);
#endif

	float3 viewDirNormWS = normalize(viewDirWS); // viewDirWS is not normalized to recnstruct position from depth
	float3 depthPoint = viewDirWS * depth;

	float3 worldspaceDepthPoint = viewDirOriginWS + depthPoint;

	float3 localDepthPoint = _FXV_WorldToObjectPos(worldspaceDepthPoint);

	float3 boxSizeWS = UNITY_ACCESS_INSTANCED_PROP(Props, _WorldSize).xyz;
	float3 boxSizeHalfWS = boxSizeWS * 0.5;

	float minScaling = min(min(boxSizeWS.x, boxSizeWS.y), boxSizeWS.z);
	float maxScaling = max(max(boxSizeWS.x, boxSizeWS.y), boxSizeWS.z);

	float3 scaling =  boxSizeWS / maxScaling;

	float3 diffTestDepth = localDepthPoint * boxSizeHalfWS;
	float dist = max(max(abs(diffTestDepth.x), abs(diffTestDepth.y)), abs(diffTestDepth.z));

#ifdef FXV_IN_AIR_FOG

	fxvRay rayOS = _FXV_GetOSRay_I(viewDirOriginWS, viewDirWS);
	fxvIntersection i = _FXV_BoxIntersectionOS_TP(rayOS);

	float tDepth = dot(rayOS.rayDir, localDepthPoint - rayOS.rayOrigin);

	float tLimit = min(i.tFar, tDepth);

	float3 currentPos = i.pNear;

	float stepT = i.tNear;

	float3 diffTest = currentPos;
	float distTest = max(max(abs(diffTest.x), abs(diffTest.y)), abs(diffTest.z));

	for (int s = 0; s < 255; s++)
	{
		float3 diffTestNew = currentPos;
		float distTestNew = max(max(abs(diffTestNew.x), abs(diffTestNew.y)), abs(diffTestNew.z));

		distTest = min(distTestNew, distTest);
		stepT += 1.0/255.0;
		stepT = min(stepT, tLimit);
		currentPos = rayOS.rayOrigin + rayOS.rayDir * stepT;
	}

	dist = distTest;

#endif

	float fogMin2 = (0.5 - fogMax);
	float fogMax2 = (0.5 - fogMin);

	fxvFogData fogData = (fxvFogData)0;
	fogData.fogDist = dist;
	fogData.fogT = _FXV_FogT_Default(fogData.fogDist, fogMin2, fogMax2);
	fogData.fogT = 1.0 - fogData.fogT;

#ifdef FXV_IN_AIR_FOG
	float3 pNearWS = _FXV_ObjectToWorldPos(i.pNear);
	float3 pFarWS = _FXV_ObjectToWorldPos(i.pFar);
	fogData.tNear = i.tNear;
	fogData.tFar = i.tFar;
	fogData.pNear = pNearWS;
	fogData.pFar = pFarWS;
#endif

	return fogData;
}

fxvFogData _FXV_HeightFog(float depth, float inputDepth, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float3 worldObjectPos, float fogMin, float fogMax, float isOrtho, float3 axis)
{
#if defined(FXV_FOG_CUSTOM_MESH)
	float3 boxLocalOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _LocalOffset).xyz;
	worldObjectPos += _FXV_ObjectToWorldDir(boxLocalOffset);
#endif

	float3 worldspace = viewDirOriginWS + viewDirWS * depth;

	float3 diff = worldObjectPos - worldspace;

	//axis = float3(0, 1, 0);
	float3 worldAxis = normalize(_FXV_ObjectToWorldDir(axis));
	//diff = _FXV_WorldToObjectDir(diff); //rotation support

	float3 boxSizeHalfWS = UNITY_ACCESS_INSTANCED_PROP(Props, _WorldSize).xyz * 0.5;
	float3 boxSizeHalfOS = float3(0.5, 0.5, 0.5);

	float axisBoxSize = abs(dot(axis, boxSizeHalfWS));

	float dist = dot(worldAxis, diff) + axisBoxSize;

#ifdef FXV_IN_AIR_FOG

	fxvRay rayOS = _FXV_GetOSRay_I(viewDirOriginWS, viewDirWS);
	fxvIntersection i = _FXV_BoxIntersectionOS_TP(rayOS);

	float3 pNearWS = _FXV_ObjectToWorldPos(i.pNear);
	float3 pFarWS = _FXV_ObjectToWorldPos(i.pFar);

	float3 diff2 = worldObjectPos - pFarWS;

	float dist2 = dot(worldAxis, diff2) + axisBoxSize;

	dist = lerp(dist, dist2, step(0, dot(viewDirWS, worldspace - pFarWS))); //equivalent of if (dot(viewDirWS, worldspace - pFarWS) > 0.0) dist = dist2;

	//pNearWS = worldPosition; //we can use worldPosition instead of calculationg pNear, but not when plane is mid intersecting with box

	float3 diff3 = worldObjectPos - pNearWS;

	float dist3 = dot(worldAxis, diff3) + axisBoxSize;

	dist = max(dist, dist3);

#endif

	fxvFogData fogData = (fxvFogData)0;
	fogData.fogDist = dist;
	fogData.fogT = _FXV_FogT_Default(fogData.fogDist, fogMin, fogMax);

#ifdef FXV_IN_AIR_FOG
	float3 pNearVS = _FXV_ObjectToViewPos(i.pNear); //TODO calc only when plane intersection
	i.isIntersection *= step(0, depth + pNearVS.z);

	fogData.fogT *= i.isIntersection;

	fogData.tNear = i.tNear;
	fogData.tFar = i.tFar;
	fogData.pNear = pNearWS;
	fogData.pFar = pFarWS;
#endif

	return fogData;
}

fxvFogData _FXV_InverseSpherical(float depth, float inputDepth, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float3 worldObjectPos, float fogMin, float fogMax, float isOrtho)
{
	float3 pDepth = viewDirOriginWS + viewDirWS * depth;

	float3 diff = worldObjectPos - pDepth;

	float3 boxSizeHalfWS = UNITY_ACCESS_INSTANCED_PROP(Props, _WorldSize).xyz * 0.5;
	float3 boxSizeHalfOS = float3(0.5, 0.5, 0.5);

	float fade = 1.0;

	float dist = length(diff);

#ifdef FXV_IN_AIR_FOG
	fxvRay rayOS = _FXV_GetOSRay_I(viewDirOriginWS, viewDirWS);
	fxvIntersection i = _FXV_BoxIntersectionOS_TP(rayOS);

	fxvRay rayWS = _FXV_GetWSRay(viewDirOriginWS, normalize(viewDirWS));
	fxvIntersection is = _FXV_SphereIntersectionWS_TP(rayWS, worldObjectPos, fogMax);

	float3 pNearWS = _FXV_ObjectToWorldPos(i.pNear);
	float3 pFarWS = _FXV_ObjectToWorldPos(i.pFar);

	i.tNear = dot(rayWS.rayDir, pNearWS - viewDirOriginWS);
	i.tFar = dot(rayWS.rayDir, pFarWS - viewDirOriginWS);
	float tDepth = dot(rayWS.rayDir, pDepth - viewDirOriginWS);

	dist = lerp(dist, length(worldObjectPos - pFarWS), step(0, dot(viewDirWS, pDepth - pFarWS)));  //equivalent of if (dot(viewDirWS, pDepth - pFarWS) > 0.0) dist = length(worldObjectPos - pFarWS);

	if (is.isIntersection == 1)
	{
		fade = clamp(max(0.0, (is.tNear - i.tNear)) / 1, 0, 1);

		fade = max(fade, clamp(max(0.0, (min(i.tFar, tDepth) - is.tFar)) / 1, 0, 1));
	}
#endif

	fxvFogData fogData = (fxvFogData)0;
	fogData.fogDist = dist;
	fogData.fogT = saturate(_FXV_FogT_Default(fogData.fogDist, fogMin, fogMax) + fade);

#ifdef FXV_IN_AIR_FOG
	fogData.fogT *= i.isIntersection;

	fogData.tNear = i.tNear;
	fogData.tFar = i.tFar;
	fogData.pNear = pNearWS;
	fogData.pFar = pFarWS;
#endif

	return fogData;
}

#define _fxv_PI 3.14159265359
#define _fxv_HALF_PI 1.57079632679
#define _fxv_SMOOTH 2.88539 // 2.0 / log(2.0)

float smoothestStep(float x, float s)
{
  s *= _fxv_SMOOTH;
  return 1.0 / (1.0 + exp2(tan(x * _fxv_PI - _fxv_HALF_PI) * -s)) + 0.5;
}

fxvFogData _FXV_CalcPrimaryFogData(float depth, float inputDepth, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float3 worldObjectPos, float fogMin, float fogMax, float isOrtho)
{
	fxvFogData fogData = (fxvFogData)0;

#ifdef FXV_FOGTYPE_VIEWALIGNED

	fogData = _FXV_ViewAlignedFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);

#elif FXV_FOGTYPE_SPHERICALPOS

	fogData = _FXV_SphericalFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);

#elif FXV_FOGTYPE_SPHERICALDIST

	fogData = _FXV_SphericalFog_RayDist(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);

#elif FXV_FOGTYPE_INVERTEDSPHERICAL

	fogData = _FXV_InverseSpherical(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);

#elif FXV_FOGTYPE_INVERTEDSPHERICALXHEIGHT

	fogData = _FXV_InverseSpherical(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);

	float fogMin2 = UNITY_ACCESS_INSTANCED_PROP(Props, _SecFogMin);
	float fogMax2 = UNITY_ACCESS_INSTANCED_PROP(Props, _SecFogMax);

	fxvFogData fogData2 = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin2, fogMax2, isOrtho, float3(0, 1, 0));

	fogData.fogT = fogData.fogT * fogData2.fogT;

#elif FXV_FOGTYPE_BOXDIST

	fogData = _FXV_BoxFog_RayDist(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);

#elif FXV_FOGTYPE_BOXPOS

	fogData = _FXV_BoxFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);
	
#elif FXV_FOGTYPE_BOXEXPERIMENTAL

	fogData = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(1, 0, 0));
	fxvFogData fogData1 = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(-1, 0, 0));
	fxvFogData fogData2 = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(0, 0, 1));
	fxvFogData fogData3 = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(0, 0, -1));
	fxvFogData fogData4 = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(0, 1, 0));
	fxvFogData fogData5 = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(0, -1, 0));

	fogData.fogT = smoothstep(0.0, 1.0, fogData.fogT) * smoothstep(0.0, 1.0, fogData1.fogT) * smoothstep(0.0, 1.0, fogData2.fogT) * smoothstep(0.0, 1.0, fogData3.fogT) * smoothstep(0.0, 1.0, fogData4.fogT) * smoothstep(0.0, 1.0, fogData5.fogT);

#elif FXV_FOGTYPE_HEIGHT

	fogData = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(0,1,0));

#elif FXV_FOGTYPE_HEIGHTXBOX

	fogData = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(0, 1, 0));

	float fogMin2 = UNITY_ACCESS_INSTANCED_PROP(Props, _SecFogMin);
	float fogMax2 = UNITY_ACCESS_INSTANCED_PROP(Props, _SecFogMax);

	fxvFogData fogData2 = _FXV_BoxFog_RayDist(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin2, fogMax2, isOrtho);

	fogData.fogT = fogData.fogT * fogData2.fogT;

#elif FXV_FOGTYPE_HEIGHTXVIEW

	fogData = _FXV_HeightFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho, float3(0, 1, 0));

	float fogMin2 = UNITY_ACCESS_INSTANCED_PROP(Props, _SecFogMin);
	float fogMax2 = UNITY_ACCESS_INSTANCED_PROP(Props, _SecFogMax);

	fxvFogData fogData2 = _FXV_ViewAlignedFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin2, fogMax2, isOrtho);

	fogData.fogT = fogData.fogT * fogData2.fogT;

#elif FXV_FOGTYPE_BOXXVIEW

	fogData = _FXV_BoxFog_RayDist(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);

	float fogMin2 = UNITY_ACCESS_INSTANCED_PROP(Props, _SecFogMin);
	float fogMax2 = UNITY_ACCESS_INSTANCED_PROP(Props, _SecFogMax);

	fxvFogData fogData2 = _FXV_ViewAlignedFog(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin2, fogMax2, isOrtho);

	fogData.fogT = fogData.fogT * fogData2.fogT;

#endif



	return fogData;
}

fxvFogData _FXV_CalcVolumetricFog(float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float inputDepth, float4 screenPos)
{
	float isOrtho = unity_OrthoParams.w; // 0 - perspective, 1 - ortho

	float fogMin = UNITY_ACCESS_INSTANCED_PROP(Props, _FogMin);
	float fogMax = UNITY_ACCESS_INSTANCED_PROP(Props, _FogMax);

	float z = _FXV_GetRawDepth(screenPos);

#if FXV_FOG_CLIP_SKYBOX
	if (z <= 0.0)
		discard;
#endif

	float depth = _FXV_GetLinearEyeDepth(z);

#if FXV_FOG_CLIP_BOUNDS
	float3 pDepth = viewDirOriginWS + viewDirWS * depth;
	float3 localDepthPoint = _FXV_WorldToObjectPos(pDepth);

#if FXV_FOGTYPE_SPHERICALPOS ||FXV_FOGTYPE_SPHERICALDIST
	clip(0.5 - length(localDepthPoint));
#else
	clip(float3(0.5, 0.5, 0.5) - abs(localDepthPoint));
#endif
#endif

	float3 worldObjectPos = _FXV_ObjectToWorldPos(float3(0,0,0));

	fxvFogData fogData = _FXV_CalcPrimaryFogData(depth, inputDepth, worldPosition, viewDirOriginWS, viewDirWS, worldObjectPos, fogMin, fogMax, isOrtho);

	float falloff = UNITY_ACCESS_INSTANCED_PROP(Props, _FogFalloff);

#ifdef FXV_SMOOTHED_FALLOFF
	fogData.fogT = smoothstep(0.0, 1.0, fogData.fogT);
#elif FXV_EXP_FALLOFF
	fogData.fogT = exp(fogData.fogT) - 1.0;
#elif FXV_EXP2_FALLOFF
	fogData.fogT = exp2(fogData.fogT) - 1.0;
#endif

	fogData.fogT = pow(fogData.fogT, falloff);

	return fogData;
}

struct fxvLightingData
{
    float3 objectPositionWS;
    float3 pixelPositionWS;
    float4 lightPositionWS;
	float3 lightDirectionWS;
	float3 lightColor;
    float3 viewDirectionOriginWS;
	float3 viewDirectionWS;
	float3 normalWS;
    float2 lightRangeAttenuation; // x - oneOverLightRangeSqr (oneOverFadeRangeSqr on mobile/switch), y - lightRangeSqrOverFadeRangeSqr, linear fade start at 80% light range
	float3 albedo;
    float alpha;
};


float3 _FXV_FogLightingFunction(fxvLightingData fxvData, fxvFogData fogData)
{
	float4 c;

	float NdotL = 1.0;

    half fogMax = UNITY_ACCESS_INSTANCED_PROP(Props, _FogMax); 
    half _scatteringFactor = UNITY_ACCESS_INSTANCED_PROP(Props, _LightScatteringFactor);
    half _invScatteringFactor = 1.0 / _scatteringFactor;
    half _lightReflectivity = UNITY_ACCESS_INSTANCED_PROP(Props, _LightReflectivity);
	half _lightTransmission = UNITY_ACCESS_INSTANCED_PROP(Props, _LightTransmission);

	float3 L = fxvData.lightDirectionWS;
	float3 V = fxvData.viewDirectionWS;
	//float3 N = fxvData.normalWS;

	float invLightRangeSqr = fxvData.lightRangeAttenuation.x;

	float rangeFade = 1.0;
	float lightAtten = 1.0;

	if (fxvData.lightPositionWS.w == 1)
	{
        float3 viewPoint = fxvData.viewDirectionOriginWS - V * length(fxvData.lightPositionWS.xyz - fxvData.viewDirectionOriginWS);
		//float3 viewPoint = _WorldSpaceCameraPos.xyz + V * dot(fxvData.lightPositionWS.xyz - _WorldSpaceCameraPos.xyz, V);//length(fxvData.lightPositionWS.xyz - _WorldSpaceCameraPos.xyz);
		float3 diffToLight = (viewPoint - fxvData.lightPositionWS.xyz);
		float distToViewPointSqr = dot(diffToLight, diffToLight);
		float factor = distToViewPointSqr * invLightRangeSqr;
		rangeFade = saturate(1.0 - factor);

		float3 pixelPosWS = fxvData.pixelPositionWS;
#if FXV_FOGTYPE_VIEWALIGNED || FXV_FOGTYPE_BOXPOS || FXV_FOGTYPE_BOXDIST || FXV_FOGTYPE_BOXEXPERIMENTAL || FXV_FOGTYPE_HEIGHT || FXV_FOGTYPE_HEIGHTXBOX || FXV_FOGTYPE_INVERTEDSPHERICAL || FXV_FOGTYPE_INVERTEDSPHERICALXHEIGHT || FXV_FOGTYPE_HEIGHTXVIEW || FXV_FOGTYPE_BOXXVIEW
#if FXV_IN_AIR_FOG
		pixelPosWS = fogData.pNear; 
#endif
#endif
		diffToLight = (pixelPosWS - fxvData.lightPositionWS.xyz);
		float distanceSqr = dot(diffToLight, diffToLight);

#if FXV_FOGTYPE_SPHERICAL
		factor = distanceSqr * invLightRangeSqr; //TODO handle SHADER_HINT_NICE_QUALITY and mobile as DistanceAttenuation RealtimeLights.hlsl
#else
		float side = dot(V, normalize(diffToLight));
		factor = lerp(distanceSqr, distToViewPointSqr, side) * invLightRangeSqr; //TODO handle SHADER_HINT_NICE_QUALITY and mobile as DistanceAttenuation RealtimeLights.hlsl
#endif
		float attenuationFade = saturate(1.0 - factor);

		lightAtten = pow(attenuationFade, _invScatteringFactor * _invScatteringFactor);

#if FXV_FOGTYPE_VIEWALIGNED || FXV_FOGTYPE_BOXPOS || FXV_FOGTYPE_BOXDIST || FXV_FOGTYPE_BOXEXPERIMENTAL || FXV_FOGTYPE_HEIGHT || FXV_FOGTYPE_HEIGHTXBOX || FXV_FOGTYPE_INVERTEDSPHERICAL || FXV_FOGTYPE_INVERTEDSPHERICALXHEIGHT || FXV_FOGTYPE_HEIGHTXVIEW || FXV_FOGTYPE_BOXXVIEW
#if FXV_IN_AIR_FOG
		L = -normalize(diffToLight); //calculate light dir to box - this is required when we use camera plane that is close to box edge - TODO - optimize so this is only calculated in described situation
#endif
#endif
	}
	else
	{
		rangeFade = saturate(dot(fxvData.lightDirectionWS, float3(0,1,0)));
	}

    c.rgb = fxvData.albedo * fxvData.lightColor.rgb * lightAtten * rangeFade * lerp(fxvData.alpha * min(1.0, _lightReflectivity * 2.0), 1, _lightReflectivity);

	float powFactor = (_invScatteringFactor);
	float VdotH = pow(saturate(dot(V, -L)), powFactor * powFactor);
/*
#if FXV_FOGTYPE_SPHERICAL
   	diffToLight = (fxvData.objectPositionWS - fxvData.lightPositionWS.xyz);
    float l = length(diffToLight);
    diffToLight = (diffToLight / l) * (max(0.0, l-fogMax));
    factor = dot(diffToLight, diffToLight) * invLightRangeSqr;
#else
    float3 boxSizeHalf = UNITY_ACCESS_INSTANCED_PROP(Props, _WorldSize).xyz * 0.5;
    diffToLight = (fxvData.objectPositionWS - fxvData.lightPositionWS.xyz);

    float dist = max(max(abs(diffToLight.x) - boxSizeHalf.x, abs(diffToLight.y) - boxSizeHalf.y), abs(diffToLight.z) - boxSizeHalf.z);
    factor = dist * dist * invLightRangeSqr;
#endif

	float rangeFade3 = saturate(1.0 - factor);
	*/

    c.rgb += ((fxvData.albedo * fxvData.lightColor.rgb * VdotH)) * rangeFade * lerp(fxvData.alpha * min(1.0, _lightTransmission * 2.0), 1, _lightTransmission); // * rangeFade3 

	return c.rgb;
}

float4 _FXV_FogDebug(float4 color, float3 worldPosition, float3 viewDirOriginWS, float3 viewDirWS, float inputDepth, float4 screenPos)
{
    if (_FogDebugMode == 0)
    {
        return color;
    }
    if (_FogDebugMode == 1)
    {
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        return lerp(float4(1, 0, 0, 1), float4(0, 1, 0, 1), unity_StereoEyeIndex);
#else
        return lerp(float4(0, 0, 1, 1), float4(0, 1, 1, 1), unity_StereoEyeIndex);
#endif
    }
    if (_FogDebugMode == 2)
    {
        float z = _FXV_GetRawDepth(screenPos);

        return float4(z, z, z, 1);
    }
    if (_FogDebugMode == 3)
    {
        float z = _FXV_GetRawDepth(screenPos);
        float depth = _FXV_GetLinear01Depth(z);
		
        return float4(depth, depth, depth, 1);
    }
    if (_FogDebugMode == 4)
    {
        float z = _FXV_GetRawDepth(screenPos);
        float depth = _FXV_GetLinearEyeDepth(z);
        float diff = abs(inputDepth - depth) * 0.1;
        float s = step(inputDepth - depth, 0.0);
        return lerp(float4(diff, 0, 0, 1), float4(0, diff, 0, 1), s);
    }
    if (_FogDebugMode == 5)
    {
        return float4(_FXV_WorldToViewPos(worldPosition), 1);
    }
    if (_FogDebugMode == 6)
    {
        return float4(_FXV_WorldToViewDir(viewDirWS), 1);
    }
    if (_FogDebugMode == 7)
    {
        return float4(screenPos.xy / screenPos.w, 0, 1);
    }
    if (_FogDebugMode == 8)
    {
        return float4(UnityStereoTransformScreenSpaceTex(screenPos.xy / screenPos.w), 0, 1);
    }
	
    return color;
}


#endif