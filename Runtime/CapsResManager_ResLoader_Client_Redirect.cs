using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static partial class ResManager
    {
        public partial class ClientResLoader
        {
            public class AssetInfo_Redirect : IAssetInfo
            {
                public CapsResManifestItem ManiItem;
                public IAssetInfo Real;

                private void UnloadRaw()
                {
                    if (ManiItem.Attached != null)
                    {
                        ManiItem.Attached = null;
                    }
                }
                public Object Load(Type type)
                {
                    if (Real != null)
                    {
                        return Real.Load(type);
                    }
                    return null;
                }
                public IEnumerator LoadAsync(CoroutineTasks.CoroutineWork req, Type type)
                {
                    if (Real != null)
                    {
                        return Real.LoadAsync(req, type);
                    }
                    else
                    {
                        return CoroutineRunner.GetEmptyEnumerator();
                    }
                }
                public void Unload()
                {
                    if (Real != null)
                    {
                        Real.Unload();
                    }
                    UnloadRaw();
                }
                public void AddRef()
                {
                    if (Real != null)
                    {
                        Real.AddRef();
                    }
                }
                public bool Release()
                {
                    bool alive = false;
                    if (Real != null)
                    {
                        alive = Real.Release();
                    }
                    if (!alive)
                    {
                        UnloadRaw();
                    }
                    return alive;
                }
                public bool CheckAlive()
                {
                    bool alive = false;
                    if (Real != null)
                    {
                        alive = Real.CheckAlive();
                    }
                    if (!alive)
                    {
                        UnloadRaw();
                    }
                    return alive;
                }
                public object Hold()
                {
                    if (Real != null)
                    {
                        return Real.Hold();
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public class TypedResLoader_Redirect : TypedResLoader_Base
            {
                public override int ResItemType { get { return (int)CapsResManifestItemType.Redirect; } }

                public override IAssetInfo PreloadRes(CapsResManifestItem item)
                {
                    var ai = item.Attached as IAssetInfo;
                    if (ai == null)
                    {
                        AssetInfo_Redirect ain = new AssetInfo_Redirect() { ManiItem = item };
                        item.Attached = ain;
                        ai = ain;
                        var realitem = item.Ref;
                        if (realitem != null)
                        {
                            var air = PreloadAsset(realitem);
                            if (air != null)
                            {
                                ain.Real = air;
                            }
                        }
                    }
                    return ai;
                }

                public override IEnumerator PreloadResAsync(CoroutineTasks.CoroutineWork req, CapsResManifestItem item)
                {
                    var realitem = item.Ref;
                    if (realitem != null)
                    {
                        var work = PreloadAssetAsync(realitem);
                        var waiter = new CoroutineTasks.CoroutineAwait();
                        waiter.SetWork(work);
                        yield return waiter;
                        var rr = work.Result as PreloadResResult;
                        if (rr != null && rr.AssetInfo != null)
                        {
                            AssetInfo_Redirect ain = new AssetInfo_Redirect() { ManiItem = item };
                            ain.Real = rr.AssetInfo;
                            var pr = new PreloadResResult();
                            pr.AssetInfo = ain;
                            req.Result = pr;
                        }
                    }
                }
            }
            public static TypedResLoader_Redirect Instance_TypedResLoader_Redirect = new TypedResLoader_Redirect();
        }
    }
}