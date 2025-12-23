using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FXV.FogDemo
{
    [ExecuteInEditMode]
    public class fxvFogPresentation : MonoBehaviour
    {
        [SerializeField]
        VolumeFog fog;

        TextMesh textMesh;

        float lightsFadeTarget = 1.0f;

        Light[] lights;


        void Start()
        {
            lights = GetComponentsInChildren<Light>();
        }

        void UpdateState()
        {
            if (textMesh == null)
            {
                textMesh = GetComponentInChildren<TextMesh>();
            }

            if (textMesh != null && fog)
            {
                string formatStr = System.Text.RegularExpressions.Regex.Replace(fog.GetFogType().ToString(), "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();

                textMesh.text = "Fog: " + formatStr + " " + (fog.IsAffectedByLights() ? "Lit" : "Unlit");
            }
        }

        private void OnEnable()
        {
            UpdateState();
        }

        private void OnValidate()
        {
            UpdateState();
        }

        public void SetLightsFade(float target, float delay)
        {
            StartCoroutine(_SetLightsFade(target, delay));
        }

        IEnumerator _SetLightsFade(float target, float delay)
        {
            yield return new WaitForSeconds(delay);

            SetLightsFade(target);
        }

        public void SetAffectedByLights(bool affected)
        {
            if (lights == null)
            {
                lights = GetComponentsInChildren<Light>();
            }

            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].gameObject.SetActive(affected);
            }

            fog.SetAffectedByLights(affected);

            UpdateState();
        }

        public void SetFogColor(Color c)
        {
            fog.SetFogColor(c);
        }

        public void SetLightsFade(float target, bool animated = true)
        {
            if (lights == null)
            {
                lights = GetComponentsInChildren<Light>();
            }
            lightsFadeTarget = target;

            if (!animated)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    lights[i].intensity = target;

                    if (lights[i].enabled && lights[i].intensity < 0.01f)
                    {
                        lights[i].enabled = false;
                    }
                    else if (!lights[i].enabled && lights[i].intensity > 0.01f)
                    {
                        lights[i].enabled = true;
                    }
                }
            }
        }

        void Update()
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].intensity = Mathf.MoveTowards(lights[i].intensity, lightsFadeTarget, Time.deltaTime * 2.0f);

                if (lights[i].enabled && lights[i].intensity < 0.01f)
                {
                    lights[i].enabled = false;
                }
                else if (!lights[i].enabled && lights[i].intensity > 0.01f)
                {
                    lights[i].enabled = true;
                }
            }
        }
    }
}
