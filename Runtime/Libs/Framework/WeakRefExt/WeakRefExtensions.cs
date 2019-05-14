using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class WeakRefExtensions
    {
        public static T GetWeakReference<T>(this System.WeakReference wr)
        {
            if (wr != null)
            {
                try
                {
                    if (wr.IsAlive)
                    {
                        var obj = wr.Target;
                        if (obj is T)
                        {
                            return (T)obj;
                        }
                    }
                }
                catch { }
            }
            return default(T);
        }
    }
}