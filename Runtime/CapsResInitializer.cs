using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsResInitializer : ScriptableObject
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    private static class Initializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void CheckInit()
        {
        }
        static Initializer()
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

    public static void CheckInit()
    {
        Capstones.UnityEngineEx.ResManager.AfterLoadFirstScene();
        Initializer.CheckInit();
    }
}
