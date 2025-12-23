using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using FXV.Internal;

namespace FXV.FogDemo
{
    public class fxvDemo : MonoBehaviour
    {
        [SerializeField]
        Image fadeInImage;

        [SerializeField]
        Text toggleText;

        [SerializeField]
        GameObject groupTypesRoot;

        [SerializeField]
        GameObject animationRoot;

        [SerializeField]
        Light mainLight;

        [SerializeField]
        Color fogColorLit = Color.white;

        [SerializeField]
        Color fogColorUnlit = Color.white;

        [SerializeField]
        GameObject[] cameraObjects;

        int currentType = 0;

        float fadeT = 1.0f;

        bool affectedByLights = true;

        void Start()
        {
            if (toggleText)
            {
                toggleText.gameObject.SetActive(false);
            }

            if (fadeInImage)
            {
                fadeInImage.gameObject.SetActive(true);
                fadeInImage.color = Color.black;
                fadeT = 1.5f;
            }
            else
            {
                fadeT = 0.0f;
            }

            if (animationRoot && groupTypesRoot)
            {
                for (int i = 0; i < groupTypesRoot.transform.childCount; i++)
                {
                    fxvFogPresentation fogPres = groupTypesRoot.transform.GetChild(i).GetComponent<fxvFogPresentation>();
                    fogPres.SetLightsFade(0.0f, false);
                }
            }
        }

        public void SetActionTex(string text)
        {
            if (toggleText)
            {
                if (text != null)
                {
                    toggleText.gameObject.SetActive(true);
                    toggleText.text = text;
                }
                else
                {
                    toggleText.gameObject.SetActive(false);
                }
            }
        }

        void Update()
        {
            if (fadeT > 0.0f)
            {
                fadeT -= Time.deltaTime;

                if (fadeT <= 0.0f)
                {
                    fadeT = 0.0f;
                    fadeInImage.gameObject.SetActive(false);
                }

                Color c = Color.black;
                c.a = fadeT;

                fadeInImage.color = c;
            }

            if (animationRoot)
            {
                if (fadeT == 0.0f)
                {
                    Animator anim = animationRoot.GetComponentInChildren<Animator>();
                    if (!anim.enabled)
                    {
                        anim.enabled = true;
                        groupTypesRoot.transform.GetChild(0).GetComponent<fxvFogPresentation>().SetLightsFade(1.0f, 1.5f);
                    }

                    if (Input.GetKeyDown(KeyCode.RightArrow) || anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
                    {
                        NextType();
                    }

                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        SwitchLights();
                    }
                }
            }

            if (cameraObjects != null && cameraObjects.Length > 1)
            {

                if (Input.GetKeyDown(KeyCode.C))
                {
                    for (int i = 0;i < cameraObjects.Length; ++i)
                    {
                        cameraObjects[i].SetActive(!cameraObjects[i].activeSelf);
                    }
                }
            }

#if FXV_VOLUMEFOG_DEBUG
            if (Input.GetKeyDown(KeyCode.D) || Input.GetMouseButtonDown(0))
            {
#if UNITY_2022_2_OR_NEWER
                VolumeFog[] fogs = FindObjectsByType<VolumeFog>(FindObjectsSortMode.None);
#else
                VolumeFog[] fogs = (VolumeFog[])GameObject.FindObjectsOfType(typeof(VolumeFog));
#endif
                for (int i = 0; i < fogs.Length; ++i)
                {
                    fogs[i].NextDebugMode();
                }

            }
#endif
        }

        public void SwitchLights()
        {
            if (groupTypesRoot)
            {
                affectedByLights = !affectedByLights;
                if (mainLight)
                {
                    mainLight.intensity = affectedByLights ? 0.1f : 0.6f;
                }
                for (int i = 0; i < groupTypesRoot.transform.childCount; i++)
                {
                    fxvFogPresentation fogPres = groupTypesRoot.transform.GetChild(i).GetComponent<fxvFogPresentation>();
                    fogPres.SetAffectedByLights(affectedByLights);

                    fogPres.SetFogColor(affectedByLights ? fogColorLit : fogColorUnlit);
                }
            }
        }

        public void NextType()
        {
            if (groupTypesRoot)
            {
                currentType++;

                if (currentType >= groupTypesRoot.transform.childCount)
                {
                    currentType = 0;
                }


                animationRoot.transform.position = groupTypesRoot.transform.GetChild(currentType).position;
                animationRoot.GetComponentInChildren<Animator>().Play("CameraShowcase", -1, 0.0f);

                for (int i = 0; i < groupTypesRoot.transform.childCount; i++)
                {
                    fxvFogPresentation fogPres = groupTypesRoot.transform.GetChild(i).GetComponent<fxvFogPresentation>();
                    if (i == currentType)
                    {
                        fogPres.SetLightsFade(1.0f, 1.5f);
                    }
                    else
                    {
                        fogPres.SetLightsFade(0.0f);
                    }
                }
            }
        }
    }

}