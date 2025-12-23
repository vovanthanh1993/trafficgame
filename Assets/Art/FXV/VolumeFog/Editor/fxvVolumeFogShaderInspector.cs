using UnityEngine;
using UnityEditor;
using System;

namespace FXV
{
    public class fxvVolumeFogShaderInspector : ShaderGUI
    {


        static class Styles
        {
            static public readonly GUIContent albedo = new GUIContent("Albedo", "Albedo (RGB)");
            static public readonly GUIContent alphaCutoffText = EditorGUIUtility.TrTextContent("Alpha Cutoff", "Threshold for alpha cutoff");
            static public readonly GUIContent normalMap = new GUIContent("Normal Map", "Normal Map");
            static public readonly GUIContent rmaMap = new GUIContent("RMA Map", "RMA Map");
            static public readonly GUIContent occlusion = new GUIContent("Occlusion", "Occlusion (G)");
            static public readonly GUIContent detail = new GUIContent("Triplanar Detail", "Detail");
            static public readonly GUIContent detailNormalMap = new GUIContent("Detail Normal Map", "Detail Normal Map");
            static public readonly GUIContent intersection = new GUIContent("Intersection Map", "Intersection");
            static public readonly GUIContent bakeItMainMap = new GUIContent("BakeIt Main Map");
            static public readonly GUIContent channelTexture = new GUIContent("Channel Texture", "Color");
            static public readonly GUIContent channelRamp = new GUIContent("Channel Ramp");

            public static string renderingMode = "Rendering Mode";
            public static readonly string[] fogTypeNames = Enum.GetNames(typeof(VolumeFog.FogType));
            public static readonly string[] blendNames = Enum.GetNames(typeof(VolumeFog.FogBlendMode));
        }

        bool _initialized;

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            Material material = editor.target as Material;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shader Params:", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            //-----------------------------------------------------------------------------------------------
            // FOG TYPE
            //-----------------------------------------------------------------------------------------------

            MaterialProperty fogType = FindProperty("_FogType", props);

            FogTypePopup(editor, fogType);

            var fogTypeVal = (VolumeFog.FogType)fogType.floatValue;

            //-----------------------------------------------------------------------------------------------s
            // BLEND MODE
            //-----------------------------------------------------------------------------------------------

            MaterialProperty blendMode = FindProperty("_BlendMode", props);

            BlendModePopup(editor, blendMode);

            //-----------------------------------------------------------------------------------------------
            // IN AIR RENDERING
            //-----------------------------------------------------------------------------------------------

            editor.ShaderProperty(FindProperty("_InAirEnabled", props), "Volumetric Rendering Enabled");


            //-----------------------------------------------------------------------------------------------
            // AXIS FADE
            //-----------------------------------------------------------------------------------------------

            if (fogTypeVal == VolumeFog.FogType.Height)
            {
                editor.ShaderProperty(FindProperty("_AxisFadeEnabled", props), "Axis Fade Enabled");
            }

            //-----------------------------------------------------------------------------------------------
            // ADVANCED
            //-----------------------------------------------------------------------------------------------

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label("Advanced Options", EditorStyles.boldLabel);

            editor.EnableInstancingField();
            editor.RenderQueueField();

            if (EditorGUI.EndChangeCheck() || !_initialized)
            {
                foreach (Material m in editor.targets)
                {
                    SetupMaterialWithFogType(m, (VolumeFog.FogType)m.GetFloat("_FogType"));
                    SetupMaterialWithBlendMode(m, (VolumeFog.FogBlendMode)m.GetFloat("_BlendMode"));

                    SetMaterialKeywords(m);
                }
            }

            _initialized = true;
        }

        void FogTypePopup(MaterialEditor editor, MaterialProperty fogType)
        {
            EditorGUI.showMixedValue = fogType.hasMixedValue;
            var mode = (VolumeFog.FogType)fogType.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (VolumeFog.FogType)EditorGUILayout.Popup("Fog Type", (int)mode, Styles.fogTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo("Fog Type");
                fogType.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        void BlendModePopup(MaterialEditor editor, MaterialProperty blendMode)
        {
            EditorGUI.showMixedValue = blendMode.hasMixedValue;
            var mode = (VolumeFog.FogBlendMode)blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (VolumeFog.FogBlendMode)EditorGUILayout.Popup("Blend Mode", (int)mode, Styles.blendNames);
            if (EditorGUI.EndChangeCheck())
            {
                editor.RegisterPropertyChangeUndo("Blend Mode");
                blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        public static void SetupMaterialWithFogType(Material material, VolumeFog.FogType fogType)
        {
            var types = Enum.GetNames(typeof(VolumeFog.FogType));
            foreach (string name in types)
            {
                material.DisableKeyword("FXV_FOGTYPE_" + name.ToUpper());
            }

            material.EnableKeyword("FXV_FOGTYPE_" + fogType.ToString().ToUpper());
        }

        public static void SetupMaterialWithBlendMode(Material material, VolumeFog.FogBlendMode blendMode)
        {

            if (blendMode == VolumeFog.FogBlendMode.AlphaBlend)
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
            }
            else if (blendMode == VolumeFog.FogBlendMode.Add)
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_ZWrite", 0);
            }
        }

        static void SetMaterialKeywords(Material material)
        {
            SetKeyword(material, "FXV_IN_AIR_FOG", material.GetInt("_InAirEnabled") > 0);
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
}