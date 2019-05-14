using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Capstones.UnityEngineEx
{
    public enum CapsResManifestItemType
    {
        Normal = 0,
        Prefab = 1,
        Scene = 2,
        Redirect = 3,
        //PackedTex = 4,
        //DynTex = 5,
        //DynSprite = 6,
    }
    [Serializable]
    public sealed class CapsResOnDiskManifestItem
    {
        public int Type; // CapsResManifestItemType and Defined by Other Modules
        public int BRef;
        public int Ref;
        public ScriptableObject ExInfo;
    }
    [Serializable]
    public class CapsResOnDiskManifestNode
    {
        public int Level;
        public string PPath;
        public CapsResOnDiskManifestItem Item;
    }
    public class CapsResOnDiskManifest : ScriptableObject
    {
        public string MFlag;
        public string DFlag;
        public bool InMain;

        public string[] Bundles;
        public CapsResOnDiskManifestNode[] Assets;
    }
}
