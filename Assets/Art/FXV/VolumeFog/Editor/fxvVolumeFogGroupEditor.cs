using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FXV
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VolumeFogGroup))]
    public class fxvVolumeFogGroupEditor : UnityEditor.Editor
    {
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

            VolumeFogGroup groupObject = (VolumeFogGroup)target;

        
            EditorGUILayout.Separator();

            GUILayout.Label("Group Properties: ", EditorStyles.boldLabel);

            EditorGUILayout.Separator();

            GUILayout.Label("Enable color control by group. ", wrapLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("controlsColor"));

            if (groupObject.IsControllingColor())
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor"));
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Enable faloff param control by group. ", wrapLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("controlsFalloffParam"));

            if (groupObject.IsControllingFalloffParam())
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("falloffParamMultiplier"));
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Enable ligting control by group. ", wrapLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("controlsLighting"));

            if (groupObject.IsControllingLighting())
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("affectedByLights"));

                if (groupObject.IsAffectedByLights())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lightScatteringFactor"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lightReflectivity"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lightTransmission"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Separator();

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(groupObject);

                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(groupObject.gameObject.scene);

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
