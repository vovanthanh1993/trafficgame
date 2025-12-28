#if UNITY_EDITOR
using UnityEditor;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kamgam.UIToolkitWorldImage
{
    [UnityEditor.CustomEditor(typeof(WorldObjectRenderer))]
    public class WorldObjectRendererEditor : UnityEditor.Editor
    {
        WorldObjectRenderer m_renderer;

        SerializedProperty m_useRenderTextureProp;
        int m_lastCameraCullingMask = -99;
        bool m_debugFoldout = false;
        bool m_texturePreviewFoldout = true;

        public void OnEnable()
        {
            m_useRenderTextureProp = serializedObject.FindProperty("m_useRenderTexture");
            m_renderer = target as WorldObjectRenderer;
            Undo.undoRedoPerformed += onUndoRedo;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= onUndoRedo;
        }

        private void onUndoRedo()
        {
            m_renderer?.ForceRenderTextureUpdate();
        }

        public override void OnInspectorGUI()
        {
            var oldId = m_renderer.Id;
            var oldFieldOfView = m_renderer.CameraFieldOfView;
            var oldBackgroundColor = m_renderer.CameraBackgroundColor;
            var oldCameraClearType = m_renderer.CameraClearType;
            var oldDepth = m_renderer.CameraDepth;
            var oldResolutionWidth = m_renderer.ResolutionWidth;
            var oldResolutionHeight = m_renderer.ResolutionHeight;
            var oldCameraUseBoundsToClip = m_renderer.CameraUseBoundsToClip;
            var oldCameraLookAtPosition = m_renderer.CameraLookAtPosition;
            var oldCameraOffset = m_renderer.CameraOffset;
            var oldUseRenderTexture = m_useRenderTextureProp != null ? m_useRenderTextureProp.boolValue : m_renderer.UseRenderTexture;
            var oldRenderTextureOverride = m_renderer.RenderTextureOverride;
            var oldFirstWorldObject = m_renderer.GetWorldObjectAt(0);
            var oldNearClipValue = m_renderer.CameraNearClipPlane;
            var oldFarClipValue = m_renderer.CameraFarClipPlane;
            var oldCameraOffsetAndPositionMultiplier = m_renderer.CameraOffsetAndPositionMultiplier;

            GUI.enabled = m_renderer.UseRenderTexture;
            m_texturePreviewFoldout = EditorGUILayout.Foldout(m_texturePreviewFoldout, "Render Texture Preview" + (m_renderer.UseRenderTexture ? "" : " (not used)") );
            if (m_texturePreviewFoldout && m_renderer.IsActive && m_renderer.UseRenderTexture && m_renderer.RenderTexture != null)
            {
                //var rect = GUILayoutUtility.GetLastRect();
                var rect = new Rect();
                EditorGUI.DrawPreviewTexture(new Rect(rect.xMin + 10, rect.yMax + 30, EditorGUIUtility.currentViewWidth * 0.5f - 30, EditorGUIUtility.currentViewWidth * 0.5f - 30), m_renderer.RenderTexture);
                EditorGUI.DrawTextureAlpha(new Rect(rect.xMin + 10 + EditorGUIUtility.currentViewWidth * 0.5f - 30, rect.yMax + 30, EditorGUIUtility.currentViewWidth * 0.5f - 30, EditorGUIUtility.currentViewWidth * 0.5f - 30), m_renderer.RenderTexture);
                GUILayout.Space(EditorGUIUtility.currentViewWidth * 0.5f - 20);

                // Force a redraw
                Repaint();
            }
            GUI.enabled = true;

            base.OnInspectorGUI();

#if KAMGAM_RENDER_PIPELINE_URP || KAMGAM_RENDER_PIPELINE_HDRP
            if (!m_renderer.UseRenderTexture)
                EditorGUILayout.HelpBox("In URP and HDRP transparent backgrounds are not supported for camera stacking.", MessageType.Info);
#endif

            serializedObject.ApplyModifiedProperties();

            if (!m_renderer.TryGetComponent<PrefabInstantiatorForWorldObjectRenderer>(out var instantiator))
            {
                GUILayout.Space(10);
                GUILayout.Label("Prefabs");
                if (GUILayout.Button("Add Prefab Instantiator"))
                {
                    m_renderer.gameObject.AddComponent<PrefabInstantiatorForWorldObjectRenderer>();
                }
            }

            // Debugging
            GUILayout.Space(10);
            m_debugFoldout = EditorGUILayout.Foldout(m_debugFoldout, "Debug");
            if (m_debugFoldout)
            {

                if (GUILayout.Button("Force Render Texture Update"))
                {
                    m_renderer.ForceRenderTextureUpdate();
                }

                if (GUILayout.Button("Update Bounds"))
                {
                    m_renderer.UpdateWorldObjectBounds();
                }

                if (GUILayout.Button("Update Active State"))
                {
                    WorldImageRegistry.Main.UpdateObjectRendererActiveState(m_renderer.Id);
                }

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Instance ID: " + m_renderer.GetInstanceID());
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (m_renderer.HasTmpRenderTexture)
                    GUILayout.Label("RT: " + m_renderer.RenderTexture.name);
                else
                    GUILayout.Label("RT: NULL");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (m_renderer.CameraFollowBoundsCenter && m_renderer.HasBounds)
                    GUILayout.Label("Following: Bounds center");
                else if (m_renderer.HasActiveWorldObjects())
                    GUILayout.Label("Following: " + m_renderer.GetWorldObjectAt(0).name);
                else
                    GUILayout.Label("Following: none (using LookAt as poistion)");
                GUILayout.EndHorizontal();
            }

            bool didChange = false;

            if (oldId != m_renderer.Id)
            {
                m_renderer.ApplyId();
                didChange = true;
            }

            if (oldFieldOfView != m_renderer.CameraFieldOfView)
            {
                m_renderer.ApplyCameraFieldOfView();
                didChange = true;
            }

            if (oldFieldOfView != m_renderer.CameraFieldOfView)
            {
                m_renderer.ApplyCameraFieldOfView();
                didChange = true;
            }

            if (oldBackgroundColor != m_renderer.CameraBackgroundColor)
            {
                m_renderer.ApplyCameraBackgroundColor();
                didChange = true;
            }
            
            if (oldCameraClearType != m_renderer.CameraClearType)
            {
                m_renderer.ApplyCameraClearType();
                didChange = true;
            }

            if (oldCameraLookAtPosition != m_renderer.CameraLookAtPosition)
            {
                m_renderer.ApplyCameraLookAtPosition();
                didChange = true;
            }

            if (oldCameraOffset != m_renderer.CameraOffset)
            {
                m_renderer.ApplyCameraOffset();
                didChange = true;
            }

            if (oldDepth != m_renderer.CameraDepth)
            {
                m_renderer.ApplyCameraDepth();
                didChange = true;
            }

            if (m_lastCameraCullingMask != m_renderer.CameraCullingMask)
            {
                m_renderer.ApplyCameraCullingMask();
                didChange = true;
            }

            if (oldResolutionWidth != m_renderer.ResolutionWidth)
            {
                m_renderer.ApplyResolutionWidth();
                didChange = true;
            }

            if (oldResolutionHeight != m_renderer.ResolutionHeight)
            {
                m_renderer.ApplyResolutionHeight();
                didChange = true;
            }

            if (oldCameraUseBoundsToClip != m_renderer.CameraUseBoundsToClip)
            {
                m_renderer.UpdateWorldObjectBounds();
                m_renderer.ObjectCamera.UpdateCameraClippingFromBounds();
                didChange = true;
            }

            if (m_useRenderTextureProp != null)
            {
                if (oldUseRenderTexture != m_useRenderTextureProp.boolValue)
                {
                    m_renderer.ApplyUseRenderTexture();
                    didChange = true;
                }
            }
            
            if (oldRenderTextureOverride != m_renderer.RenderTextureOverride)
            {
                m_renderer.ForceRenderTextureUpdate();
                didChange = true;
            }

            if (oldFirstWorldObject != m_renderer.GetWorldObjectAt(0))
            {
                m_renderer.UpdateWorldObjectBounds();
                didChange = true;
            }

            if (oldNearClipValue != m_renderer.CameraNearClipPlane)
            {
                m_renderer.ApplyCameraNearClipPlane();
                didChange = true;
            }

            if (oldFarClipValue != m_renderer.CameraFarClipPlane)
            {
                m_renderer.ApplyCameraFarClipPlane();
                didChange = true;
            }

            if (!Mathf.Approximately(oldCameraOffsetAndPositionMultiplier, m_renderer.CameraOffsetAndPositionMultiplier))
            {
                m_renderer.ApplyCameraOffsetAndPositionMultiplier(oldCameraOffsetAndPositionMultiplier);
                didChange = true;
            }

            if (didChange)
            {
                m_renderer.UpdateCameraTransform();
                m_renderer.UpdateWorldObjectBounds();
                WorldImageRegistry.Main.MarkDirtyRepaint(m_renderer.Id);
            }

            serializedObject.Update();

            m_lastCameraCullingMask = m_renderer.CameraCullingMask;
        }
    }
}
#endif