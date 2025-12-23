fxvFogData _fxvFogData;
float3 _viewDirWS;
float3 _viewDirOriginWS;

float4 _FXV_GetAdditionalLightPositionWS(uint i)
{
#if USE_CLUSTERED_LIGHTING
    int lightIndex = i;
#elif USE_FORWARD_PLUS
    int lightIndex = i;
#else
    int lightIndex = GetPerObjectLightIndex(i);
#endif

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    return _AdditionalLightsBuffer[lightIndex].position;
#else
    return _AdditionalLightsPosition[lightIndex];
#endif
}

float2 _FXV_GetAdditionalLightRangeAttenuation(uint i)
{
#if USE_CLUSTERED_LIGHTING
    int lightIndex = i;
#elif USE_FORWARD_PLUS
    int lightIndex = i;
#else
    int lightIndex = GetPerObjectLightIndex(i);
#endif

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    return _AdditionalLightsBuffer[lightIndex].attenuation.xy;
#else
    return _AdditionalLightsAttenuation[lightIndex].xy;
#endif
}

half3 _FXV_LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, fxvLightingData fxvData, half3 lightDirectionWS, half lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlightsOff)
{
    float4 c;
    
    half isOrtho = unity_OrthoParams.w;
    
    fxvLightingData fxvLightData = (fxvLightingData)0;
    fxvLightData.pixelPositionWS = fxvData.pixelPositionWS;
    fxvLightData.objectPositionWS = _FXV_ObjectToWorldPos(float3(0,0,0));
    fxvLightData.lightPositionWS = fxvData.lightPositionWS;
    fxvLightData.lightRangeAttenuation = fxvData.lightRangeAttenuation;
	fxvLightData.lightDirectionWS = lightDirectionWS;
	fxvLightData.lightColor = lightColor.rgb;
    fxvLightData.viewDirectionOriginWS = _viewDirOriginWS;
	fxvLightData.viewDirectionWS = viewDirectionWS;
	fxvLightData.normalWS = normalWS;
	fxvLightData.albedo = brdfData.albedo;
    fxvLightData.alpha = fxvData.alpha;

	c.rgb = _FXV_FogLightingFunction(fxvLightData, _fxvFogData);

	return c.rgb;
}

half3 _FXV_LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light, fxvLightingData fxvData, half3 normalWS, half3 viewDirectionWS, half clearCoatMask, bool specularHighlightsOff)
{
    return _FXV_LightingPhysicallyBased(brdfData, brdfDataClearCoat, light.color, fxvData, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, clearCoatMask, specularHighlightsOff);
}

// Backwards compatibility
half3 _FXV_LightingPhysicallyBased(BRDFData brdfData, Light light, fxvLightingData fxvData, half3 normalWS, half3 viewDirectionWS)
{
    #ifdef _SPECULARHIGHLIGHTS_OFF
    bool specularHighlightsOff = true;
#else
    bool specularHighlightsOff = false;
#endif
    const BRDFData noClearCoat = (BRDFData)0;
    return _FXV_LightingPhysicallyBased(brdfData, noClearCoat, light, fxvData, normalWS, viewDirectionWS, 0.0, specularHighlightsOff);
}

half3 _FXV_LightingPhysicallyBased(BRDFData brdfData, half3 lightColor, fxvLightingData fxvData, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS)
{
    Light light;
    light.color = lightColor;
    light.direction = lightDirectionWS;
    light.distanceAttenuation = lightAttenuation;
    light.shadowAttenuation   = 1;
    return _FXV_LightingPhysicallyBased(brdfData, light, fxvData, normalWS, viewDirectionWS);
}

half3 _FXV_LightingPhysicallyBased(BRDFData brdfData, Light light, fxvLightingData fxvData, half3 normalWS, half3 viewDirectionWS, bool specularHighlightsOff)
{
    const BRDFData noClearCoat = (BRDFData)0;
    return _FXV_LightingPhysicallyBased(brdfData, noClearCoat, light, fxvData, normalWS, viewDirectionWS, 0.0, specularHighlightsOff);
}

half3 _FXV_LightingPhysicallyBased(BRDFData brdfData, half3 lightColor, fxvLightingData fxvData, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS, bool specularHighlightsOff)
{
    Light light;
    light.color = lightColor;
    light.direction = lightDirectionWS;
    light.distanceAttenuation = lightAttenuation;
    light.shadowAttenuation   = 1;
    return _FXV_LightingPhysicallyBased(brdfData, light, fxvData, viewDirectionWS, specularHighlightsOff, specularHighlightsOff);
}

half3 _FXV_GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV)
{
    half3 reflectVector = -viewDirectionWS; //reflect(-viewDirectionWS, normalWS);
    half NoV = 1.0f; //saturate(dot(normalWS, viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);

    half3 indirectDiffuse = bakedGI;

#if USE_FORWARD_PLUS
    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, positionWS, brdfData.perceptualRoughness, 1.0h, normalizedScreenSpaceUV);
