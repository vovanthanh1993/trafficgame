using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitWorldImage
{
    public class WorldImageRegistry
    {
        public static WorldImageRegistry Main = new WorldImageRegistry();

        public List<WorldImage> Images = new List<WorldImage>();

        public void Register(WorldImage image)
        {
            Register(image, registerCallbacks: true);
        }

        public void Register(WorldImage image, bool registerCallbacks)
        {
            if (!Images.Contains(image))
            {
                Images.Add(image);

                if (registerCallbacks)
                {
                    image.RegisterCallback<AttachToPanelEvent>(onAttachToPanel);
                    image.RegisterCallback<DetachFromPanelEvent>(onDetachFromPanel);
                }
            }
        }

        private void onDetachFromPanel(DetachFromPanelEvent evt)
        {
            Unregister(evt.target as WorldImage);
        }

        private void onAttachToPanel(AttachToPanelEvent evt)
        {
            Register(evt.target as WorldImage, registerCallbacks: false);

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var image = evt.target as WorldImage;
                image.UpdateBackgroundTexture();
            }
#endif
        }

        public void Unregister(WorldImage image)
        {
            Images.Remove(image);
        }

        public void Defrag()
        {
            for (int i = Images.Count - 1; i >= 0; i--)
            {
                if (!isValid(Images[i]))
                {
                    Images.RemoveAt(i); 
                }
            }
        }

        protected bool isValid(WorldImage image)
        {
            return image != null && image.panel != null; 
        }

        public WorldImage Find(string rendererId)
        {
            foreach (var image in Images)
            {
                if (image.RendererId == rendererId)
                    return image;
            }

            return null;
        }

        public WorldImage FindInGameView(string rendererId)
        {
            foreach (var image in Images)
            {
                if (image.panel != null && image.panel.contextType == UnityEngine.UIElements.ContextType.Player && image.RendererId == rendererId)
                    return image;
            }

            return null;
        }

        public void UpdateTexture(string id)
        {
            foreach (var image in Images) 
            {
                if (image.RendererId == id)
                {
                    image.UpdateBackgroundTexture();
                }
            }
        }

        public void MarkDirtyRepaint(string id)
        {
            foreach (var image in Images)
            {
                if (image.RendererId == id)
                {
                    image.MarkDirtyRepaint();
                }
            }
        }

        public void UpdateObjectRendererActiveState(string id)
        {
            // Use the first found to trigger the check.
            // In the first run only used images that are drawn.
            WorldImage checkImg = null;
            foreach (var image in Images)
            {
                if (image.RendererId == id && image.IsDrawn())
                {
                    checkImg = image;
                    break;
                }
            }

            // If none was found then try again but this time use
            // any image that matches the renderer id.
            if(checkImg == null)
            {
                foreach (var image in Images)
                {
                    if (image.RendererId == id)
                    {
                        checkImg = image;
                        break;
                    }
                }
            }

            // It doesn't matter which image is used because the
            // check will iterate over all images anyways.
            if(checkImg != null)
                checkImg.UpdateObjectRendererActiveState();
        }
    }
}