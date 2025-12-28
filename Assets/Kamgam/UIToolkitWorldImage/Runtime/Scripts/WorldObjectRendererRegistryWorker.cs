using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitWorldImage
{
    [ExecuteAlways]
    public class WorldObjectRendererRegistryWorker : MonoBehaviour
    {
        protected WorldObjectRenderer m_renderer;
        public WorldObjectRenderer Renderer
        {
            get
            {
                if (m_renderer == null)
                {
                    m_renderer = this.GetComponent<WorldObjectRenderer>();
                }
                return m_renderer;
            }
        }

        public void OnEnable()
        {
            WorldObjectRendererRegistry.Main.Register(Renderer);
        }

        public void OnDestroy()
        {
            WorldObjectRendererRegistry.Main.Unregister(Renderer);
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (!enabled)
            {
                enabled = true;
                Debug.LogWarning("You must not disable the WorldObjectRendererRegistryWorker. It is vital for the renderer registry to work.");
            }
        }
#endif

    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(WorldObjectRendererRegistryWorker))]
    public class WorldObjectRendererRegistryWorkerEditor : UnityEditor.Editor
    {
        WorldObjectRendererRegistryWorker obj;

        public void OnEnable()
        {
            obj = target as WorldObjectRendererRegistryWorker;
        }

        public override void OnInspectorGUI()
        {
            if (!obj.enabled)
                UnityEditor.EditorGUILayout.HelpBox("Do not disable this component!", UnityEditor.MessageType.Error);
            else
                UnityEditor.EditorGUILayout.HelpBox("This is needed so the WorldImage in the UI Document can find the scene objects. Just ignore it.", UnityEditor.MessageType.Info);
        }
    }
#endif
}