using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace FXV
{
    [ExecuteInEditMode]
    public class VolumeFog : MonoBehaviour
    {
        static bool XR_ENABLED = false;
        static float Z_PLANE_OFFSET = 0.025f;

        public static void SetEnabledXR(bool enabled)
        {
            XR_ENABLED = enabled;
        }

        public static void SetCustomZOffset(float zOffset)
        {
            Z_PLANE_OFFSET = zOffset;
        }

        public enum FogType
        {
            ViewAligned = 0,
            SphericalPos = 1,
            SphericalDist = 2,
            InvertedSpherical = 8,
            InvertedSphericalXHeight = 11,
            BoxPos = 3,
            BoxDist = 4,
            BoxExperimental = 5,
            Height = 6,
            HeightXBox = 7,
            HeightXView = 9,
            BoxXView = 10,
        };

        public enum FogClipping
        {
            None = 0,
            ClipToSkybox = 1,
            ClipToBounds = 2
        }

        public enum FogBlendMode
        {
            AlphaBlend,
            Add
        }

        public enum FogMeshType
        {
            Default = 0,
            Custom
        }

        public enum FogFallof
        {
            Linear = 0,
            Smoothed,
            Exp,
            Exp2
        }

        public enum FogFunction
        {
            Default = 0,
            Alternative
        }

        public enum FogRenderMode
        {
            Simplified = 0,
            Default
        }

        public enum FogDebugMode
        {
            None = 0,
            StereoEyeIndex,
            CameraDepthTexture,
            CameraDepthTexture01,
            SceneDepthMinusCameraDepth,
            Position,
            ViewDir,
            ScreenPosXY,
            ScreenPosXY_StereoTransform
        }

        [SerializeField]
        FogType fogType = FogType.ViewAligned;

        [SerializeField]
        float fogMin = 0.0f;

        [SerializeField]
        float fogMax = 1.0f;

        [SerializeField]
        float _fogDepth = 1.0f;

        public float FogMin
        {
            get
            {
                return fogMin;
            }
            set
            {
                fogMin = value;

                _fogDepth = fogMax - fogMin;

                isPropsDirty = true;
            }
        }

        public float FogMax
        {
            get
            {
                return fogMax;
            }
            set
            {
                fogMax = value;

                _fogDepth = fogMax - fogMin;

                isPropsDirty = true;
            }
        }

        public float FogDepth
        {
            get
            {
                return fogMax - fogMin;
            }
            set
            {
                _fogDepth = value;

                fogMin = fogMax - value;

                isPropsDirty = true;
            }
        }

        [SerializeField]
        float secFogMin = 0.0f;

        [SerializeField]
        float secFogMax = 1.0f;

        [SerializeField]
        Vector3 worldSize = Vector3.one;

        [SerializeField]
        FogFallof fogFalloffType = FogFallof.Linear;

        [SerializeField, Range(0.5f, 15.0f)]
        float fogFalloffCurve = 1.0f;

        //[SerializeField]
        //int renderQueue = 3100;

        [SerializeField, FXV.Internal.fxvSortingLayerAttribute]
        string sortingLayer = "Default";

        [SerializeField]
        int sortingOrder = 0;

        [SerializeField]
        uint renderingLayerMask = 0xffffffff;

        [SerializeField]
        Color fogColor = Color.white;

        [SerializeField]
        FogBlendMode blendType = FogBlendMode.AlphaBlend;

        [SerializeField]
        FogMeshType fogMeshType = FogMeshType.Default;

        [SerializeField]
        Mesh customMesh = null;

        [SerializeField]
        FogClipping fogClipping = FogClipping.None;

        [SerializeField]
        FogRenderMode renderMode = FogRenderMode.Default;

        [SerializeField]
        bool affectedByLights = false;

        [SerializeField, Range(0.1f, 2.0f)]
        float lightScatteringFactor = 1.0f;

        [SerializeField, Range(0.0f, 1.0f)]
        float lightReflectivity = 1.0f;

        [SerializeField, Range(0.0f, 1.0f)]
        float lightTransmission = 0.5f;

        [SerializeField]
        FogDebugMode debugMode = FogDebugMode.None;

        VolumeFogGroup fogGroup = null;

        Renderer myRenderer = null;
        MeshFilter myFilter = null;
        MaterialPropertyBlock props = null;
        bool isPropsDirty = true;

        Mesh plane = null;

        Mesh renderMesh = null;
        bool renderMeshIsProcedural = false;

        Bounds rendererLocalBounds;

        Transform myTransform;
        Vector3 myPosition;

        Vector3 boundsOffset = Vector3.zero;

        float globalScaleFactor = 1.0f;

        private void Start()
        {
            SetupComponents();

            if (myTransform == null)
            {
                myTransform = transform;
                myPosition = transform.position;
            }

            PrepareFogObject();

            myRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

#if UNITY_EDITOR
            Material mat = GetCachedMaterial(this);
            if (myRenderer.sharedMaterial != mat)
            {
                myRenderer.sharedMaterial = mat;
            }
#endif

            rendererLocalBounds = myRenderer.localBounds;

            Camera.onPostRender += CameraFinishedRendering;
        }

        void SetupComponents()
        {
            if (myFilter == null)
            {
                myFilter = GetComponent<MeshFilter>();

                if (myFilter == null)
                {
                    myFilter = gameObject.AddComponent<MeshFilter>();
                }
                myFilter.hideFlags = HideFlags.HideInInspector;
            }

            if (myRenderer == null)
            {
                myRenderer = GetComponent<Renderer>();

                if (myRenderer == null)
                {
                    myRenderer = gameObject.AddComponent<MeshRenderer>();
                }
                myRenderer.hideFlags = HideFlags.HideInInspector;
            }
        }

        internal void DestroyAsset(Object assetObject)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(assetObject);
            }
            else
#endif
            {
                Destroy(assetObject);
            }
        }

        internal void PrepareFogObject()
        {
            if (IsUsingDepthWorkflow())
            {
                FogDepth = _fogDepth;
            }
            else
            {
                FogDepth = fogMax - fogMin;
            }

            if (fogGroup != null)
            {
                VolumeFogGroup parentGroup = GetComponentInParent<VolumeFogGroup>();
                if (parentGroup == null)
                {
                    TryUnegisterFromGroup();
                }
                else if (parentGroup != fogGroup)
                {
                    TryUnegisterFromGroup();
                    TryRegisterInGroup();
                }
            }

            SetupComponents();

#if UNITY_EDITOR
            Material mat = GetCachedMaterial(this);
            if (myRenderer.sharedMaterial != mat)
            {
                myRenderer.sharedMaterial = mat;
            }
#endif

            if (myTransform == null)
            {
                myTransform = transform;

            }
            myPosition = myTransform.position;

            UpdateScale();

            if (props == null)
            {
                CreateMaterialProps();
            }
            else
            {
                UpdateMaterialProps();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += _DelayedValidate;
#else
            _DelayedValidate();
#endif
        }

        private void OnValidate()
        {
            PrepareFogObject();

            UpdateScale();
        }

        internal void _DelayedValidate()
        {
            UpdateRenderMesh();
        }

        public bool IsCustomMesh()
        {
            return fogMeshType == FogMeshType.Custom;
        }

        public bool IsUsingDepthWorkflow()
        {
            return (fogType == FogType.InvertedSpherical) || fogType == FogType.InvertedSphericalXHeight;
        }

        public bool IsSphereShape()
        {
            return (fogType == FogType.SphericalPos || fogType == FogType.SphericalDist);
        }

        public bool IsBoxShape()
        {
            return !IsSphereShape();
        }

        public bool IsUsingSecondaryParams()
        {
            return fogType == FogType.HeightXBox || fogType == FogType.HeightXView || fogType == FogType.BoxXView || fogType == FogType.InvertedSphericalXHeight;
        }

        internal void UpdateRenderMesh()
        {
            if (this == null)
            {
                return;
            }

            if (myFilter == null)
            {
                myFilter = GetComponent<MeshFilter>();
            }

            if (renderMesh != null && renderMeshIsProcedural)
            {
                DestroyAsset(renderMesh);
                renderMeshIsProcedural = false;
            }

            if (IsCustomMesh())
            {
                myFilter.sharedMesh = customMesh;
                renderMesh = customMesh;
                worldSize = myRenderer.localBounds.size;
                boundsOffset = myRenderer.localBounds.center;

                UpdateMaterialProps();
            }
            else
            {
                if (IsSphereShape())
                {
                    renderMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
                    myFilter.sharedMesh = renderMesh;
                }
                else if (IsBoxShape())
                {
                    renderMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    myFilter.sharedMesh = renderMesh;
                }
                else
                {
                    InitializeBoxRenderMesh();
                    myFilter.sharedMesh = renderMesh;
                }
                boundsOffset = Vector3.zero;
            }

            rendererLocalBounds = myRenderer.localBounds;
        }


        internal void UpdateGlobalScaleFactor()
        {
            Vector3 parentScale = (transform.parent != null) ? transform.parent.lossyScale : Vector3.one;
            float maxScale = Mathf.Max(Mathf.Max(parentScale.x, parentScale.y), parentScale.z);
            float minScale = Mathf.Min(Mathf.Min(parentScale.x, parentScale.y), parentScale.z);

            globalScaleFactor = (minScale + (maxScale - minScale) * 0.5f);
        }

        internal void UpdateScale()
        {
            UpdateGlobalScaleFactor();

            if (IsCustomMesh())
            {
                transform.localScale = Vector3.one;
                // lock scale for custom mesh
            }
            else
            {
                if (IsSphereShape())
                {
                    transform.localScale = Vector3.one * Mathf.Max(fogMin, fogMax) * 2.0f;
                }
                else if (IsBoxShape())
                {
                    transform.localScale = worldSize;
                }
                else
                {
                    transform.localScale = Vector3.one;
                }
            }
        }
        internal void UpdateTransformScaleChanges()
        {
            if (IsCustomMesh())
            {
                transform.localScale = Vector3.one;
                // lock scale for custom mesh
            }
            else
            {
                if (IsSphereShape())
                {
                    float currentScale = Mathf.Max(fogMin, fogMax) * 2.0f;
                    bool changed = false;
                    float r = fogMin / fogMax;
                    if (transform.localScale.x != currentScale)
                    { 
                        fogMax = transform.localScale.x * 0.5f;
                        transform.localScale = Vector3.one * transform.localScale.x;
                        changed = true;
                    }
                    if (transform.localScale.y != currentScale)
                    {
                        fogMax = transform.localScale.y * 0.5f;
                        transform.localScale = Vector3.one * transform.localScale.y;
                        changed = true;
                    }
                    if (transform.localScale.z != currentScale)
                    {
                        fogMax = transform.localScale.z * 0.5f;
                        transform.localScale = Vector3.one * transform.localScale.z;
                        changed = true;
                    }

                    if (changed)
                    {
                        fogMin = r * fogMax;
                        UpdateGlobalScaleFactor();
                        UpdateMaterialProps();
                    }
                }
                else if (IsBoxShape())
                {
                    if (worldSize != transform.localScale)
                    {
                        worldSize = transform.localScale;

                        UpdateGlobalScaleFactor();
                        UpdateMaterialProps();
                    }
                }
                else
                {
                    transform.localScale = Vector3.one;
                }
            }
        }

        internal void CreateMaterialProps()
        {
            props = new MaterialPropertyBlock();
            UpdateMaterialProps();
        }

        internal void UpdateMaterialProps()
        {
            props.SetColor("_Color", GetFogColor());
            props.SetVector("_WorldSize", worldSize * globalScaleFactor);
            if (IsCustomMesh())
            {
                props.SetVector("_LocalSize", worldSize); //worldSize stores local bounds size for custom mesh
                props.SetVector("_LocalOffset", boundsOffset);
            }
            props.SetFloat("_FogMin", fogMin * globalScaleFactor);
            props.SetFloat("_FogMax", fogMax * globalScaleFactor);
            props.SetFloat("_FogFalloff", GetFogFalloffParam());

            if (IsUsingSecondaryParams())
            {
                props.SetFloat("_SecFogMin", secFogMin * globalScaleFactor);
                props.SetFloat("_SecFogMax", secFogMax * globalScaleFactor);
            }

            if (IsAffectedByLights())
            {
                props.SetFloat("_LightScatteringFactor", GetLightScatteringFactor());
                props.SetFloat("_LightReflectivity", GetLightReflectivity());
                props.SetFloat("_LightTransmission", GetLightTransmission());
            }

            isPropsDirty = true;
        }

        internal void TryRegisterInGroup()
        {
            if (fogGroup == null)
            {
                fogGroup = GetComponentInParent<VolumeFogGroup>();
                if (fogGroup)
                {
                    fogGroup.RegisterFogObject(this);
                }
            }
        }
        internal void TryUnegisterFromGroup()
        {
            if (fogGroup)
            {
                fogGroup.UnregisterFogObject(this);
                fogGroup = null;
            }
        }

        private void OnEnable()
        {
            SetupComponents();

            TryRegisterInGroup();

            myRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Camera.onPostRender -= CameraFinishedRendering;
            Camera.onPostRender += CameraFinishedRendering;

            if (props == null)
            {
                CreateMaterialProps();
            }
            else
            {
                UpdateMaterialProps();
            }
        }

        private void OnDisable()
        {
            TryUnegisterFromGroup();

            Camera.onPostRender -= CameraFinishedRendering;
        }

        void OnDestroy()
        {
            if (plane)
            {
                DestroyAsset(plane);
                plane = null;
            }

            if (renderMesh != null && renderMeshIsProcedural)
            {
                DestroyAsset(renderMesh);
                renderMeshIsProcedural = false;
            }

            if (fogGroup)
            {
                fogGroup.UnregisterFogObject(this);
                fogGroup = null;
            }

            props = null;
            myRenderer = null;

            Camera.onPostRender -= CameraFinishedRendering;
        }

        public void OnDidApplyAnimationProperties()
        {
            if (props != null)
            {
                UpdateMaterialProps();
            }
        }

        private void OnTransformParentChanged()
        {
            if (props == null)
            {
                return;
            }

            UpdateScale();
            UpdateMaterialProps();
        }

        private void Update()
        {

        }

        internal bool RenderCameraPlane(Camera camera)
        {
            if (IsSphereShape())
            {
                //TODO: optimize this
                float nearOffset = (camera.nearClipPlane + Z_PLANE_OFFSET) / Mathf.Cos(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float radius = Mathf.Max(fogMin, fogMax) * globalScaleFactor + nearOffset;
                Vector3 distVec = transform.position - camera.transform.position;
                if (distVec.sqrMagnitude < radius * radius)
                {
                    return true;
                }
            }
            else
            {
                Bounds b = myRenderer.bounds;
                b.extents += Vector3.one * ((camera.nearClipPlane + Z_PLANE_OFFSET) / Mathf.Cos(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
                return b.Contains(camera.transform.position);
            }

            return false;
        }

        private void LateUpdate()
        {
            UpdateParams();
        }

        private void OnWillRenderObject()
        {
            UpdateParams();

            if (!Application.isPlaying)
            {
                UpdateTransformScaleChanges();
            }

            Camera camera = VolumeFogManager.instance.GetCurrentRenderingCamera();

            if (camera)
            {
                if ((camera.depthTextureMode & DepthTextureMode.Depth) == 0)
                {
                    camera.depthTextureMode |= DepthTextureMode.Depth;
                }

                if (RenderCameraPlane(camera))
                {
                    if (props == null)
                    {
                        CreateMaterialProps();
                    }

                    if (plane == null)
                    {
                        InitializeCameraMesh();
                    }

                    UpdateMesh(camera);

                    myFilter.sharedMesh = plane;

                }
                else
                {
                    myFilter.sharedMesh = renderMesh;
                }
            }
        }

        private void CameraFinishedRendering(Camera cam)
        {

        }

        void InitializeBoxRenderMesh()
        {
            Vector3 extents = worldSize * 0.5f;
            renderMesh = new Mesh();
            renderMesh.name = "ProceduralBox_" + worldSize.x + "_" + worldSize.y + "_" + worldSize.z;

            Vector3[] c = new Vector3[8];
            c[0] = new Vector3(-extents.x, -extents.y, extents.z);
            c[1] = new Vector3(extents.x, -extents.y, extents.z);
            c[2] = new Vector3(extents.x, -extents.y, -extents.z);
            c[3] = new Vector3(-extents.x, -extents.y, -extents.z);

            c[4] = new Vector3(-extents.x, extents.y, extents.z);
            c[5] = new Vector3(extents.x, extents.y, extents.z);
            c[6] = new Vector3(extents.x, extents.y, -extents.z);
            c[7] = new Vector3(-extents.x, extents.y, -extents.z);

            Vector3[] vertices = new Vector3[24]
            {
                c[0], c[1], c[2], c[3], // Bottom
	            c[7], c[4], c[0], c[3], // Left
	            c[4], c[5], c[1], c[0], // Front
	            c[6], c[7], c[3], c[2], // Back
	            c[5], c[6], c[2], c[1], // Right
	            c[7], c[6], c[5], c[4]  // Top
            };

            renderMesh.vertices = vertices;
            int[] tris = new int[]
            {
                3, 1, 0,        3, 2, 1,        // Bottom	
	            7, 5, 4,        7, 6, 5,        // Left
	            11, 9, 8,       11, 10, 9,      // Front
	            15, 13, 12,     15, 14, 13,     // Back
	            19, 17, 16,     19, 18, 17,	    // Right
	            23, 21, 20,     23, 22, 21,     // Top
            };

            renderMesh.triangles = tris;

            Vector3 up = Vector3.up;
            Vector3 down = Vector3.down;
            Vector3 forward = Vector3.forward;
            Vector3 back = Vector3.back;
            Vector3 left = Vector3.left;
            Vector3 right = Vector3.right;

            Vector3[] normals = new Vector3[]
            {
                down, down, down, down,             // Bottom
	            left, left, left, left,             // Left
	            forward, forward, forward, forward,	// Front
	            back, back, back, back,             // Back
	            right, right, right, right,         // Right
	            up, up, up, up                      // Top
            };

            renderMesh.normals = normals;

            Vector2 uv00 = new Vector2(0f, 0f);
            Vector2 uv10 = new Vector2(1f, 0f);
            Vector2 uv01 = new Vector2(0f, 1f);
            Vector2 uv11 = new Vector2(1f, 1f);

            Vector2[] uv = new Vector2[]
            {
                uv11, uv01, uv00, uv10, // Bottom
	            uv11, uv01, uv00, uv10, // Left
	            uv11, uv01, uv00, uv10, // Front
	            uv11, uv01, uv00, uv10, // Back	        
	            uv11, uv01, uv00, uv10, // Right 
	            uv11, uv01, uv00, uv10  // Top
            };

            renderMesh.uv = uv;
            renderMesh.RecalculateBounds();
            renderMesh.Optimize();
            renderMesh.MarkDynamic();

            renderMeshIsProcedural = true;
        }

        void UpdateBoxRenderMesh()
        {
            Vector3 extents = worldSize * 0.5f;

            Vector3[] c = new Vector3[8];
            c[0] = new Vector3(-extents.x, -extents.y, extents.z);
            c[1] = new Vector3(extents.x, -extents.y, extents.z);
            c[2] = new Vector3(extents.x, -extents.y, -extents.z);
            c[3] = new Vector3(-extents.x, -extents.y, -extents.z);

            c[4] = new Vector3(-extents.x, extents.y, extents.z);
            c[5] = new Vector3(extents.x, extents.y, extents.z);
            c[6] = new Vector3(extents.x, extents.y, -extents.z);
            c[7] = new Vector3(-extents.x, extents.y, -extents.z);

            Vector3[] vertices = new Vector3[24]
            {
                c[0], c[1], c[2], c[3], // Bottom
	            c[7], c[4], c[0], c[3], // Left
	            c[4], c[5], c[1], c[0], // Front
	            c[6], c[7], c[3], c[2], // Back
	            c[5], c[6], c[2], c[1], // Right
	            c[7], c[6], c[5], c[4]  // Top
            };

            renderMesh.vertices = vertices;
        }

        void InitializeCameraMesh()
        {
            plane = new Mesh();
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-1.0f, -1.0f, 0),
                new Vector3(1.0f, -1.0f, 0),
                new Vector3(-1.0f, 1.0f, 0),
                new Vector3(1.0f, 1.0f, 0)
            };
            plane.vertices = vertices;
            int[] tris = new int[6]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };
            plane.triangles = tris;
            Vector3[] normals = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            plane.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(0, 1),
                        new Vector2(1, 1)
            };
            plane.uv = uv;
            plane.bounds = rendererLocalBounds;
            plane.MarkDynamic();
        }

        Vector3[] verticesBuff = new Vector3[4];
        void UpdateMesh(Camera camera)
        {
            float zOffset = Z_PLANE_OFFSET;

            float offsetExpand = zOffset * 100.0f;

            float stereoSeparationX = ((camera.stereoEnabled || XR_ENABLED) ? 3.0f : 0.25f) + offsetExpand;
            float stereoSeparationY = ((camera.stereoEnabled || XR_ENABLED) ? 1.0f : 0.25f) + offsetExpand;

            float near = camera.nearClipPlane + zOffset;

            verticesBuff[0] = camera.ViewportToWorldPoint(new Vector3(-stereoSeparationX, -stereoSeparationY, near));
            verticesBuff[1] = camera.ViewportToWorldPoint(new Vector3(1.0f + stereoSeparationX, -stereoSeparationY, near));
            verticesBuff[2] = camera.ViewportToWorldPoint(new Vector3(-stereoSeparationX, 1.0f + stereoSeparationY, near));
            verticesBuff[3] = camera.ViewportToWorldPoint(new Vector3(1.0f + stereoSeparationX, 1.0f + stereoSeparationY, near));


            verticesBuff[0] = transform.InverseTransformPoint(verticesBuff[0]);
            verticesBuff[1] = transform.InverseTransformPoint(verticesBuff[1]);
            verticesBuff[2] = transform.InverseTransformPoint(verticesBuff[2]);
            verticesBuff[3] = transform.InverseTransformPoint(verticesBuff[3]);

            plane.vertices = verticesBuff;
            plane.bounds = rendererLocalBounds;
        }

        public void SetFogColor(Color color)
        {
            if (fogGroup != null && fogGroup.controlsColor)
            {
                Debug.LogWarning("Setting color for fog object that is controlled by group - this will not take effect");
            }

            fogColor = color;
            props.SetColor("_Color", GetFogColor());
            isPropsDirty = true;
        }

        public FogType GetFogType()
        {
            return fogType;
        }

        public float GetFogFalloffParam()
        {
            return fogGroup != null && fogGroup.controlsFalloffParam ? Mathf.Clamp(fogFalloffCurve * fogGroup.falloffParamMultiplier, 0.5f, 15.0f) : fogFalloffCurve;
        }

        public Vector3 GetFogBoxSize()
        {
            return worldSize;
        }

        public void SetFogBoxSize(Vector3 size)
        {
            worldSize = size;
        }

        public Color GetFogColor()
        {
            return fogGroup != null && fogGroup.controlsColor ? fogGroup.fogColor : fogColor;
        }

        public VolumeFogGroup GetParentFogGroup()
        {
            return fogGroup;
        }

        public void SetAffectedByLights(bool affected) //This will created material instance when used runtime !!!!
        {
            if (fogGroup != null && fogGroup.controlsLighting)
            {
                Debug.LogWarning("Setting affected by lights for fog object that is controlled by group - this will not take effect");
            }

            affectedByLights = affected;

            Material mat = GetCachedMaterial(this);
            if (myRenderer.sharedMaterial != mat)
            {
                myRenderer.sharedMaterial = mat;
            }

            UpdateMaterialProps();
        }

        public bool IsAffectedByLights()
        {
            return fogGroup != null && fogGroup.controlsLighting ? fogGroup.affectedByLights : affectedByLights;
        }

        public float GetLightScatteringFactor()
        {
            return fogGroup != null && fogGroup.controlsLighting ? fogGroup.lightScatteringFactor : lightScatteringFactor;
        }

        public float GetLightReflectivity()
        {
            return fogGroup != null && fogGroup.controlsLighting ? fogGroup.lightReflectivity : lightReflectivity;
        }

        public float GetLightTransmission()
        {
            return fogGroup != null && fogGroup.controlsLighting ? fogGroup.lightTransmission : lightTransmission;
        }

        void UpdateParams()
        {
            if (isPropsDirty)
            {
                myRenderer.SetPropertyBlock(props);
                myRenderer.sortingLayerName = sortingLayer;
                myRenderer.sortingOrder = sortingOrder;
                myRenderer.renderingLayerMask = renderingLayerMask;

                isPropsDirty = false;
            }
        }

        internal string GetMaterialName()
        {
            string ret = "VF";

            ret += IsAffectedByLights() ? "Lit" : "Unlit";

            ret += blendType.ToString();

            ret += fogType.ToString();

            ret += fogFalloffType.ToString();

            ret += renderMode.ToString();

            ret += fogMeshType.ToString();

            ret += fogClipping.ToString();

            if (Internal.fxvFogAssetConfig.ActiveRenderPipeline == Internal.fxvFogAssetConfig.Pipeline.URP)
            {
                ret += "URP";
            }

            return ret;
        }

        public FogDebugMode GetDebugMode()
        {
            return debugMode;
        }

        public void NextDebugMode()
        {
            int intMode = (int)debugMode;
            intMode++;
            if (intMode >= System.Enum.GetNames(typeof(VolumeFog.FogDebugMode)).Length)
            {
                intMode = 0;
            }
            SetDebugMode((VolumeFog.FogDebugMode)intMode);
        }

        public void SetDebugMode(FogDebugMode debugMode)
        {
            this.debugMode = debugMode;

            Renderer r = GetComponent<Renderer>();
            if (r && r.sharedMaterial != null)
            {
                SetupMaterialDebugOptions(r.sharedMaterial, this);
            }
        }

        internal static Dictionary<string, Material> cachedMaterials = new Dictionary<string, Material> ();

        internal static Material GetCachedMaterial(VolumeFog fogObject)
        {
            string materialKey = fogObject.GetMaterialName();

            Material mat = null;

            if (cachedMaterials.TryGetValue(materialKey, out mat))
            {
                if (mat == null)
                {
                    mat = CreateMaterial(fogObject);
                    if (mat)
                    {
                        cachedMaterials[materialKey] = mat;
                    }
                }

                SetupMaterialDebugOptions(mat, fogObject);

                return mat;
            }

#if UNITY_EDITOR
            string cachePath = Internal.fxvFogAssetConfig.AssetPath + "/CreatedResources/MaterialsCache/" + materialKey + ".mat";
            mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(cachePath);
#endif

            if (mat == null)
            {
                mat = CreateMaterial(fogObject);
                if (mat)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        fogObject._DelayedSaveToCache(materialKey, mat);
                    };
#endif

                    cachedMaterials[materialKey] = mat;
                }
            }
            else
            {
                SetupMaterialKeywords(mat, fogObject);

                cachedMaterials[materialKey] = mat;
            }

            SetupMaterialDebugOptions(mat, fogObject);

            return mat;
        }

