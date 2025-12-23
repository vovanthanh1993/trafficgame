using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FXV.VolumetricFogEditorUtils
{
    public partial class fxvIntroWindow : EditorWindow
    {
#pragma warning disable CS0414
        static string urpVersion = "version 1.0.12";
#pragma warning restore CS0414

        void Setup_URP_AfterImport()
        {
            FXV.Internal.fxvFogAssetConfig.UpdateShadersForActiveRenderPipeline();
        }

        void GUI_URP_AfterImport()
        {
            UniversalRenderPipelineAsset currentRP = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;

            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            { 
                if (currentRP.supportsCameraDepthTexture)
                {
                    GUILayout.Label("  Depth texture enabled.", greenStyle);
                }
                else
                {
                    GUILayout.Label(" Enable depth texture in pipeline asset for fog to work.\n Alternatively you can do this for specific camera in it's component on scene.", redStyle);
                    if (GUILayout.Button("Show Asset", GUILayout.Width(buttonWidth)))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(currentRP));
                    }


                    if (GUILayout.Button("Fix", GUILayout.Width(buttonWidth)))
                    {
                        currentRP.supportsCameraDepthTexture = true;
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Check demo scenes for fog examples.");

                if (GUILayout.Button("Show", GUILayout.Width(buttonWidth)))
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/URP/Demo/Demo1_URP.unity");
                }
            }
            GUILayout.EndHorizontal();

            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("To quickly add fog to scene [RightClick -> FXV -> Fog (type)] in Hierarchy panel.");
            }
            GUILayout.EndHorizontal();

            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("When adding big fog groups consider parenting them under object with VolumeFogGroup component.");

                if (GUILayout.Button("Show", GUILayout.Width(buttonWidth)))
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/Scripts/VolumeFogGroup.cs");
                }
            }
            GUILayout.EndHorizontal();

            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Please read documentation for implementation guidelines.");

                if (GUILayout.Button("Show", GUILayout.Width(buttonWidth)))
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/Documentation.pdf");
                }
            }
            GUILayout.EndHorizontal();
            GUILine(Color.gray);
        }
    }
}