using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static partial class ResManager
    {
        public static void HoldRes(this GameObject parent, Object res)
        {
            if (parent && res)
            {
                var holder = parent.GetComponent<CapsResHolder>();
                if (!holder)
                {
                    holder = parent.AddComponent<CapsResHolder>();
                }
                if (holder.ResList == null)
                {
                    holder.ResList = new List<Object>();
                }
                holder.ResList.Add(res);
            }
        }
        public static void HoldRes(this Component parent, Object res)
        {
            if (parent && res)
            {
                HoldRes(parent.gameObject, res);
            }
        }
        private static CapsResHolderEx _ExHolder;
        public static void HoldRes(this object parent, object res)
        {
            if (!_ExHolder)
            {
                var holderobj = new GameObject();
                holderobj.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(holderobj);
                _ExHolder = holderobj.AddComponent<CapsResHolderEx>();
            }
            _ExHolder.AddHolder(parent, res);
        }
    }
}