#if UNITY_EDITOR
        internal void _DelayedSaveToCache(string materialKey, Material mat)
        {
            if (mat == null)
            {
                Debug.Log("Trying to save null material for key " +  materialKey);
                return;
            }

            string saveFolder = Internal.fxvFogAssetConfig.AssetPath + "/CreatedResources/MaterialsCache/";
            if (!System.IO.Directory.Exists(saveFolder))
            {
                System.IO.Directory.CreateDirectory(saveFolder);
            }

            UnityEditor.AssetDatabase.CreateAsset(mat, saveFolder + materialKey + ".mat");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif

        internal static void SetupMaterialKeywords(Material mat, VolumeFog fogObject)
        {
            mat.SetFloat("_FogType", (float)fogObject.fogType);
            var types = System.Enum.GetNames(typeof(VolumeFog.FogType));
            foreach (string name in types)
            {
                mat.DisableKeyword("FXV_FOGTYPE_" + name.ToUpper());
            }
            mat.EnableKeyword("FXV_FOGTYPE_" + fogObject.fogType.ToString().ToUpper());

            mat.SetFloat("_FogFalloffType", (float)fogObject.fogFalloffType);
            if (fogObject.fogFalloffType == VolumeFog.FogFallof.Linear)
            {
                mat.EnableKeyword("FXV_LINEAR_FALLOFF");
            }
            else if (fogObject.fogFalloffType == VolumeFog.FogFallof.Smoothed)
            {
                mat.EnableKeyword("FXV_SMOOTHED_FALLOFF");
            }
            else if (fogObject.fogFalloffType == VolumeFog.FogFallof.Exp)
            {
                mat.EnableKeyword("FXV_EXP_FALLOFF");
            }
            else if (fogObject.fogFalloffType == VolumeFog.FogFallof.Exp2)
            {
                mat.EnableKeyword("FXV_EXP2_FALLOFF");
            }

            mat.SetFloat("_BlendMode", (float)fogObject.blendType);
            if (fogObject.blendType == VolumeFog.FogBlendMode.AlphaBlend)
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
            }
            else if (fogObject.blendType == VolumeFog.FogBlendMode.Add)
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_ZWrite", 0);
            }

            mat.SetFloat("_InAirEnabled", (float)fogObject.renderMode);
            if (fogObject.renderMode == FogRenderMode.Default)
            {
                mat.EnableKeyword("FXV_IN_AIR_FOG");
            }

            mat.SetFloat("_FogMeshType", (float)fogObject.fogMeshType);
            if (fogObject.fogMeshType == FogMeshType.Custom)
            {
                mat.EnableKeyword("FXV_FOG_CUSTOM_MESH");
            }

            mat.SetFloat("_FogClipping", (float)fogObject.fogClipping);
            if (fogObject.fogClipping == FogClipping.ClipToSkybox)
            {
                mat.EnableKeyword("FXV_FOG_CLIP_SKYBOX");
            }
            else if (fogObject.fogClipping == FogClipping.ClipToBounds)
            {
                mat.EnableKeyword("FXV_FOG_CLIP_BOUNDS");
            }

            mat.enableInstancing = true;
        }

        static void SetupMaterialDebugOptions(Material mat, VolumeFog fogObject)
        {
            if (mat != null)
            {
                mat.SetInt("_FogDebugMode", (int)fogObject.debugMode);
            }
        }

        internal static Material CreateMaterial(VolumeFog fogObject)
        {
            Material mat = null;
            string shaderVersion = "";
            if (Internal.fxvFogAssetConfig.ActiveRenderPipeline == Internal.fxvFogAssetConfig.Pipeline.URP)
            {
                shaderVersion += "URP";
            }
            if (fogObject.IsAffectedByLights())
            {
                Shader shader = Shader.Find("FXV/FXVVolumeFogLit" + shaderVersion);

                if (!shader) //this might happen when render pipeline specific package is not imported
                {
                    return null;
                }

                mat = new Material(shader);
            }
            else
            {
                mat = new Material(Shader.Find("FXV/FXVVolumeFog"));
            }

            SetupMaterialKeywords(mat, fogObject);

            return mat;
        }

#if UNITY_EDITOR
        public static void SetFogType(VolumeFog fogObject, FogType type)
        {
            fogObject.fogType = type;

            fogObject.PrepareFogObject();
        }

        public static void UpdateFogType(VolumeFog fogObject)
        {
            fogObject.UpdateRenderMesh();
            fogObject.UpdateScale();
        }

        public static Material SetupFogMaterial(VolumeFog fogObject)
        {
            Material mat = GetCachedMaterial(fogObject);

            Renderer r = fogObject.GetComponent<Renderer>();

            if (r.sharedMaterial != mat)
            {
                r.sharedMaterial = mat;
            }

            return mat;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            if (IsBoxShape())
            { 
                Gizmos.DrawWireCube(transform.position + boundsOffset, worldSize);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position + boundsOffset, fogMax);
            }
        }
#endif
    }
}
