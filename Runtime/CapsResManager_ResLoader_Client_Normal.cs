﻿using System;
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
            public class AssetInfo_Normal : AssetInfo_Base
            {
                public class AssetRef
                {
                    public WeakReference Asset;
                }

                public Type MainType;
                public Dictionary<Type, AssetRef> TypedAssets = new Dictionary<Type, AssetRef>();

                private Object LoadMainAsset()
                {
                    if (MainType != null)
                    {
                        if (MainType == typeof(object))
                        {
                            return null;
                        }
                        AssetRef rMain;
                        if (TypedAssets.TryGetValue(MainType, out rMain))
                        {
                            if (rMain.Asset != null)
                            {
                                var asset = rMain.Asset.GetWeakReference<Object>();
                                if (asset)
                                {
                                    return asset;
                                }
                            }
                        }
                    }

                    if (ManiItem != null && DepBundles.Count > 0)
                    {
                        var bi = DepBundles[DepBundles.Count - 1];
                        if (bi != null && bi.Bundle != null)
                        {
                            var path = ConcatAssetPath();

                            var asset = bi.Bundle.LoadAsset(path);
                            if (!asset)
                            {
                                MainType = typeof(object);
                                return null;
                            }
                            if (asset is Texture2D)
                            {
                                var sprite = bi.Bundle.LoadAsset(path, typeof(Sprite));
                                if (sprite)
                                {
                                    asset = sprite;
                                }
                            }

                            MainType = asset.GetType();
                            TypedAssets[MainType] = new AssetRef() { Asset = new WeakReference(asset) };
                            return asset;
                        }
                    }
                    return null;
                }
                public override Object Load(Type type)
                {
                    if (MainType == null)
                    {
                        var main = LoadMainAsset();
                        if (MainType == typeof(object) || type == null || type.IsAssignableFrom(MainType))
                        {
                            return main;
                        }
                    }
                    else if (MainType == typeof(object))
                    {
                        return null;
                    }
                    else if (type == null || type.IsAssignableFrom(MainType))
                    {
                        return LoadMainAsset();
                    }

                    AssetRef rAsset;
                    if (TypedAssets.TryGetValue(type, out rAsset))
                    {
                        if (rAsset.Asset != null)
                        {
                            var asset = rAsset.Asset.GetWeakReference<Object>();
                            if (asset)
                            {
                                return asset;
                            }
                        }
                    }

                    if (ManiItem != null && DepBundles.Count > 0)
                    {
                        var bi = DepBundles[DepBundles.Count - 1];
                        if (bi != null && bi.Bundle != null)
                        {
                            var path = ConcatAssetPath();

                            var asset = bi.Bundle.LoadAsset(path, type);

                            TypedAssets[type] = new AssetRef() { Asset = new WeakReference(asset) };
                            return asset;
                        }
                    }
                    return null;
                }

                private IEnumerator LoadMainAssetAsync(CoroutineTasks.CoroutineWork req)
                {
                    if (ManiItem != null && DepBundles.Count > 0)
                    {
                        var bi = DepBundles[DepBundles.Count - 1];
                        if (bi != null && bi.Bundle != null)
                        {
                            var path = ConcatAssetPath();

                            while (AsyncWorkTimer.Check()) yield return null;

                            AssetBundleRequest rawreq = null;
                            try
                            {
                                rawreq = bi.Bundle.LoadAssetAsync(path);
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                            if (rawreq != null)
                            {
                                yield return rawreq;
                                var asset = rawreq.asset;
                                if (asset is Texture2D)
                                {
                                    rawreq = null;
                                    try
                                    {
                                        rawreq = bi.Bundle.LoadAssetAsync(path, typeof(Sprite));
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                    }
                                    if (rawreq != null)
                                    {
                                        yield return rawreq;
                                        if (rawreq.asset)
                                        {
                                            asset = rawreq.asset;
                                        }
                                    }
                                }
                                req.Result = asset;
                            }
                        }
                    }
                }
                public override IEnumerator LoadAsync(CoroutineTasks.CoroutineWork req, Type type)
                {
                    var holdhandle = Hold();
                    try
                    {
                        while (AsyncWorkTimer.Check()) yield return null;

                        if (MainType == null)
                        {
                            var mainwork = new CoroutineTasks.CoroutineWorkSingle();
                            mainwork.SetWork(LoadMainAssetAsync(mainwork));
                            mainwork.StartCoroutine();

                            while (true)
                            {
                                if (MainType != null)
                                {
                                    mainwork.Dispose();
                                    break;
                                }
                                if (mainwork.Done)
                                {
                                    var asset = mainwork.Result as Object;
                                    if (!asset)
                                    {
                                        MainType = typeof(object);
                                    }
                                    else
                                    {
                                        MainType = asset.GetType();
                                        TypedAssets[MainType] = new AssetRef() { Asset = new WeakReference(asset) };
                                    }
                                    if (MainType == typeof(object) || type == null || type.IsAssignableFrom(MainType))
                                    {
                                        req.Result = asset;
                                        yield break;
                                    }
                                }
                            }
                        }
                        if (MainType == typeof(object))
                        {
                            yield break;
                        }
                        else if (type == null || type.IsAssignableFrom(MainType))
                        {
                            type = MainType;
                        }

                        AssetRef rAsset;
                        if (TypedAssets.TryGetValue(type, out rAsset))
                        {
                            if (rAsset.Asset != null)
                            {
                                var asset = rAsset.Asset.GetWeakReference<Object>();
                                if (asset)
                                {
                                    req.Result = asset;
                                    yield break;
                                }
                            }
                        }

                        while (AsyncWorkTimer.Check()) yield return null;
                        if (rAsset == null)
                        {
                            rAsset = new AssetRef();
                            TypedAssets[type] = rAsset;
                        }
                        rAsset.Asset = null;

                        if (ManiItem != null && DepBundles.Count > 0)
                        {
                            var bi = DepBundles[DepBundles.Count - 1];
                            if (bi != null && bi.Bundle != null)
                            {
                                var path = ConcatAssetPath();

                                AssetBundleRequest reqraw = null;
                                try
                                {
                                    reqraw = bi.Bundle.LoadAssetAsync(path, type);
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                }
                                if (reqraw != null)
                                {
                                    yield return reqraw;
                                    var asset = reqraw.asset;

                                    if (!asset)
                                    {
                                        yield break;
                                    }

                                    rAsset.Asset = new WeakReference(asset);
                                    req.Result = asset;
                                }
                            }
                        }
                    }
                    finally
                    {
                        GC.KeepAlive(holdhandle);
                    }
                }
                public override bool CheckRefAlive()
                {
                    foreach (var kvpAsset in TypedAssets)
                    {
                        var rAsset = kvpAsset.Value;
                        if (rAsset.Asset != null)
                        {
                            var asset = rAsset.Asset.GetWeakReference<Object>();
                            if (asset)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }

            public class TypedResLoader_Normal : TypedResLoader_Base
            {
                public override int ResItemType { get { return (int)CapsResManifestItemType.Normal; } }

                protected virtual AssetInfo_Base CreateAssetInfoRaw(CapsResManifestItem item)
                {
                    return new AssetInfo_Normal() { ManiItem = item };
                }

                public override IAssetInfo CreateAssetInfo(CapsResManifestItem item)
                {
                    var ai = item.Attached as IAssetInfo;
                    if (ai == null)
                    {
                        AssetInfo_Base ain = CreateAssetInfoRaw(item);
                        item.Attached = ain;
                        ai = ain;
                    }
                    return ai;
                }
            }
            public static TypedResLoader_Normal Instance_TypedResLoader_Normal = new TypedResLoader_Normal();
        }
    }
}