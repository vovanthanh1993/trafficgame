using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.UIToolkitWorldImage
{
    public interface IPrefabInstantiatorForWorldObjectRendererPrefabSource
    {
        List<PrefabInstantiatorForWorldObjectRenderer.PrefabHandle> GetPrefabHandles();
    }
}