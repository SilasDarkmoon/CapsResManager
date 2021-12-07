using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    [CreateAssetMenu]
    public class CapsResRef : ScriptableObject
    {
        public Object[] Refs;
    }
}