#else
    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, positionWS, brdfData.perceptualRoughness, 1.0h);
#endif

    half3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);

    if (IsOnlyAOLightingFeatureEnabled())
    {
        color = half3(1,1,1); // "Base white" for AO debug lighting mode
    }

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half3 coatIndirectSpecular = GlossyEnvironmentReflection(reflectVector, positionWS, brdfDataClearCoat.perceptualRoughness, 1.0h);
    // TODO: "grazing term" causes problems on full roughness
    half3 coatColor = EnvironmentBRDFClearCoat(brdfDataClearCoat, clearCoatMask, coatIndirectSpecular, fresnelTerm);

    // Blend with base layer using khronos glTF recommended way using NoV
    // Smooth surface & "ambiguous" lighting
    // NOTE: fresnelTerm (above) is pow4 instead of pow5, but should be ok as blend weight.
    half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * fresnelTerm;
    return (color * (1.0 - coatFresnel * clearCoatMask) + coatColor) * occlusion;
#else
    return color * occlusion;
#endif
}

half3 _FXV_CalculateLightingColor(LightingData lightingData, half3 albedo)
{
    half3 lightingColor = 0;

    if (IsOnlyAOLightingFeatureEnabled())
    {
        return lightingData.giColor; // Contains white + AO
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_GLOBAL_ILLUMINATION))
    {
        lightingColor += lightingData.giColor;
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_MAIN_LIGHT))
    {
        lightingColor += lightingData.mainLightColor;
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_ADDITIONAL_LIGHTS))
    {
        lightingColor += lightingData.additionalLightsColor;
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_VERTEX_LIGHTING))
    {
        lightingColor += lightingData.vertexLightingColor;
    }

    lightingColor *= albedo;

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_EMISSION))
    {
        lightingColor += lightingData.emissionColor;
    }

    return lightingColor;
}

half4 _FXV_CalculateFinalColor(LightingData lightingData, half alpha)
{
    half3 finalColor = _FXV_CalculateLightingColor(lightingData, 1);

    return half4(finalColor, alpha);
}

uint _fxv_GetMeshRenderingLayer()
{
#if UNITY_VERSION < 202220
    return GetMeshRenderingLightLayer(); 
#else
    return GetMeshRenderingLayer();
#endif
}

half4 _FXV_FogFragmentPBR(InputData inputData, SurfaceData surfaceData)
{
    #if defined(_SPECULARHIGHLIGHTS_OFF)
    bool specularHighlightsOff = true;
    #else
    bool specularHighlightsOff = false;
    #endif
    BRDFData brdfData;

    // NOTE: can modify "surfaceData"...
    InitializeBRDFData(surfaceData, brdfData);

    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return debugColor;
    }
    #endif

    // Clear-coat calculation...
    //BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
   // BRDFData brdfData = (BRDFData)0;
   // brdfData.albedo = surfaceData.albedo * surfaceData.alpha;

    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = _fxv_GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
  //  MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    float2 normalizedScreenSpaceUV = 0;
#if USE_FORWARD_PLUS
    normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(inputData.positionCS);
#endif

    lightingData.giColor = _FXV_GlobalIllumination(brdfData, brdfData, surfaceData.clearCoatMask,
                                              inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                              inputData.normalWS, inputData.viewDirectionWS, normalizedScreenSpaceUV);

