using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitWorldImage
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;

#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class WorldImage : VisualElement
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string s_ussClassName = "kamgam-world-image";

#if !UNITY_6000_0_OR_NEWER
        [UnityEngine.Scripting.Preserve]
        public new class UxmlFactory : UxmlFactory<WorldImage, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_rendererId =
                new UxmlStringAttributeDescription { name = "renderer-id", defaultValue = "" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                var image = ve as WorldImage;
                if (image == null)
                    return;

                base.Init(ve, bag, cc);

                image.RendererId = m_rendererId.GetValueFromBag(bag, cc);

                image.UpdateBackgroundTexture();
            }
        }
#endif

        /// <summary>
        /// The id of the world object renderer this image should be bound to.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("renderer-id")]
#endif
        public string RendererId { get; set; } = "";

        public WorldImage()
        {
            AddToClassList(s_ussClassName);

            // Register callbacks
            RegisterCallback<AttachToPanelEvent>(onAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(onDetachFromPanel);
            RegisterCallback<GeometryChangedEvent>(onGeometryChange);

            WorldImageRegistry.Main.Register(this);
        }

        Nullable<bool> m_lastIsDrawnValue;

        protected virtual void onGeometryChange(GeometryChangedEvent evt)
        {
            bool isDrawn = IsDrawn();

            if (!m_lastIsDrawnValue.HasValue || m_lastIsDrawnValue.Value != isDrawn)
            {
                m_lastIsDrawnValue = isDrawn;

                schedule.Execute(UpdateObjectRendererActiveState);

                if (isDrawn)
                {
                    schedule.Execute(UpdateBackgroundTexture);
                }
            }
        }

        /// <summary>
        /// Returns whether or not the image is drawn based on the contentRect size.<br />
        /// It acts like the activeInHierarchy we are used to from game objects (takes parents into account).
        /// </summary>
        public bool IsDrawn()
        {
            var rect = contentRect;
            return !float.IsNaN(rect.width) && !Mathf.Approximately(rect.width, 0f) && !Mathf.Approximately(rect.height, 0f);
        }

        protected virtual void onAttachToPanel(AttachToPanelEvent evt)
        {
            //UpdateObjectRendererActiveState();
        }

        protected virtual void onDetachFromPanel(DetachFromPanelEvent evt)
        {
            //UpdateObjectRendererActiveState();
        }

        public void UpdateObjectRendererActiveState()
        {
            // Count all the images that are visible.
            int visibleImages = 0;
            foreach (var image in WorldImageRegistry.Main.Images)
            {
                if (   image.RendererId == RendererId 
                    && image.panel != null
#if UNITY_EDITOR
                    // During play mode the display state of the images can differ between UI Builder and Player Window.
                    && (
                           (UnityEditor.EditorApplication.isPlaying && image.panel.contextType == ContextType.Player)
                        || (!UnityEditor.EditorApplication.isPlaying) // We don't care about the panel context while editing
                       )
#endif
                    && image.IsDrawn())
                {
                    visibleImages++;
                }
            }
            
            var renderer = WorldObjectRendererRegistry.Main.Find(RendererId);
            if (renderer != null)
            {
                renderer.SetActive(visibleImages > 0);
            }
        }

        public void UpdateBackgroundTexture()
        {
            var renderTexture = GetRenderTexture();
            if (renderTexture == null)
            {
                style.backgroundImage = null;
            }
            else
            {
                style.backgroundImage = new StyleBackground(Background.FromRenderTexture(GetRenderTexture()));
            }
        }

        public RenderTexture GetRenderTexture()
        {
            var renderer = WorldObjectRendererRegistry.Main.Find(RendererId);
            if (renderer != null && renderer.IsActive)
                return renderer.RenderTexture;
            else
                return null;
        }

        public WorldObjectRenderer GetWorldObjectRenderer()
        {
            return WorldObjectRendererRegistry.Main.Find(RendererId);
        }

        public PrefabInstantiatorForWorldObjectRenderer GetPrefabInstantiator()
        {
            return GetWorldObjectRenderer().PrefabInstantiator;
        }
    }
}