using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FXV
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VolumeFog))]
    public class fxvVolumeFogEditor : UnityEditor.Editor
    {

        [MenuItem("GameObject/FXV/Fog - Spherical", false, 0)]
        internal static void CreateSphericalFog(MenuCommand menuCommand)
        {
            GameObject selected = Selection.activeObject as GameObject;

            GameObject go = new GameObject("SphericalFog");

            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            VolumeFog fog = go.AddComponent<VolumeFog>();
            fog.FogMax = 1.0f;
            VolumeFog.SetFogType(fog, VolumeFog.FogType.SphericalPos);

            GameObjectUtility.SetParentAndAlign(go, selected);

            Undo.RegisterCreatedObjectUndo(go, "Created Fog " + go.name);
        }

        [MenuItem("GameObject/FXV/Fog - Height", false, 0)]
        internal static void CreateHeightFog(MenuCommand menuCommand)
        {
            GameObject selected = Selection.activeObject as GameObject;

            GameObject go = new GameObject("HeightFog");

            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            VolumeFog fog = go.AddComponent<VolumeFog>();
            fog.SetFogBoxSize(new Vector3(10.0f, 1.0f, 10.0f));
            fog.FogMax = 1.0f;
            VolumeFog.SetFogType(fog, VolumeFog.FogType.Height);

            GameObjectUtility.SetParentAndAlign(go, selected);

            Undo.RegisterCreatedObjectUndo(go, "Created Fog " + go.name);
        }

        [MenuItem("GameObject/FXV/Fog - Box", false, 0)]
        internal static void CreateBoxFog(MenuCommand menuCommand)
        {
            GameObject selected = Selection.activeObject as GameObject;

            GameObject go = new GameObject("BoxFog");

            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            VolumeFog fog = go.AddComponent<VolumeFog>();
            fog.SetFogBoxSize(new Vector3(1.0f, 1.0f, 1.0f));
            fog.FogMax = 0.5f;
            VolumeFog.SetFogType(fog, VolumeFog.FogType.BoxDist);

            GameObjectUtility.SetParentAndAlign(go, selected);

            Undo.RegisterCreatedObjectUndo(go, "Created Fog " + go.name);
        }

        [MenuItem("GameObject/FXV/Fog - View Aligned", false, 0)]
        internal static void CreateBasicFog(MenuCommand menuCommand)
        {
            GameObject selected = Selection.activeObject as GameObject;

            GameObject go = new GameObject("ViewAlignedFog");

            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            VolumeFog fog = go.AddComponent<VolumeFog>();
            fog.SetFogBoxSize(new Vector3(1.0f, 1.0f, 1.0f));
            VolumeFog.SetFogType(fog, VolumeFog.FogType.ViewAligned);

            GameObjectUtility.SetParentAndAlign(go, selected);

            Undo.RegisterCreatedObjectUndo(go, "Created Fog " + go.name);
        }

        public static int DrawSortingLayersPopup(int layerID)
        {
            var layers = SortingLayer.layers;
            var names = layers.Select(l => l.name).ToArray();

            if (!SortingLayer.IsValid(layerID))
            {
                layerID = layers[0].id;
            }

            var layerValue = SortingLayer.GetLayerValueFromID(layerID);

            var newLayerValue = EditorGUILayout.Popup("Sorting Layer", layerValue, names);

            return layers[newLayerValue].id;
        }

        public override void OnInspectorGUI()
        {
            GUIStyle wrapLabel = new GUIStyle(EditorStyles.boldLabel);
            wrapLabel.wordWrap = true;

            GUIStyle wrapLabelRed = new GUIStyle(EditorStyles.boldLabel);
            wrapLabelRed.wordWrap = true;
            wrapLabelRed.normal.textColor = new Color(0.8f, 0.2f, 0.0f);

            GUIStyle groupControlledLabel = new GUIStyle(EditorStyles.label);
            groupControlledLabel.wordWrap = true;
            groupControlledLabel.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            VolumeFog fogObject = (VolumeFog)target;

            VolumeFogGroup fogGroup = fogObject.GetParentFogGroup();

            for (int i = 0; i < targets.Length; i++)
            {
                VolumeFog.SetupFogMaterial((VolumeFog)targets[i]); 
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Fog Properties: ", EditorStyles.boldLabel);

            VolumeFog.FogType oldType = fogObject.GetFogType();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogType"));

            if (fogObject.IsUsingSecondaryParams())
            {
                string[] names = fogObject.GetFogType().ToString().Split('X');

                string prefix1 = "[" + names[0] + "] ";
                string prefix2 = "[" + names[1] + "] ";

                if (fogObject.IsUsingDepthWorkflow())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogMax"), new GUIContent(prefix1 + "Fog Max"));

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_fogDepth"), new GUIContent(prefix1 + "Fog Depth"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogMin"), new GUIContent(prefix1 + "Fog Min"));

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogMax"), new GUIContent(prefix1 + "Fog Max"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("secFogMin"), new GUIContent(prefix2 + "Fog Min"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("secFogMax"), new GUIContent(prefix2 + "Fog Max"));
            }
            else
            {
                if (fogObject.IsUsingDepthWorkflow())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogMax"));

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_fogDepth"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogMin"));

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogMax"));
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("blendType"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogMeshType"));

            if (fogObject.IsCustomMesh())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customMesh"));
            }
            else
            {
                if (fogObject.IsBoxShape())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("worldSize"), new GUIContent("Fog Box Size"));
                }
            }

            if (fogGroup && fogGroup.IsControllingColor())
            {
                GUILayout.Label(" - Color controlled by parent group.", groupControlledLabel);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogFalloffType"), new GUIContent("Fog Falloff Type"));

            if (fogGroup && fogGroup.IsControllingFalloffParam())
            {
                GUILayout.Label(" - Falloff param is multiplied by parent group.", groupControlledLabel);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogFalloffCurve"), new GUIContent("Fog Falloff Param"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fogClipping"), new GUIContent("Fog Clipping Mode"));

            {
                EditorGUILayout.Separator();

                GUILayout.Label("Default render mode will fit all camera scenarios, while Simplified is faster but will not render properly from every camera view", wrapLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("renderMode"), new GUIContent("Fog Render Mode"));
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Switch between Lit/Unlit version. ", wrapLabel);


            if (fogGroup && fogGroup.IsControllingLighting())
            {
                GUILayout.Label(" - Lighting params controlled by parent group.", groupControlledLabel);
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("affectedByLights"));

                if (fogObject.IsAffectedByLights())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lightScatteringFactor"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lightReflectivity"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lightTransmission"));
                }
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Rendering properties. ", wrapLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("sortingLayer"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sortingOrder"));


            EditorGUILayout.Separator();

#if FXV_VOLUMEFOG_DEBUG
            GUILayout.Label("Debug options. ", wrapLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("debugMode"));
#endif

            //  GUILayout.Label("Render fog after transparent objects (> 3000): ", wrapLabel);

            //  fogObject.renderQueue = EditorGUILayout.IntField("Render Queue", fogObject.renderQueue);

            if (GUI.changed || oldType != fogObject.GetFogType())
            {
                serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(fogObject);

                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(fogObject.gameObject.scene);

                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null)
                    {
                        EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                    }
                }
            }
        }
    }
}