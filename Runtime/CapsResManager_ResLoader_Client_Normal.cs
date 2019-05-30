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

                protected virtual AssetInfo_Base CreateAssetInfo(CapsResManifestItem item)
                {
                    return new AssetInfo_Normal() { ManiItem = item };
                }

                public override IAssetInfo PreloadRes(CapsResManifestItem item)
                {
                    var ai = item.Attached as IAssetInfo;
                    if (ai == null)
                    {
                        var opmod = item.Manifest.MFlag;
                        var mod = opmod;
                        if (item.Manifest.InMain)
                        {
                            mod = "";
                        }

                        string bundle = item.BRef;
                        if (string.IsNullOrEmpty(bundle))
                        {
                            bundle = FormatBundleNameFor(item);
                        }

                        AssetInfo_Base ain = CreateAssetInfo(item);
                        item.Attached = ain;
                        ai = ain;

                        var cabi = LoadAssetBundleEx(mod, bundle);
                        if (cabi != null)
                        {
                            AssetBundleManifest umani;
                            if (UnityManifests.TryGetValue(mod, out umani) && umani)
                            {
                                var deps = umani.GetAllDependencies(bundle);
                                if (deps != null)
                                {
                                    for (int i = 0; i < deps.Length; ++i)
                                    {
                                        var dep = deps[i];
                                        if (dep.EndsWith(".=.ab"))
                                        {
                                            // this special name means the assetbundle should not be dep of other bundle. for example, replaceable font.
                                            continue;
                                        }
                                        var bi = LoadAssetBundleEx(mod, dep);
                                        if (bi != null)
                                        {
                                            bi.AddRef();
                                            ain.DepBundles.Add(bi);
                                        }
                                    }
                                }
                            }

                            cabi.AddRef();
                            ain.DepBundles.Add(cabi);
                        }
                    }
                    return ai;
                }

                public override IEnumerator PreloadResAsync(CoroutineTasks.CoroutineWork req, CapsResManifestItem item)
                {
                    var opmod = item.Manifest.MFlag;
                    var mod = opmod;
                    if (item.Manifest.InMain)
                    {
                        mod = "";
                    }

                    string bundle = item.BRef;
                    if (string.IsNullOrEmpty(bundle))
                    {
                        bundle = FormatBundleNameFor(item);
                    }

                    AssetBundleInfo cabi = null;
                    List<AssetBundleInfo> bundles = new List<AssetBundleInfo>();
                    try
                    {
                        cabi = LoadAssetBundleEx(mod, bundle);
                        if (cabi != null)
                        {
                            cabi.AddRef();

                            while (AsyncWorkTimer.Check()) yield return null;

                            AssetBundleManifest umani;
                            if (UnityManifests.TryGetValue(mod, out umani) && umani)
                            {
                                var deps = umani.GetAllDependencies(bundle);
                                if (deps != null)
                                {
                                    for (int i = 0; i < deps.Length; ++i)
                                    {
                                        var dep = deps[i];
                                        if (dep.EndsWith(".=.ab"))
                                        {
                                            // this special name means the assetbundle should not be dep of other bundle. for example, replaceable font.
                                            continue;
                                        }
                                        var bi = LoadAssetBundleEx(mod, dep);
                                        if (bi != null)
                                        {
                                            bi.AddRef();
                                            bundles.Add(bi);

                                            while (AsyncWorkTimer.Check()) yield return null;
                                        }
                                    }
                                }
                            }


                            AssetInfo_Base ain = CreateAssetInfo(item);
                            ain.DepBundles.AddRange(bundles);
                            ain.DepBundles.Add(cabi);

                            var pr = new PreloadResResult();
                            pr.AssetInfo = ain;
                            req.Result = pr;

                            bundles = null;
                            cabi = null;
                        }
                    }
                    finally
                    {
                        if (bundles != null)
                        {
                            for (int i = 0; i < bundles.Count; ++i)
                            {
                                bundles[i].Release();
                            }
                        }
                        if (cabi != null)
                        {
                            cabi.Release();
                        }
                    }
                }

                public virtual string FormatBundleNameFor(CapsResManifestItem item)
                {
                    return FormatBundleName(item);
                }
                public static string FormatBundleName(CapsResManifestItem item)
                {
                    var node = item.Node;
                    var depth = node.GetDepth();
                    string[] parts = new string[depth];
                    for (int i = depth - 1; i >= 0; --i)
                    {
                        parts[i] = node.PPath;
                        node = node.Parent;
                    }

                    var mod = item.Manifest.MFlag;
                    var dist = item.Manifest.DFlag;
                    var rootdepth = 2; // Assets/CapsRes/
                    if (depth > 2 && parts[1] == "Mods")
                    {
                        rootdepth += 2; // Assets/Mods/XXX/CapsRes/
                    }
                    else if (depth > 1 && parts[0] == "Packages")
                    {
                        rootdepth += 1; // Packages/xx.xx.xx/CapsRes/
                    }
                    if (!string.IsNullOrEmpty(dist))
                    {
                        rootdepth += 2; // .../dist/XXX/
                    }

                    System.Text.StringBuilder sbbundle = new System.Text.StringBuilder();
                    sbbundle.Append("m-");
                    sbbundle.Append((mod ?? "").ToLower());
                    sbbundle.Append("-d-");
                    sbbundle.Append((dist ?? "").ToLower());
                    sbbundle.Append("-");
                    for (int i = rootdepth; i < depth - 1; ++i)
                    {
                        if (i > rootdepth)
                        {
                            sbbundle.Append("-");
                        }
                        sbbundle.Append(parts[i].ToLower());
                    }
                    var filename = item.Node.PPath;
                    if (filename.EndsWith(".unity"))
                    {
                        var sceneName = parts[depth - 1];
                        sceneName = sceneName.Substring(0, sceneName.Length - ".unity".Length);
                        sbbundle.Append("-");
                        sbbundle.Append(sceneName.ToLower());
                        sbbundle.Append(".s");
                    }
                    else if (filename.EndsWith(".prefab"))
                    {
                        sbbundle.Append(".o");
                    }
                    sbbundle.Append(".ab");
                    return sbbundle.ToString();
                }
            }
            public static TypedResLoader_Normal Instance_TypedResLoader_Normal = new TypedResLoader_Normal();
        }
    }
}