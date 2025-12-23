using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FXV
{
    [ExecuteInEditMode]
    public class VolumeFogManager
    {
        public static VolumeFogManager instance = null;

        static Camera currentCamera = null;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        static void CreateInstance()
        {
            instance = new VolumeFogManager();

            RenderPipelineManager.beginCameraRendering -= OnBeginCamRender;

            if (Internal.fxvFogAssetConfig.ActiveRenderPipeline != Internal.fxvFogAssetConfig.Pipeline.BuiltIn)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCamRender;
            }
        }

        static void OnBeginCamRender(ScriptableRenderContext context, Camera camera)
        {
            currentCamera = camera;
        }

        public Camera GetCurrentRenderingCamera()
        {
            if (Internal.fxvFogAssetConfig.ActiveRenderPipeline != Internal.fxvFogAssetConfig.Pipeline.BuiltIn)
            {
                return currentCamera;
            }

            return Camera.current;
        }
    }
}
