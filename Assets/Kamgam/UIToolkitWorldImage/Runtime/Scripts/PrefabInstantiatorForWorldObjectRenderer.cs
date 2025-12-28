using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kamgam.UIToolkitWorldImage
{
    [RequireComponent(typeof(WorldObjectRenderer))]
    public class PrefabInstantiatorForWorldObjectRenderer : MonoBehaviour
    {

        [Serializable]
        public class PrefabHandle
        {
            public GameObject Prefab;
            
            [Space(5)]
            public Vector3 Position = Vector3.zero;
            public Quaternion Rotation;
            public Vector3 Scale = Vector3.one;
            
            [Tooltip("Leave empty to spawn at root level.")]
            public Transform Parent;

            [System.NonSerialized]
            public GameObject Instance;

            /// <summary>
            /// Use this in a dynamic setting to add a reference to your custom data.
            /// </summary>
            [System.NonSerialized]
            public object UserData;

            public PrefabHandle(GameObject prefab, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Transform parent, object userData = null)
            {
                Prefab = prefab;
                Position = localPosition;
                Scale = localScale;
                Rotation = localRotation;
                Parent = parent;
                UserData = userData;
            }

            public bool HasPrefab => Prefab != null;
            public bool HasInstance => Instance != null;

            public void CreateOrUpdateInstance(WorldObjectRenderer renderer, Transform parentOverride = null, System.Action<PrefabHandle> onCreateEvent = null)
            {
                if (Prefab == null)
                    return;

                bool created = false;
                if (Instance == null)
                {
                    Instance = GameObject.Instantiate(Prefab, parentOverride != null ? parentOverride : Parent);
                    Instance.name = Prefab.name + " (for World Renderer " + renderer.GetInstanceID() + ")";
                    Instance.AddComponent<DestroyAfterDeserializationInEditor>();
                    created = true;
                }

                Instance.transform.localPosition = Position;
                Instance.transform.localScale = Scale;
                Instance.transform.localRotation = Rotation;

                if (created)
                    onCreateEvent?.Invoke(this);
            }

            public void DestroyInstance()
            {
                if (Instance == null)
                {
                    return;
                }

                var instance = Instance;
                Instance = null;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    GameObject.DestroyImmediate(instance);
                }
                else
#endif
                {
                    GameObject.Destroy(instance);
                }
            }

            public void EnableInstance()
            {
                if (Instance == null)
                {
                    return;
                }

                Instance.gameObject.SetActive(true);
            }

            public void DisableInstance()
            {
                if (Instance == null)
                {
                    return;
                }

                Instance.gameObject.SetActive(false);
            }

            public override string ToString()
            {
                if (Instance != null)
                    return Instance.name;

                if (Prefab != null)
                    return Prefab.name;

                return base.ToString();
            }
        }



        /// <summary>
        /// A list of all the prefab handles.
        /// To access the prefab object use the ".Prefab" property of a handle.
        /// To access the instance in the scene use the ".Instance" property of a handle.
        /// </summary>
        [SerializeField]
        [ShowIf("m_prefabSourceAsset", null, ShowIfAttribute.DisablingType.ReadOnly)]
        [Tooltip("List of prefabs.\n" +
            "NOTICE: These may be overruled by the PrefabSourceAsset.")]
        protected List<PrefabHandle> m_prefabs = new List<PrefabHandle>();

        /// <summary>
        /// If set then this will take precedence over the default Prefabs list.<br />
        /// This Scriptable Object will have to implement the IPrefabInstantiatorForWorldObjectRendererPrefabSource interface or else it will be ignored.
        /// </summary>
        [SerializeField]
        [Tooltip("If set then this will take precedence over the default Prefabs list.\n" +
            "This Scriptable Object will have to implement the IPrefabInstantiatorForWorldObjectRendererPrefabSource interface or else it will be ignored.")]
        protected ScriptableObject m_prefabSourceAsset;

        /// <summary>
        /// If set then this will take precendence over both the PrefabSourceAsset and the Prefabs list.
        /// </summary>
        protected IPrefabInstantiatorForWorldObjectRendererPrefabSource m_prefabSource;

        /// <summary>
        /// If set then this will be used as the parent for all prefab instances.
        /// </summary>
        [Tooltip("If set then this will be used as the parent for all prefab instances.")]
        public Transform PrefabParentOverride;

        public List<PrefabHandle> Prefabs
        {
            get
            {
                if (m_prefabSource != null)
                {
                    return m_prefabSource.GetPrefabHandles();
                }

                if (m_prefabSourceAsset != null)
                {
                    var sourceAsset = m_prefabSourceAsset as IPrefabInstantiatorForWorldObjectRendererPrefabSource;
                    if(sourceAsset != null)
                        return sourceAsset.GetPrefabHandles();
                }

                return m_prefabs;
            }
        }
        

        public void SetPrefabs(List<PrefabHandle> prefabs)
        {
            m_prefabs = prefabs;
        }

        public void SetPrefabSource(IPrefabInstantiatorForWorldObjectRendererPrefabSource source)
        {
            m_prefabSource = source;
        }

        [Tooltip("Should instances be marked as do-not-save in the editor?")]
        public bool MarkAsDoNotSave = true;

        [Space(5)]
        [Tooltip("Should the prefabs be instantiated in OnEnable?\n" +
            "Instances will only be generated if not yet instantiated.")]
        public bool InstantiateOnEnable = false;

        [Tooltip("If the renderer is enabled then the prefab instances will be enabled.")]
        public bool ActivateOnEnable = false;

        [Tooltip("If empty then all prefabs will be instantiated or activated. If set then the indices in the list will be instantiated/activated.\n" +
            "This is used for both 'InstantiateOnEnable' and 'ActivateOnEnable'.\n" +
            "NOTICE: This will do nothing if 'InstantiateOnEnable' and 'ActivateOnEnable' are both off.")]
        public List<int> OnEnableIndices = new List<int>();

        [Tooltip("If the renderer is disabled then the prefab instances will be disabled.")]
        public bool DeactivateOnDisable = true;

        [Tooltip("If the renderer is destroyed then all the prefab instances will be destroyed too.")]
        public bool DestroyOnDestroy = true;
        [Space(5)]

        [Tooltip("Should instances be added to the world objects list?\n" +
            "Usually keeping this ON is recommended. Though it may be useful to disable this " +
            "if you have a dedicated prefab instance parent for all your prefabs.")]
        public bool AddToWorldObjectsList = true;

        public event System.Action<PrefabInstantiatorForWorldObjectRenderer> OnWillEnable;
        public event System.Action<PrefabInstantiatorForWorldObjectRenderer> OnWillDisable;
        public event System.Action<PrefabInstantiatorForWorldObjectRenderer> OnWillDestroy;

        /// <summary>
        /// An event you can subscribe to that is called after the creation of a new prefab instance.<br />
        /// Useful for doing setup stuff on your instances.
        /// </summary>
        public event Action<PrefabHandle> OnCreatedInstance;

        [System.NonSerialized]
        protected WorldObjectRenderer m_WorldObjectRenderer;
        public WorldObjectRenderer WorldObjectRenderer
        {
            get
            {
                if (m_WorldObjectRenderer == null || m_WorldObjectRenderer.gameObject == null)
                {
                    m_WorldObjectRenderer = this.GetComponent<WorldObjectRenderer>();
                    RegisterActiveStateEvents(m_WorldObjectRenderer);
                }
                return m_WorldObjectRenderer;
            }
        }

        public void RegisterActiveStateEvents(WorldObjectRenderer renderer)
        {
            if (renderer != null)
            {
                renderer.OnWillEnable -= onRendererWillEnable;
                renderer.OnWillEnable += onRendererWillEnable;

                renderer.OnWillDisable -= onRendererWillDisable;
                renderer.OnWillDisable += onRendererWillDisable;
            }
        }

        protected void onRendererWillEnable(WorldObjectRenderer renderer)
        {
            if (InstantiateOnEnable)
            {
                instantiateOnEnable(renderer);
            }

            if (ActivateOnEnable)
            {
                activateOnEnable();
            }
        }

        private void instantiateOnEnable(WorldObjectRenderer renderer)
        {
            if (OnEnableIndices.Count == 0)
            {
                CreateAndAddAllInstancesToImage(renderer);
            }
            else
            {
                for (int i = 0; i < Prefabs.Count; i++)
                {
                    if (!OnEnableIndices.Contains(i))
                        continue;

                    var handle = Prefabs[i];
                    CreateOrUpdateInstance(renderer, handle);
                    if (AddToWorldObjectsList)
                    {
                        AddToImage(renderer, handle);
                    }
                }
            }
        }

        private void activateOnEnable()
        {
            if (OnEnableIndices.Count == 0)
            {
                foreach (var handle in Prefabs)
                {
                    if (handle.Instance != null)
                    {
                        handle.Instance.SetActive(true);
                    }
                }
            }
            else
            {
                for (int i = 0; i < Prefabs.Count; i++)
                {
                    if (!OnEnableIndices.Contains(i))
                        continue;

                    var handle = Prefabs[i];
                    if (handle.Instance != null)
                    {
                        handle.Instance.SetActive(true);
                    }
                }
            }

            WorldObjectRenderer.UpdateWorldObjectBounds();
        }

        protected void onRendererWillDisable(WorldObjectRenderer renderer)
        {
            if (DeactivateOnDisable)
            {
                foreach (var handle in Prefabs)
                {
                    if (handle.Instance != null)
                    {
                        handle.Instance.SetActive(false);
                    }
                }
            }
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            foreach (var handle in Prefabs)
            {
                if (handle.Scale.sqrMagnitude < 0.001f && handle.Position.sqrMagnitude < 0.001f)
                {
                    handle.Scale = Vector3.one;
                    UnityEditor.EditorUtility.SetDirty(this);
                }

                if (handle.Prefab != null && UnityEditor.PrefabUtility.GetPrefabAssetType(handle.Prefab) == UnityEditor.PrefabAssetType.NotAPrefab)
                {
                    handle.Prefab = null;
                    Debug.LogError("Prefab Instantiator: The chosen object is not a prefab! Resetting to null.");
                }

                // Update while editing
                if (handle.Instance != null)
                {
                    handle.Instance.transform.localPosition = handle.Position;
                    handle.Instance.transform.localScale = handle.Scale;
                    handle.Instance.transform.localRotation = handle.Rotation;
                }
            }

            WorldObjectRenderer.UpdateWorldObjectBounds();
        }
#endif

        public void CreateAndAddAllInstancesToImage(WorldObjectRenderer renderer)
        {
            CreateOrUpdateAllInstances(renderer);

            if (AddToWorldObjectsList)
                AddAllToImage(renderer);
        }

        public void CreateAndAddInstanceToImage(WorldObjectRenderer renderer, PrefabHandle handle)
        {
            CreateOrUpdateInstance(renderer, handle);

            if (AddToWorldObjectsList)
                AddToImage(renderer, handle);
        }

        public void RemoveAllFromImageAndDestroyInstances(WorldObjectRenderer renderer)
        {
            RemoveAllFromImage(renderer);
            DestroyAllInstances();
        }

        public void RemoveFromImageAndDestroyInstance(WorldObjectRenderer renderer, PrefabHandle handle)
        {
            RemoveFromImage(renderer, handle);
            DestroyInstance(handle);
        }

        public void RemoveFromImageAndDisableInstance(WorldObjectRenderer renderer, PrefabHandle handle)
        {
            RemoveFromImage(renderer, handle);
            DisableInstance(handle);
        }

        public void CreateOrUpdateAllInstances(WorldObjectRenderer renderer)
        {
            foreach (var handle in Prefabs)
            {
                CreateOrUpdateInstance(renderer, handle);
            }
        }

        public void CreateOrUpdateInstance(WorldObjectRenderer renderer, PrefabHandle handle)
        {
            handle.CreateOrUpdateInstance(renderer, PrefabParentOverride, OnCreatedInstance);

            if (handle.Instance != null)
            {
                if (MarkAsDoNotSave)
                {
                    handle.Instance.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
                    if (!handle.Instance.name.StartsWith("[Temp]")) 
                        handle.Instance.name = "[Temp] " + handle.Instance.name;
                }
                else
                {
                    handle.Instance.hideFlags = HideFlags.None;
                }
            }
        }

        public void ToogleOrCreate(int prefabIndex)
        {
            ToogleOrCreate(prefabIndex, destroyOnDisable: false, disableOthers: true);
        }

        public void ToogleOrCreate(int prefabIndex, bool destroyOnDisable, bool disableOthers)
        {
            if (prefabIndex < 0 || prefabIndex >= Prefabs.Count)
                return;

            var prefabHandle = Prefabs[prefabIndex];
            ToogleOrCreate(prefabHandle, destroyOnDisable, disableOthers);
        }

        public void ToogleOrCreate(PrefabHandle prefabHandle, bool destroyOnDisable = false, bool disableOthers = true)
        {
            bool show;

            if (prefabHandle.Instance == null)
            {
                CreateAndAddInstanceToImage(WorldObjectRenderer, prefabHandle);
                show = true;
            }
            else
            {
                if (destroyOnDisable)
                {
                    show = false;
                }
                else
                {
                    show = !prefabHandle.Instance.activeInHierarchy;
                }
            }

            if (show)
            {
                if (!destroyOnDisable)
                {
                    prefabHandle.Instance.SetActive(true);
                }

                if (disableOthers)
                {
                    // Destroy/disable out all the others
                    foreach (var handle in Prefabs)
                    {
                        if (handle != prefabHandle && handle.HasInstance)
                        {
                            if (destroyOnDisable)
                            {
                                RemoveFromImageAndDestroyInstance(WorldObjectRenderer, handle);
                            }
                            else
                            {
                                DisableInstance(handle);
                            }
                        }
                    }
                }
            }
            else
            {
                if (destroyOnDisable)
                {
                    RemoveFromImageAndDestroyInstance(WorldObjectRenderer, prefabHandle);
                }
                else
                {
                    DisableInstance(prefabHandle);
                }
            }
        }

        public void EnableOrCreate(int prefabIndex)
        {
            EnableOrCreate(prefabIndex, destroyOnDisable: false, disableOthers: true);
        }

        public void EnableOrCreate(int prefabIndex, bool destroyOnDisable, bool disableOthers)
        {
            if (prefabIndex < 0 || prefabIndex >= Prefabs.Count)
                return;

            var prefabHandle = Prefabs[prefabIndex];
            EnableOrCreate(prefabHandle, destroyOnDisable, disableOthers);
        }

        public void EnableOrCreate(PrefabHandle prefabHandle, bool destroyOnDisable = false, bool disableOthers = true)
        {
            if (prefabHandle.Instance == null)
            {
                CreateAndAddInstanceToImage(WorldObjectRenderer, prefabHandle);
            }

            if (!destroyOnDisable)
            {
                prefabHandle.Instance.SetActive(true);
            }

            if (disableOthers)
            {
                // Destroy/disable out all the others
                foreach (var handle in Prefabs)
                {
                    if (handle != prefabHandle && handle.HasInstance)
                    {
                        if (destroyOnDisable)
                        {
                            RemoveFromImageAndDestroyInstance(WorldObjectRenderer, handle);
                        }
                        else
                        {
                            DisableInstance(handle);
                        }
                    }
                }
            }

            WorldObjectRenderer.UpdateWorldObjectBounds();
        }

        public void DisableAllInstances()
        {
            foreach (var handle in Prefabs)
            {
                handle.DisableInstance();
            }
        }

        public void DestroyAllInstances()
        {
            foreach (var handle in Prefabs)
            {
                handle.DestroyInstance();
            }
        }

        public void DestroyInstance(PrefabHandle handle)
        {
            handle.DestroyInstance();
        }

        public void EnableInstance(PrefabHandle handle)
        {
            handle.EnableInstance();
        }

        public void DisableInstance(PrefabHandle handle)
        {
            handle.DisableInstance();
        }

        public void AddAllToImage(WorldObjectRenderer renderer)
        {
            if (renderer == null)
                return;

            foreach (var handle in Prefabs)
            {
                if (handle.Instance != null)
                {
                    renderer.AddWorldObject(handle.Instance.transform);
                }
            }

            renderer.DefragWorldObjects();
            renderer.UpdateWorldObjectBounds();
        }

        public void AddToImage(WorldObjectRenderer renderer, PrefabHandle handle)
        {
            if (renderer == null || handle == null || handle.Instance == null)
                return;

            renderer.AddWorldObject(handle.Instance.transform);

            renderer.DefragWorldObjects();
            renderer.UpdateWorldObjectBounds();
        }

        public void RemoveAllFromImage(WorldObjectRenderer renderer)
        {
            if (renderer == null)
                return;

            foreach (var handle in Prefabs)
            {
                if (handle.Instance != null)
                {
                    renderer.RemoveWorldObject(handle.Instance.transform);
                }
            }

            renderer.DefragWorldObjects();
            renderer.UpdateWorldObjectBounds();
        }

        public void RemoveFromImage(WorldObjectRenderer renderer, PrefabHandle handle)
        {
            if (renderer == null || handle == null || handle.Instance == null)
                return;

            renderer.RemoveWorldObject(handle.Instance.transform);
            
            renderer.DefragWorldObjects();
            renderer.UpdateWorldObjectBounds();
        }

        public void OnEnable()
        {
            OnWillEnable?.Invoke(this);

            onRendererWillEnable(WorldObjectRenderer);
        }

        public void OnDisable()
        {
            OnWillDisable?.Invoke(this);
        }

        public void OnDestroy()
        {
            if (DestroyOnDestroy)
                DestroyAllInstances();

            OnWillDestroy?.Invoke(this);
        }
    }
}