using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.FogDemo
{
    [ExecuteInEditMode]
    public class fxvArrangeChildrenInCircle : MonoBehaviour
    {
        [SerializeField]
        float circleRadius = 31.0f;

        void OnValidate()
        {
            float angle = 0.0f;
            float angleStep = Mathf.PI * 2.0f / (transform.childCount);

            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);

                child.transform.position = transform.position + new Vector3(Mathf.Cos(angle) * circleRadius, 0.0f, Mathf.Sin(angle) * circleRadius);
                child.transform.rotation = Quaternion.Euler(0.0f, -angle * (180.0f / Mathf.PI), 0.0f);
                angle += angleStep;
            }
        }
    }
}
