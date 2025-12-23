using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FXV.VolumetricFogEditorUtils
{
    internal class fxvAssetPostprocess : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            foreach (string str in importedAssets)
            {
#if FX_DEBUG_LOGS
                Debug.Log("Reimported Asset: " + str);
#endif
                if (str.Contains("fxvIntroWindow"))
                {
                    fxvIntroWindow.ShowPipelineIntro();
                    return;
                }
            }
        }
    }

    public partial class fxvIntroWindow : EditorWindow
    {
        static string version = "version 1.0.12";

        static int windowWidth = 700;
        static int windowHeight = 400;
        static int buttonWidth = 100;

        [MenuItem("Window/FXV/VolumetricFog/Intro")]
        public static void ShowPipelineIntro()
        {
            var currentRP = GraphicsSettings.currentRenderPipeline;
            if (currentRP == null)
            {
                ShowIntroWindowBuiltIn();
                return;
            }

            var curPipeline = currentRP.GetType().ToString().ToLower();

            if (curPipeline.Contains("universal"))
            {
                ShowIntroWindowURP();
            }
            else if (curPipeline.Contains("high definition") || curPipeline.Contains("highdefinition"))
            {
                ShowIntroWindowHDRP();
            }
        }

        public static void ShowIntroWindowBuiltIn()
        {
#if FX_DEBUG_LOGS
            Debug.Log("FXV.Shield ShowIntroWindow");
#endif
            fxvIntroWindow wnd = GetWindow<fxvIntroWindow>();
            wnd.titleContent = new GUIContent("Welcome Built In");
            wnd.pipelineType = 0;

            wnd.minSize = new Vector2(windowWidth, windowHeight);
            wnd.maxSize = new Vector2(windowWidth, windowHeight);

            wnd.Init();
        }

        public static void ShowIntroWindowURP()
        {
#if FX_DEBUG_LOGS
            Debug.Log("FXV.Shield ShowIntroWindow");
#endif
            fxvIntroWindow wnd = GetWindow<fxvIntroWindow>();
            wnd.titleContent = new GUIContent("Welcome URP");
            wnd.pipelineType = 1;

            wnd.minSize = new Vector2(windowWidth, windowHeight);
            wnd.maxSize = new Vector2(windowWidth, windowHeight);

            wnd.Init();
        }

        public static void ShowIntroWindowHDRP()
        {
#if FX_DEBUG_LOGS
            Debug.Log("FXV.Shield ShowIntroWindow");
#endif
            fxvIntroWindow wnd = GetWindow<fxvIntroWindow>();
            wnd.titleContent = new GUIContent("Welcome HDRP");
            wnd.pipelineType = 2;

            wnd.minSize = new Vector2(windowWidth, windowHeight);
            wnd.maxSize = new Vector2(windowWidth, windowHeight);

            wnd.Init();
        }

        int pipelineType = -1;
        string assetPath;
        bool pipelineSetupDone = false;

        GUIStyle titleStyle;
        GUIStyle greenStyle;
        GUIStyle redStyle;
        Texture2D fxvLogo;

        public void Init()
        {
            var g = AssetDatabase.FindAssets($"t:Script {nameof(fxvIntroWindow)}");

            string scriptPath = null;
            for (int i = 0; i < g.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(g[i]);
                if (p.Contains("fxvIntroWindow.cs"))
                {
                    scriptPath = p;
                    break;
                }
            }

            assetPath = Path.GetDirectoryName(scriptPath);
            assetPath = Path.GetDirectoryName(assetPath);

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = new Color(0.6f, 0.8f, 1.0f, 1.0f);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;

            redStyle = new GUIStyle();
            redStyle.normal.textColor = new Color(0.9f, 0.9f, 0.4f, 1.0f);

            greenStyle = new GUIStyle();
            greenStyle.normal.textColor = new Color(0.4f, 0.9f, 0.4f, 1.0f);

            fxvLogo = (Texture2D)Resources.Load("FXVFogCardImg", typeof(Texture2D));
        }

        public void OnGUI()
        {
            if (assetPath == null || assetPath.Length == 0)
            {
                Init();
            }

            GUILayout.BeginHorizontal();

            GUILayout.Box(fxvLogo);

            GUILayout.BeginVertical();

            GUILayout.Label(version);
            GUILayout.Space(4);
            GUILayout.Label(" Thank you for purchasing \n Fast Volumetric Area Fog asset !!!", titleStyle);

#if UNITY_2021_3_OR_NEWER

#else
            GUILayout.Label("WARRNING - Your unity version (" + Application.unityVersion + ") is older than supported (2021.3.31f1+).\nPlease update to LTS version for maximum compatibility.", redStyle);
#endif

            if (GUILayout.Button("fx.valley.contact@gmail.com", GUILayout.Width(buttonWidth * 2.0f)))
            {
                Application.OpenURL("mailto:fx.valley.contact@gmail.com");
            }

            if (GUILayout.Button("Join Discord", GUILayout.Width(buttonWidth * 2.0f)))
            {
                Application.OpenURL("https://discord.gg/3ssjcBcgpu");
            }

            if (GUILayout.Button("Leave Review on Asset Page", GUILayout.Width(buttonWidth * 2.0f)))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/slug/123912");
            }

            GUILayout.Label("Below you can find configuration tips based on pipeline your project uses.");

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (pipelineType == 0)
            {
                GUIBuiltIn();
            }
            else if (pipelineType == 1)
            {
                GUIURP();
            }
            else if (pipelineType == 2)
            {
                GUIHDRP();
            }
        }

        public static void GUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        void GUIBuiltIn()
        {
            GUILayout.Space(5);

            GUILayout.Space(5);

            bool builtinUnpacked = false;
            if (File.Exists(assetPath + "/BuiltIn/Shaders/FXVVolumeFogLit.shader"))
            {
                builtinUnpacked = true;

                var type = this.GetType();
                var fieldInfo = type.GetField("builtInVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (fieldInfo == null || (string)fieldInfo.GetValue(this) != version)
                {
                    builtinUnpacked = false;
                }
            }

            if (!builtinUnpacked)
            {
                GUILine(Color.gray);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(" Import BuiltIn Render Pipeline fog asset package.", redStyle);

                    if (GUILayout.Button("Show package", GUILayout.Width(buttonWidth)))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/InstallBuiltIn.unitypackage");
                    }

                    if (GUILayout.Button("Import", GUILayout.Width(buttonWidth)))
                    {
                        AssetDatabase.ImportPackage(assetPath + "/InstallBuiltIn.unitypackage", true);
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                var type = this.GetType();

                if (!pipelineSetupDone)
                {
                    var initMethod = type.GetMethod("Setup_BuiltIn_AfterImport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (initMethod != null)
                    {
                        initMethod.Invoke(this, null);
                    }

                    pipelineSetupDone = true;
                }

                var method = type.GetMethod("GUI_BuiltIn_AfterImport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(this, null);
                }
            }
        }

        void GUIURP()
        {
            GUILayout.Space(5);

            bool urpUnpacked = false;
            if (File.Exists(assetPath + "/URP/Shaders/FXVVolumetricFogLitURP.shader"))
            {
                urpUnpacked = true;

                var type = this.GetType();
                var fieldInfo = type.GetField("urpVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (fieldInfo == null || (string)fieldInfo.GetValue(this) != version)
                {
                    urpUnpacked = false;
                }
            }

            if (!urpUnpacked)
            {
                GUILine(Color.gray);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(" Import URP fog asset package.", redStyle);

                    if (GUILayout.Button("Show package", GUILayout.Width(buttonWidth)))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/InstallURP.unitypackage");
                    }

                    if (GUILayout.Button("Import", GUILayout.Width(buttonWidth)))
                    {
                        AssetDatabase.ImportPackage(assetPath + "/InstallURP.unitypackage", true);
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                var type = this.GetType();

                if (!pipelineSetupDone)
                {
                    var initMethod = type.GetMethod("Setup_URP_AfterImport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (initMethod != null)
                    {
                        initMethod.Invoke(this, null);
                    }

                    pipelineSetupDone = true;
                }

                var method = type.GetMethod("GUI_URP_AfterImport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(this, null);
                }
            }
        }

        void GUIHDRP()
        {
            GUILayout.Space(5);

            bool hdrpUnpacked = false;
            if (File.Exists(assetPath + "/HDRP/Shaders/FXVVolumetricFogLitHDRP.shader"))
            {
                hdrpUnpacked = true;
            }

            if (!hdrpUnpacked)
            {
                GUILine(Color.gray);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(" Sorry but HDRP is not supported at the moment.", redStyle);
                    GUILayout.Label(" It's planned to be added in the future update.", redStyle);
                    /*   GUILayout.Label(" Import HDRP fog asset package.", redStyle);

                       if (GUILayout.Button("Show package", GUILayout.Width(buttonWidth)))
                       {
                           Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/InstallHDRP.unitypackage");
                       }

                       if (GUILayout.Button("Import", GUILayout.Width(buttonWidth)))
                       {
                           AssetDatabase.ImportPackage(assetPath + "/InstallHDRP.unitypackage", true);
                       }*/
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                var type = this.GetType();

                if (!pipelineSetupDone)
                {
                    var initMethod = type.GetMethod("Setup_HDRP_AfterImport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (initMethod != null)
                    {
                        initMethod.Invoke(this, null);
                    }

                    pipelineSetupDone = true;
                }

                var method = type.GetMethod("GUI_HDRP_AfterImport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(this, null);
                }
            }
        }
    }
}
