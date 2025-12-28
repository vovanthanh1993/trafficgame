#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kamgam.UIToolkitWorldImage
{
    public partial class WorldObjectRenderer
    {
        [MenuItem("GameObject/UI Toolkit/World Object Renderer", false, 2009)]
        public static void AddRendererToSelection()
        {
            WorldObjectRenderer renderer = Create(Selection.activeGameObject);
            Selection.objects = new GameObject[] { renderer.gameObject };
        }

        /// <summary>
        /// Use this to create a WorldImageRenderer via code.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static WorldObjectRenderer Create(GameObject parent)
        {
            // Create one for reach selected game object.
            var go = new GameObject("World Object Renderer");
            go.SetActive(false);

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            go.AddComponent<WorldObjectRendererRegistryWorker>();
            WorldObjectRenderer renderer = go.AddComponent<WorldObjectRenderer>();
            if (Selection.activeGameObject == null)
                renderer.Id = "r" + Random.Range(1, 999999);
            else
                renderer.Id = santitizeName(Selection.activeGameObject.name);

            go.SetActive(true);

            return renderer;
        }

        static string santitizeName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(name.Replace(" ", "-"), @"[^-_.A-Za-z0-9]", "").ToLower();
        }
    }
}
#endif