#if defined(_LIGHT_LAYERS) || (UNITY_VERSION < 202230)
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        fxvLightingData fxvData = (fxvLightingData)0;
        fxvData.alpha = surfaceData.alpha;
        lightingData.mainLightColor = _FXV_LightingPhysicallyBased(brdfData, brdfData, 
                                                              mainLight, fxvData,
                                                              inputData.normalWS, inputData.viewDirectionWS,
                                                              surfaceData.clearCoatMask, specularHighlightsOff);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

 /*   #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

#if defined(_LIGHT_LAYERS) || (UNITY_VERSION < 202230)
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            fxvLightingData fxvData = (fxvLightingData)0;
            fxvData.pixelPositionWS = inputData.positionWS;
            fxvData.objectPositionWS = _FXV_ObjectToWorldPos(float3(0,0,0));
            fxvData.lightPositionWS = _FXV_GetAdditionalLightPositionWS(lightIndex);
            fxvData.lightRangeAttenuation = _FXV_GetAdditionalLightRangeAttenuation(lightIndex);
            fxvData.alpha = surfaceData.alpha;
            lightingData.additionalLightsColor += _FXV_LightingPhysicallyBased(brdfData, brdfData, light, fxvData,
                                                                          inputData.normalWS, inputData.viewDirectionWS, 
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    }
    #endif*/


    #if USE_CLUSTERED_LIGHTING
    for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    
    #if defined(_LIGHT_LAYERS) || (UNITY_VERSION < 202230)
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            fxvLightingData fxvData = (fxvLightingData)0;
            fxvData.pixelPositionWS = inputData.positionWS;
            fxvData.objectPositionWS = _FXV_ObjectToWorldPos(float3(0,0,0));
            fxvData.lightPositionWS = _FXV_GetAdditionalLightPositionWS(lightIndex);
            fxvData.lightRangeAttenuation = _FXV_GetAdditionalLightRangeAttenuation(lightIndex);
            fxvData.alpha = surfaceData.alpha;
            lightingData.additionalLightsColor += _FXV_LightingPhysicallyBased(brdfData, brdfData, light, fxvData,
                                                                          inputData.normalWS, inputData.viewDirectionWS, 
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

    #if defined(_LIGHT_LAYERS) || (UNITY_VERSION < 202230)
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            fxvLightingData fxvData = (fxvLightingData)0;
            fxvData.pixelPositionWS = inputData.positionWS;
            fxvData.objectPositionWS = _FXV_ObjectToWorldPos(float3(0,0,0));
            fxvData.lightPositionWS = _FXV_GetAdditionalLightPositionWS(lightIndex);
            fxvData.lightRangeAttenuation = _FXV_GetAdditionalLightRangeAttenuation(lightIndex);
            fxvData.alpha = surfaceData.alpha;
            lightingData.additionalLightsColor += _FXV_LightingPhysicallyBased(brdfData, brdfData, light, fxvData,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
  //  lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
    #endif

    return _FXV_CalculateFinalColor(lightingData, surfaceData.alpha);
}


void InitializeInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    #ifdef _NORMALMAP
        // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
        float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
        float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

        inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
        #if _NORMAL_DROPOFF_TS
            inputData.normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, inputData.tangentToWorld);
        #elif _NORMAL_DROPOFF_OS
            inputData.normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
        #elif _NORMAL_DROPOFF_WS
            inputData.normalWS = surfaceDescription.NormalWS;
        #endif
    #else
        inputData.normalWS = input.normalWS;
    #endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, input.sh, -inputData.viewDirectionWS/*inputData.normalWS*/);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.sh, -inputData.viewDirectionWS/*inputData.normalWS*/);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.sh;
    #endif
    #endif
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
#if defined(VARYINGS_NEED_DEPTH)
    output.depth = _FXV_ComputeVertexDepth(output.positionWS);
#endif
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if _ALPHATEST_ON
        half alpha = surfaceDescription.Alpha;
        clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
        half alpha = surfaceDescription.Alpha;
    #else
        half alpha = 1;
    #endif

    InputData inputData;
    InitializeInputData(unpacked, surfaceDescription, inputData);
    // TODO: Mip debug modes would require this, open question how to do this on ShaderGraph.
    //SETUP_DEBUG_TEXTURE_DATA(inputData, unpacked.texCoord1.xy, _MainTex);

    #ifdef _SPECULAR_SETUP
        float3 specular = surfaceDescription.Specular;
        float metallic = 1;
    #else
        float3 specular = 0;
        float metallic = surfaceDescription.Metallic;
    #endif

    half3 normalTS = half3(0, 0, 0);
    #if defined(_NORMALMAP) && defined(_NORMAL_DROPOFF_TS)
        normalTS = surfaceDescription.NormalTS;
    #endif

    SurfaceData surface;
    surface.albedo              = half3(0,0,0);
    surface.metallic            = saturate(metallic);
    surface.specular            = specular;
    surface.smoothness          = saturate(surfaceDescription.Smoothness),
    surface.occlusion           = surfaceDescription.Occlusion,
    surface.emission            = surfaceDescription.Emission,
    surface.alpha               = saturate(alpha);
    surface.normalTS            = normalTS;
    surface.clearCoatMask       = 0;
    surface.clearCoatSmoothness = 1;

#if defined(VARYINGS_NEED_DEPTH)
	_FXV_GetViewRayOriginAndDirWS_fromPositionWS(unpacked.positionWS, _viewDirOriginWS, _viewDirWS);
	_fxvFogData = _FXV_CalcVolumetricFog(unpacked.positionWS, _viewDirOriginWS, _viewDirWS, unpacked.depth, unpacked.screenPosition);

    float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

    surface.albedo = color.rgb;
    surface.alpha = _fxvFogData.fogT * color.a;
#endif

    #ifdef _CLEARCOAT
        surface.clearCoatMask       = saturate(surfaceDescription.CoatMask);
        surface.clearCoatSmoothness = saturate(surfaceDescription.CoatSmoothness);
    #endif

#ifdef _DBUFFER
    //ApplyDecalToSurfaceData(unpacked.positionCS, surface, inputData);
#endif

    half4 finalColor = _FXV_FogFragmentPBR(inputData, surface);

    finalColor.rgb = MixFog(finalColor.rgb, inputData.fogCoord);
#if defined(VARYINGS_NEED_DEPTH)
    return half4(finalColor.rgb, surface.alpha);
    //return half4(unpacked.screenPosition.xyz, 1.0);
    //return half4(unpacked.depth, unpacked.depth, unpacked.depth, 1.0);
    //return half4(worldRay, 1.0); 
    //return half4(unpacked.positionWS, fogT);
#else
    return finalColor;
#endif
}
