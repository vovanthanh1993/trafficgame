using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FXV.FogDemo
{
    public class fxvSinPos : MonoBehaviour
    {
        public bool random = false;

        public float amplitude = 2.0f;
        public float speed = 1.0f;

        private Vector3 startPos;

        private float angle = 0.0f;

        Vector3 direction = Vector3.up;

        void Start()
        {
            startPos = transform.position;

            if (random)
            {
                amplitude = Random.Range(0, amplitude);
                speed = Random.Range(0, speed);

                angle = Random.Range(0, Mathf.PI * 2.0f);

                direction = Random.insideUnitSphere.normalized;
            }
        }

        void Update()
        {
            angle += Time.deltaTime * speed;
            if (angle > Mathf.PI * 2.0f)
                angle -= Mathf.PI * 2.0f;

            transform.position = startPos + direction * Mathf.Sin(angle) * amplitude;
        }
    }

}