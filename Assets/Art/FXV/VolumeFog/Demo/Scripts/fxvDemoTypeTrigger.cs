using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.FogDemo
{
    public class fxvDemoTypeTrigger : MonoBehaviour
    {
        [SerializeField]
        private Animator switchAnimator;

        bool isActive = false;

        void Start()
        {

        }

        void Update()
        {
            if (isActive && Input.GetKeyDown(KeyCode.E))
            {
                OnButtonPressed();
            }
        }

        public void OnButtonPressed()
        {
            switchAnimator.Play("ButtonPress", -1, 0.0f);

#if UNITY_2022_2_OR_NEWER
            fxvDemo demo = FindFirstObjectByType<fxvDemo>();
#else
            fxvDemo demo = GameObject.FindObjectOfType<fxvDemo>();
#endif
            demo.NextType();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() != null)
            {
#if UNITY_2022_2_OR_NEWER
                fxvDemo demo = FindFirstObjectByType<fxvDemo>();
#else
                fxvDemo demo = GameObject.FindObjectOfType<fxvDemo>();
#endif
                demo.SetActionTex("[E] Change Type");

                isActive = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<CharacterController>() != null)
            {
#if UNITY_2022_2_OR_NEWER
                fxvDemo demo = FindFirstObjectByType<fxvDemo>();
#else
                fxvDemo demo = GameObject.FindObjectOfType<fxvDemo>();
#endif
                demo.SetActionTex(null);

                isActive = false;
            }
        }
    }
}

