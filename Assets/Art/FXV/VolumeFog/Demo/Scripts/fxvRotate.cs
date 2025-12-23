using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.FogDemo
{
    public class fxvRotate : MonoBehaviour
    {
        public bool randomRotation = false;

        public Vector3 rotationSpeed = Vector3.up;

        private Vector3 currentRotation;

        void Start()
        {
            currentRotation = transform.rotation.eulerAngles;

            if (randomRotation)
            {
                rotationSpeed = new Vector3(Random.Range(-rotationSpeed.x, rotationSpeed.x), Random.Range(-rotationSpeed.y, rotationSpeed.y), Random.Range(-rotationSpeed.z, rotationSpeed.z));
            }
        }

        void Update()
        {
            currentRotation += rotationSpeed * Time.deltaTime;

            transform.rotation = Quaternion.Euler(currentRotation);
        }
    }
}
