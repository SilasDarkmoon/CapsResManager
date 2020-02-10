using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class CapsResInitializer : ScriptableObject
{
    public CapsResInitializer[] SubInitializers;

    public virtual void Init()
    {
        if (SubInitializers != null)
        {
            for (int i = 0; i < SubInitializers.Length; ++i)
            {
                var initializer = SubInitializers[i];
                if (initializer != null)
                {
                    initializer.Init();
                }
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void CheckInit()
    {
    }

    static CapsResInitializer()
    {
        var assets = Resources.LoadAll<CapsResInitializer>("CapsResInitializer");
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; ++i)
            {
                var asset = assets[i];
                if (asset)
                {
                    asset.Init();
                }
            }
        }
    }
}
