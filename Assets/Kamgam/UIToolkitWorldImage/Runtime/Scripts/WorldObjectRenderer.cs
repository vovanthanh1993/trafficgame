using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kamgam.UIToolkitWorldImage
{
    /// <summary>
    /// NOTICE: The game object should NEVER be disabled. There is an explicit SetActive() method to 
    /// enable/disable the renderer components.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(WorldObjectRendererRegistryWorker))]
    public partial class WorldObjectRenderer : MonoBehaviour
    {
        /// <summary>
        /// Called once after the camera has been created and positioned.<br />
        /// Use to add custom object to your renering (like lights, extra backgrounds, ..).
        /// </summary>
        public event System.Action<WorldObjectCamera> OnCameraCreated;

        public event System.Action<WorldObjectRenderer> OnWillEnable;
        public event System.Action<WorldObjectRenderer> OnWillDisable;
        public event System.Action<WorldObjectRenderer> OnWillDestroy;

        [SerializeField]
        protected List<Transform> m_worldObjects = new List<Transform>();
        public IEnumerable<Transform> WorldObjects => m_worldObjects;
        public int WorldObjectsCount => m_worldObjects.Count;

        public void AddWorldObject(Transform obj)
        {
            if (!m_worldObjects.Contains(obj))
            {
                m_worldObjects.Add(obj);
                UpdateWorldObjectBounds();
            }
        }

        public void RemoveWorldObject(Transform obj)
        {
            if (m_worldObjects.Contains(obj))
            {
                m_worldObjects.Remove(obj);
                UpdateWorldObjectBounds();
            }
        }

        public void DefragWorldObjects()
        {
            for (int i = m_worldObjects.Count-1; i >= 0; i--)
            {
                if (m_worldObjects[i] == null)
                {
                    m_worldObjects.RemoveAt(i);
                }
            }
        }

        public Transform GetWorldObjectAt(int index)
        {
            DefragWorldObjects();

            if (m_worldObjects == null || m_worldObjects.Count <= index)
                return null;

            return m_worldObjects[index];
        }

        public bool HasActiveWorldObjects()
        {
            if (m_worldObjects == null || m_worldObjects.Count == 0)
                return false;

            foreach (var obj in m_worldObjects)
            {
                if (obj != null && obj.gameObject.activeSelf)
                    return true;
            }

            return false;
        }

        public Transform GetFirstActiveWorldObject()
        {
            if (m_worldObjects == null || m_worldObjects.Count == 0)
                return null;

            foreach (var obj in m_worldObjects)
            {
                if (obj != null && obj.gameObject.activeSelf)
                    return obj;
            }

            return null;
        }

        [SerializeField]
        [Tooltip("This is the 'Renderer Id' that you have to enter in the WorldImage element to connect it to this renderer.")]
        protected string m_id = null;
        public string Id
        {
            get
            {
                return m_id;
            }

            set
            {
                if (m_id != value)
                {
                    m_id = value;
                    ApplyId();
                }
            }
        }

        public void ApplyId()
        {
            gameObject.name = "World Object Renderer (" + Id + ")";

            if (m_objectCamera != null)
            {
                m_objectCamera.name = WorldObjectCamera.CreateName(this);
            }
        }

        [Header("Rendering")]

        [SerializeField]
        [ShowIf("m_useRenderTexture", true, ShowIfAttribute.DisablingType.ReadOnly)]
        protected RenderTextureSize m_resolutionWidth = RenderTextureSize._512;
        public RenderTextureSize ResolutionWidth
        {
            get
            {
                return m_resolutionWidth;
            }

            set
            {
                if (m_resolutionWidth != value)
                {
                    m_resolutionWidth = value;
                    ApplyResolutionWidth();
                }
            }
        }

        public void ApplyResolutionWidth()
        {
            ForceRenderTextureUpdate();

#if UNITY_EDITOR
            if (RenderTextureOverride != null)
            {
                Debug.LogWarning("WorldImage: Render texture override is set. Changing the resolution will have no effect until you remove the override.");
            }
#endif
        }

        [SerializeField]
        [ShowIf("m_useRenderTexture", true, ShowIfAttribute.DisablingType.ReadOnly)]
        protected RenderTextureSize m_resolutionHeight = RenderTextureSize._512;
        public RenderTextureSize ResolutionHeight
        {
            get
            {
                return m_resolutionHeight;
            }

            set
            {
                if (m_resolutionHeight != value)
                {
                    m_resolutionHeight = value;
                    ApplyResolutionHeight();
                }
            }
        }

        public void ApplyResolutionHeight()
        {
            ForceRenderTextureUpdate();

#if UNITY_EDITOR
            if (RenderTextureOverride != null)
            {
                Debug.LogWarning("WorldImage: Render texture override is set. Changing the resolution will have no effect until you remove the override.");
            }
#endif
        }

        public void ForceRenderTextureUpdate()
        {
            RenderTexture = null;
            RenderTexture = getOrCreateRenderTexture();
            WorldImageRegistry.Main.UpdateTexture(Id);
            WorldImageRegistry.Main.MarkDirtyRepaint(Id);
        }

        [System.NonSerialized]
        protected RenderTexture m_renderTexture;
        public RenderTexture RenderTexture
        {
            get
            {
                if (!UseRenderTexture || !gameObject.activeInHierarchy)
                    return null;

                if (RenderTextureOverride != null)
                {
                    return RenderTextureOverride;
                }

                if (m_renderTexture == null)
                {
                    RenderTexture = getOrCreateRenderTexture();
                }
                return m_renderTexture;
            }

            set
            {
                if (m_renderTexture != value)
                {
                    if (value == null)
                    {
                        RenderTexturePool.ReturnTexture(m_renderTexture);
                        m_renderTexture = null;
                    }
                    else
                    {
                        m_renderTexture = value;
                    }

                    // Update texture on camera
                    if (m_renderTexture == null)
                    {
                        // Do NOT auto create camera if value is null.
                        if (m_objectCamera != null && m_objectCamera.gameObject != null && m_objectCamera.Camera != null)
                        {
                            if (RenderTextureOverride != null && UseRenderTexture)
                                m_objectCamera.SetTargetTexture(RenderTextureOverride);
                            else
                                m_objectCamera.SetTargetTexture(null);
                        }
                        return;
                    }
                    else if (ObjectCamera != null && ObjectCamera.gameObject != null && ObjectCamera.Camera != null) // Auto creates camera if needed.
                    {
                        if (UseRenderTexture)
                        {
                            if (RenderTextureOverride != null)
                                ObjectCamera.SetTargetTexture(RenderTextureOverride);
                            else
                                ObjectCamera.SetTargetTexture(m_renderTexture);
                        }
                        else
                        {
                            ObjectCamera.SetTargetTexture(null);
                        }
                    }
                }
            }
        }

        public bool HasTmpRenderTexture => UseRenderTexture && m_renderTexture != null;

        [System.NonSerialized]
        protected WorldObjectCamera m_objectCamera;
        public WorldObjectCamera ObjectCamera
        {
            get
            {
                if (CameraOverride != null)
                {
                    return CameraOverride;
                }

                if (m_objectCamera == null || m_objectCamera.IsScheduledForDestruction)
                {
                    var cam = Utils.FindRootObjectByType<WorldObjectCamera>(includeInactive: true);

                    //UnityEditor.EditorApplication
                    m_objectCamera = WorldObjectCamera.CreateForImage(this);
                    UpdateCameraTransform();
                    m_objectCamera.SetTargetTexture(RenderTexture);
                    OnCameraCreated?.Invoke(m_objectCamera);
                }
                return m_objectCamera;
            }

            set
            {
                if (value != m_objectCamera)
                    m_objectCamera = value;
            }
        }

        [SerializeField]
        [Tooltip("The offset position of the point where the camera looks at relative to the world object.\n" +
            "The up-axis will be defined by the 'CameraRoll' option.")]
        protected Vector3 m_cameraLookAtPosition = new Vector3(0f, 0f, 0f);
        public Vector3 CameraLookAtPosition
        {
            get => m_cameraLookAtPosition * m_cameraOffsetAndPositionMultiplier;
            set
            {
                if (m_cameraLookAtPosition == value)
                    return;

                m_cameraLookAtPosition = value;
                ApplyCameraLookAtPosition();
            }
        }

        public void ApplyCameraLookAtPosition()
        {
            UpdateCameraTransform();
            ObjectCamera.UpdateCameraClippingFromBounds();
        }


        [SerializeField]
        [Tooltip("The offset of the rendering camera relative to CameraLookAtPosition.\n" +
            "Usually z = -1 is a good starting point.")]
        protected Vector3 m_cameraOffset = new Vector3(0f, 0f, -10f);
        public Vector3 CameraOffset
        {
            get => m_cameraOffset * m_cameraOffsetAndPositionMultiplier;
            set
            {
                if (m_cameraOffset == value)
                    return;

                m_cameraOffset = value;
                ApplyCameraOffset();
            }
        }

        public void ApplyCameraOffset()
        {
            UpdateCameraTransform();
            ObjectCamera.UpdateCameraClippingFromBounds();
        }

        [SerializeField]
        [Tooltip("All camera position and offset values are multiplied by this.\n\n" +
            "If set to a low value (like 0.1f) then this makes it easier to position the camera in relation to small objects.")]
        [Range(0.01f, 2f)]
        protected float m_cameraOffsetAndPositionMultiplier = 0.1f;
        public float CameraOffsetAndPositionMultiplier
        {
            get => m_cameraOffsetAndPositionMultiplier;
            set
            {
                if (Mathf.Approximately(m_cameraOffsetAndPositionMultiplier, value))
                    return;

                float oldValue = m_cameraOffsetAndPositionMultiplier;
                m_cameraOffsetAndPositionMultiplier = value;
                ApplyCameraOffsetAndPositionMultiplier(oldValue);
            }
        }

        public void ApplyCameraOffsetAndPositionMultiplier(float oldValue)
        {
            // Apply the inverse the the position and offset
            float changeRatio = oldValue / m_cameraOffsetAndPositionMultiplier;
            m_cameraOffset *= changeRatio;
            m_cameraLookAtPosition *= changeRatio;
        }

        [SerializeField]
        [Tooltip("The angle to rotate the camera around (clock-wise in look direction).\n" +
            "This is used while making the camera look at the 'CameraLookAtOffset' position.")]
        protected float m_cameraRoll = 0f;
        public float CameraRoll
        {
            get => m_cameraRoll;
            set
            {
                if (m_cameraRoll == value)
                    return;

                m_cameraRoll = value;
                ObjectCamera.UpdateCameraClippingFromBounds();
            }
        }

        [SerializeField]
        [Tooltip("If enabled then the offsets (CameraLookAtOffset and CameraOffset) are calculated within the local space of the first WorldObject transform.\n\n" +
            "This means the camera will follow the rotation and scale of the transform.\n" +
            "NOTICE: By default the camera position is always relative to the first active world object in the list.")]
        [ShowIf("CameraOrigin", true, ShowIfAttribute.DisablingType.ReadOnly)]
        protected bool m_CameraFollowTransform = false;
        public bool CameraFollowTransform
        {
            get => HasActiveWorldObjects() && m_CameraFollowTransform;
            set
            {
                if (m_CameraFollowTransform == value)
                    return;

                m_CameraFollowTransform = value;
            }
        }

        [SerializeField]
        [Tooltip("If set the this will be used as the origin position to which the camera LookAt etc. will be added.")]
        protected Transform m_cameraOrigin;
        public Transform CameraOrigin
        {
            get => m_cameraOrigin;
            set
            {
                if (m_cameraOrigin == value)
                    return;

                m_cameraOrigin = value;
                ObjectCamera.UpdateTransform();
            }
        }

        [SerializeField]
        [Tooltip("If enabled then the near an far clipping plane of the rendering camera will be " +
            "automatically reduced to the size of the 'WorldObjects' combined bounding boxes.")]
        protected bool m_cameraUseBoundsToClip = true;
        public bool CameraUseBoundsToClip
        {
            get => HasActiveWorldObjects() && m_cameraUseBoundsToClip;
            set
            {
                if (m_cameraUseBoundsToClip == value)
                    return;

                m_cameraUseBoundsToClip = value;
                ObjectCamera.UpdateCameraClippingFromBounds();
            }
        }

        [SerializeField]
        [Tooltip("If enabled then the offsets (CameraLookAtOffset and CameraOffset) are calculated based on the bounding box center.\n\n" +
            "If disabled then the offsets are based on the position of the very first entry in the 'WorldObjects' list.\n\n" +
            "HINT: Turn this off if any of your objects are animating or else the camera might be jumpy since the bounds will change with the animation.")]
        protected bool m_cameraFollowBoundsCenter = false;
        public bool CameraFollowBoundsCenter
        {
            get => HasActiveWorldObjects() && m_cameraFollowBoundsCenter;
            set
            {
                if (m_cameraFollowBoundsCenter == value)
                    return;

                m_cameraFollowBoundsCenter = value;

                m_worldObjectBounds = calculateWorldObjectsBounds();
                ObjectCamera.UpdateCameraClippingFromBounds();
            }
        }

        [SerializeField]
        [Tooltip("If enabled then the bounds to center on will be updated every frame.\n\n" +
            "Keep disabled if possible and instead call 'UpdateCameraClippingFromBounds()'.")]
        protected bool m_cameraAutoUpdateBounds = false;
        public bool CameraAutoUpdateBounds
        {
            get => HasActiveWorldObjects() && m_cameraAutoUpdateBounds;
            set
            {
                if (m_cameraAutoUpdateBounds == value)
                    return;

                m_cameraAutoUpdateBounds = value;

                m_worldObjectBounds = calculateWorldObjectsBounds();
                ObjectCamera.UpdateCameraClippingFromBounds();
            }
        }

        [SerializeField]
        [Tooltip("If disabled then the UI image will not render anything and instead camera stacking will be used. \n\n" +
            "NOTICE: This option only works for SCREEN SPACE CAMERA canvases!\n\n" +
            "NOTICE: If disabled then transparent backgrounds are NOT supported in URP and HDRP.")]
        protected bool m_useRenderTexture = true;
        public bool UseRenderTexture
        {
            get
            {
                return m_useRenderTexture;
            }
            set
            {
                if (m_useRenderTexture == value)
                    return;

                m_useRenderTexture = value;
                ApplyUseRenderTexture();
            }
        }
        public bool _useRenderTextureRawEditor => m_useRenderTexture;

        public void ApplyUseRenderTexture()
        {
            if (!UseRenderTexture)
            {
                RenderTexture = null;
                ObjectCamera.SetUseRenderTexture(false);
            }
            else
            {
                ObjectCamera.SetUseRenderTexture(UseRenderTexture);
                RenderTexture = RenderTexture;
            }

            WorldImageRegistry.Main.UpdateTexture(Id);
            WorldImageRegistry.Main.MarkDirtyRepaint(Id);
        }

        [SerializeField]
        protected float m_CameraNearClipPlane = 0.3f;

        public float CameraNearClipPlane
        {
            get => m_CameraNearClipPlane;
            set
            {
                if (Mathf.Approximately(m_CameraNearClipPlane, value))
                    return;

                m_CameraNearClipPlane = value;
                ApplyCameraNearClipPlane();
            }
        }

        public void ApplyCameraNearClipPlane()
        {
            ObjectCamera.Camera.nearClipPlane = m_CameraNearClipPlane ;
        }

        [SerializeField]
        protected float m_CameraFarClipPlane = 1000f;

        public float CameraFarClipPlane
        {
            get => m_CameraFarClipPlane;
            set
            {
                if (Mathf.Approximately(m_CameraFarClipPlane, value))
                    return;

                m_CameraFarClipPlane = value;
                ApplyCameraFarClipPlane();
            }
        }

        public void ApplyCameraFarClipPlane()
        {
            ObjectCamera.Camera.farClipPlane = m_CameraFarClipPlane;
        }

        public static float DefaultCameraFieldOfView = 60f;

        [SerializeField]
        protected float m_cameraFieldOfView = DefaultCameraFieldOfView;
        public float CameraFieldOfView
        {
            get => m_cameraFieldOfView;
            set
            {
                if (Mathf.Approximately(m_cameraFieldOfView, value))
                    return;

                m_cameraFieldOfView = value;

                ApplyCameraFieldOfView();
            }
        }

        public void ApplyCameraFieldOfView()
        {
            ObjectCamera.Camera.fieldOfView = m_cameraFieldOfView;
        }

        public static Color DefaultCameraBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 0f);

        [SerializeField]
        public WorldObjectCamera.ClearType m_cameraClearType = WorldObjectCamera.ClearType.Color; 
        public WorldObjectCamera.ClearType CameraClearType
        {
            get => m_cameraClearType;
            set
            {
                if (m_cameraClearType == value)
                    return;

                m_cameraClearType = value;
                ApplyCameraClearType();
            }
        }

        public void ApplyCameraClearType()
        {
            ObjectCamera.SetCameraClearType(m_cameraClearType);
        }


        [ShowIf("m_cameraClearType", WorldObjectCamera.ClearType.Color, ShowIfAttribute.DisablingType.DontDraw)] 
        [SerializeField]
        protected Color m_cameraBackgroundColor = DefaultCameraBackgroundColor;
        public Color CameraBackgroundColor
        {
            get => m_cameraBackgroundColor;
            set
            {
                if (m_cameraBackgroundColor == value)
                    return;

                m_cameraBackgroundColor = value;
                ApplyCameraBackgroundColor();
            }
        }

        public void ApplyCameraBackgroundColor()
        {
            ObjectCamera.SetBackgroundColor(m_cameraBackgroundColor);
        }

        public static float DefaultCameraDepth = 0f;

        [SerializeField]
        [ShowIf("m_useRenderTexture", false, ShowIfAttribute.DisablingType.ReadOnly)]
        [Tooltip("The depth that is used if camera stacking is used instead of a render texture.")]
        protected float m_cameraDepth = DefaultCameraDepth;
        public float CameraDepth
        {
            get => m_cameraDepth;
            set
            {
                if (Mathf.Approximately(m_cameraDepth, value))
                    return;

                m_cameraDepth = value;
                ApplyCameraDepth();
            }
        }

        public void ApplyCameraDepth()
        {
            ObjectCamera.Camera.depth = m_cameraDepth;
        }

        public static int DefaultCameraCullingMask = -1;

        [SerializeField]
        [Tooltip("Which layers the camera renders")]
        protected LayerMask m_cameraCullingMask = DefaultCameraCullingMask;
        public int CameraCullingMask
        {
            get => m_cameraCullingMask.value;
            set
            {
                if (m_cameraCullingMask == value)
                    return;

                m_cameraCullingMask = value;
                ApplyCameraCullingMask();
            }
        }

        public void ApplyCameraCullingMask()
        {
            ObjectCamera.Camera.cullingMask = m_cameraCullingMask.value;
        }


        [Header("Overrides")]
        [Tooltip("If set then this camera will be used to render into the render texture.\n\n" +
            "Useful for debugging or if you want to use a custom camera.")]
        public WorldObjectCamera CameraOverride;

        [Tooltip("If set then this render texture will be used as the render tearget of the object camera.\n\n" +
            "Useful for debugging or if you want to use a custom render texture.")]
        public RenderTexture RenderTextureOverride;


        protected PrefabInstantiatorForWorldObjectRenderer m_prefabInstantiator;
        public PrefabInstantiatorForWorldObjectRenderer PrefabInstantiator
        {
            get
            {
                if (m_prefabInstantiator == null)
                {
                    m_prefabInstantiator = this.GetComponent<PrefabInstantiatorForWorldObjectRenderer>();
                }
                return m_prefabInstantiator;
            }
        }

        public bool IsActive => enabled;

        public void SetActive(bool active)
        {
            if (enabled == active)
                return;

            // Usually the renderer is disabled if no image is enabled but this is a poor user
            // experience because what if the UI is code generated or the UI is split into multiple
            // layouts. Both are valid reasons for the image to not being loaded but the renderer should
            // still be editable. Thus we always enable it if the user is in the editor and in edit-mode.
            bool inEditMode = false;
#if UNITY_EDITOR
            inEditMode = !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#endif

            if (active || inEditMode)
            {
                gameObject.hideFlags = HideFlags.None;
                enabled = true;

                if (PrefabInstantiator != null)
                {
                    PrefabInstantiator.enabled = true;
                }

            }
            else
            {
                if (m_objectCamera != null && m_objectCamera.Camera != null)
                {
                    m_objectCamera.gameObject.SetActive(false);
                    m_objectCamera.SetTargetTexture(null);
                }

                if (PrefabInstantiator != null)
                {
                    PrefabInstantiator.enabled = false;
                }

                gameObject.hideFlags = HideFlags.NotEditable;
                enabled = false;
            }
        }

        RenderTexture getOrCreateRenderTexture()
        {
            var width = RenderTextureSizeUtils.SizeToPixels(ResolutionWidth);
            var height = RenderTextureSizeUtils.SizeToPixels(ResolutionHeight);
            var texture = RenderTexturePool.GetTexture(width, height, depth: 16, RenderTextureFormat.Default);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            return texture;
        }

        /// <summary>
        /// Releases the render texture and sets the current render texture to null.<br />
        /// NOTICE: If 'UseRenderTexture' is true then it is very likely that the render
        /// texture will be regenerated automatically.
        /// </summary>
        public void ReleaseRenderTexture()
        {
            var rt = m_renderTexture;
            RenderTexture = null;

            // Make extra sure the camera texture is also set to null if the texture is released.
            if (m_objectCamera != null && m_objectCamera.Camera != null && m_objectCamera.Camera.targetTexture == rt)
            {
                m_objectCamera.SetTargetTexture(null);
            }

            rt.Release();
        }

        [System.NonSerialized]
        Bounds? m_worldObjectBounds = null;

        [System.NonSerialized]
        List<MeshRenderer> m_tmpBoundsMeshRenderers = new List<MeshRenderer>();

        [System.NonSerialized]
        List<SkinnedMeshRenderer> m_tmpBoundsSkinnedMeshRenderers = new List<SkinnedMeshRenderer>();

        public bool HasBounds => m_worldObjectBounds.HasValue;

        public Bounds? GetWorldObjectsBounds()
        {
            if (!m_worldObjectBounds.HasValue)
                UpdateWorldObjectBounds();

            return m_worldObjectBounds;
        }

        public void UpdateWorldObjectBounds()
        {
            m_worldObjectBounds = calculateWorldObjectsBounds();
        }

        Bounds? calculateWorldObjectsBounds()
        {
            Bounds? bounds = null;
            foreach (var obj in m_worldObjects)
            {
                if (obj == null)
                    continue;

                m_tmpBoundsMeshRenderers.Clear();
                obj.GetComponentsInChildren<MeshRenderer>(m_tmpBoundsMeshRenderers);
                if (m_tmpBoundsMeshRenderers.Count > 0)
                {
                    foreach (var meshRenderer in m_tmpBoundsMeshRenderers)
                    {
                        if (!meshRenderer.gameObject.activeInHierarchy)
                            continue;

                        if (bounds.HasValue)
                        {
                            var bnds = bounds.Value;
                            bnds.Encapsulate(meshRenderer.bounds);
                            bounds = bnds;
                        }
                        else
                        {
                            bounds = meshRenderer.bounds;
                        }
                    }
                }

                m_tmpBoundsSkinnedMeshRenderers.Clear();
                obj.GetComponentsInChildren<SkinnedMeshRenderer>(m_tmpBoundsSkinnedMeshRenderers);
                if (m_tmpBoundsSkinnedMeshRenderers.Count > 0)
                {
                    foreach (var skinnedRenderer in m_tmpBoundsSkinnedMeshRenderers)
                    {
                        if (!skinnedRenderer.gameObject.activeInHierarchy)
                            continue;

                        if (bounds.HasValue)
                        {
                            var bnds = bounds.Value;
                            bnds.Encapsulate(skinnedRenderer.bounds);
                            bounds = bnds;
                        }
                        else
                        {
                            bounds = skinnedRenderer.bounds;
                        }
                    }
                }
            }

            return bounds;
        }

        public void Update()
        {
            ObjectCamera.gameObject.SetActive(gameObject.activeInHierarchy);

            if (m_worldObjects != null && m_worldObjects.Count > 0)
            {
                UpdateCameraTransform();
            }
        }

        public void OnEnable()
        {
            WorldObjectRendererRegistry.Main.Register(this);

            WorldImageRegistry.Main.UpdateObjectRendererActiveState(Id);
            WorldImageRegistry.Main.UpdateTexture(Id);
            WorldImageRegistry.Main.MarkDirtyRepaint(Id);

            if (TryGetComponent<PrefabInstantiatorForWorldObjectRenderer>(out var prefabInstantiator))
            {
                prefabInstantiator.RegisterActiveStateEvents(this); 
            }

            OnWillEnable?.Invoke(this);

            ObjectCamera.SetTargetTexture(RenderTexture);
            ObjectCamera.gameObject.SetActive(true);

            // Apply critical properties (n2h: investigate, maybe apply all here)
            ApplyCameraClearType();
            ApplyCameraBackgroundColor();

            // Force an update of the render texture after deserialization in editor (usually after recompile).
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && UseRenderTexture)
            {
                Update();
            }
#endif
        }

        public void OnDisable()
        {
            OnWillDisable?.Invoke(this);

            if (m_objectCamera != null)
                m_objectCamera.gameObject.SetActive(false);
            RenderTexture = null;
        }

        public void OnDestroy()
        {
            OnWillDestroy?.Invoke(this);

            RenderTexture = null;
            destroyCamera();
            if (TryGetComponent<PrefabInstantiatorForWorldObjectRenderer>(out var prefabInstantiator))
            {
                prefabInstantiator.RemoveAllFromImage(this);
                prefabInstantiator.DestroyAllInstances();
            }
        }

        private void destroyCamera()
        {
            if (m_objectCamera != null && m_objectCamera.gameObject != null)
            {
                Utils.SmartDestroy(m_objectCamera.gameObject);
            }
        }

        public void UpdateCameraTransform()
        {
            // Update bounds only if necessary
            if (!m_worldObjectBounds.HasValue || CameraAutoUpdateBounds)
            {
                m_worldObjectBounds = calculateWorldObjectsBounds();
            }

            ObjectCamera.UpdateTransform();

            if (m_worldObjectBounds.HasValue)
            {
                ObjectCamera.UpdateCameraClippingFromBounds();
            }
        }
    }
}