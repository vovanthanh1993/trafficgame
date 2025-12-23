using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FXV
{
    [ExecuteInEditMode]
    public class VolumeFogGroup : MonoBehaviour
    {
        [SerializeField]
        internal bool controlsColor = false;

        [SerializeField]
        internal Color fogColor;

        [SerializeField]
        internal bool controlsFalloffParam = false;

        [SerializeField, Range(0.1f, 2.0f)]
        internal float falloffParamMultiplier = 1.0f;

        [SerializeField]
        internal bool controlsLighting = false;

        [SerializeField]
        internal bool affectedByLights = false;

        [SerializeField, Range(0.1f, 2.0f)]
        internal float lightScatteringFactor = 1.0f;

        [SerializeField, Range(0.0f, 1.0f)]
        internal float lightReflectivity = 1.0f;

        [SerializeField, Range(0.0f, 1.0f)]
        internal float lightTransmission = 0.5f;

        List<VolumeFog> controlledFogObjects = new List<VolumeFog>();

        internal void RegisterFogObject(VolumeFog fog)
        {
            controlledFogObjects.Add(fog);
        }

        internal bool UnregisterFogObject(VolumeFog fog)
        {
            return controlledFogObjects.Remove(fog);
        }

        public bool IsControllingColor()
        {
            return controlsColor;
        }

        public bool IsControllingFalloffParam()
        {
            return controlsFalloffParam;
        }

        public bool IsControllingLighting()
        {
            return controlsLighting;
        }

        public bool IsAffectedByLights()
        {
            return affectedByLights;
        }

        void Start()
        {
            if (controlledFogObjects.Count == 0)
            {
                VolumeFog[] fogs = GetComponentsInChildren<VolumeFog>();
                for (int i = 0; i < fogs.Length; i++)
                {
                    fogs[i].TryRegisterInGroup();
                }
            }
        }

        void Update()
        {

        }

        private void OnValidate()
        {
            for (int i = 0; i < controlledFogObjects.Count; i++)
            {
                controlledFogObjects[i].PrepareFogObject();
            }
        }
    }
}
