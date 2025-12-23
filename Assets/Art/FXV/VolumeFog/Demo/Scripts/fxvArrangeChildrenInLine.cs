using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.FogDemo
{
    [ExecuteInEditMode]
    public class fxvArrangeChildrenInLine : MonoBehaviour
    {
        [SerializeField]
        float offset = 15.0f;

        [SerializeField]
        Vector3 direction = Vector3.right;

        void OnValidate()
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);

                child.transform.position = direction.normalized * i * offset;
            }
        }
    }
}
