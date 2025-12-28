#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kamgam.UIToolkitWorldImage
{
    [UnityEditor.CustomEditor(typeof(PrefabInstantiatorForWorldObjectRenderer))]
    public class PrefabInstantiatorForWorldObjectRendererEditor : UnityEditor.Editor
    {
        PrefabInstantiatorForWorldObjectRenderer m_instantiator;
        bool m_createButtonsFoldout = true;

        public void OnEnable()
        {
            m_instantiator = target as PrefabInstantiatorForWorldObjectRenderer;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Create or Update ALL Instances"))
            {
                m_instantiator.CreateAndAddAllInstancesToImage(m_instantiator.WorldObjectRenderer);
            }

            if (GUILayout.Button("Destroy ALL Instances"))
            {
                m_instantiator.RemoveAllFromImageAndDestroyInstances(m_instantiator.WorldObjectRenderer);
            }

            m_createButtonsFoldout = EditorGUILayout.Foldout(m_createButtonsFoldout, new GUIContent("Toggle Instances"));
            if (m_createButtonsFoldout)
            {
                foreach (var prefabHandle in m_instantiator.Prefabs)
                {
                    if (prefabHandle.Prefab == null)
                        continue;

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Toggle " + prefabHandle.Prefab.name))
                    {
                        m_instantiator.ToogleOrCreate(prefabHandle, destroyOnDisable: true);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}
#endif