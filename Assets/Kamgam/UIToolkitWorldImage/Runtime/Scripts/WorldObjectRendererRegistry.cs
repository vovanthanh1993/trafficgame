using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kamgam.UIToolkitWorldImage
{
    public class WorldObjectRendererRegistry
    {
        public static WorldObjectRendererRegistry Main = new WorldObjectRendererRegistry();

        public List<WorldObjectRenderer> Renderers = new List<WorldObjectRenderer>();

        public void Register(WorldObjectRenderer renderer)
        {
            if (renderer == null)
                return;

            Defrag();

            if (!Renderers.Contains(renderer))
            {
                Renderers.Add(renderer);
            }
        }

        public void Unregister(WorldObjectRenderer renderer)
        {
            Defrag();

            Renderers.Remove(renderer);
        }

        public void Defrag()
        {
            for (int i = Renderers.Count-1; i >= 0; i--)
            {
                if (!isValid(Renderers[i]))
                {
                    Renderers.RemoveAt(i);
                }
            }
        }

        protected bool isValid(WorldObjectRenderer renderer)
        {
            return renderer != null && renderer.gameObject != null;
        }

        public WorldObjectRenderer Find(string id)
        {
            Defrag();

            foreach (var renderer in Renderers)
            {
                if (renderer.Id == id)
                    return renderer;
            }

            return null;
        }
    }
}