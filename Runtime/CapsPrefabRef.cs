﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public class CapsPrefabRef : ScriptableObject
    {
        [NonSerialized]
        public object RefHandle;
    }
}