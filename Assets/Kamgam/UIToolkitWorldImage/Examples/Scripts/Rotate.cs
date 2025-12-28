using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.UIToolkitWorldImage.Examples
{
    public partial class Rotate : MonoBehaviour
    {
        public void Update()
        {
            transform.Rotate(Vector3.up, Time.deltaTime * 90f, Space.Self);
        }
    }
}