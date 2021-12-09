using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    [CreateAssetMenu]
    public sealed class CapsResRef : ScriptableObject
    {
        public Object[] Refs;
    }
}