using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static partial class ResManager
    {
        public partial class ClientResLoader : IResLoader
        {
            public ClientResLoader()
            {
#if !UNITY_EDITOR || FORCE_USE_CLIENT_RESLOADER
                ResLoader = this;
#endif
            }
            //public void OnEnable() { }
            public void BeforeLoadFirstScene() { }
            public void AfterLoadFirstScene() { }

            public static Dictionary<string, AssetBundleManifest> UnityManifests = new Dictionary<string, AssetBundleManifest>();
            public static void DiscardUnityManifests()
            {
                foreach (var kvpMani in UnityManifests)
                {
                    Object.Destroy(kvpMani.Value);
                }
                UnityManifests.Clear();
            }

            public static CapsResManifest CollapsedManifest = new CapsResManifest();
            private static void ParseManifest(string mod, string[] manibundlenames)
            {
                mod = mod ?? "";
                var lmod = mod.ToLower();
                var umanipath = "res";
                if (mod != "")
                {
                    umanipath = "mod/" + mod + "/" + mod;
                }
                var mbinfo = LoadAssetBundle(umanipath, true);
                if (mbinfo != null)
                {
                    if (mbinfo.Bundle != null)
                    {
                        var umanis = mbinfo.Bundle.LoadAllAssets<AssetBundleManifest>();
                        if (umanis != null && umanis.Length > 0)
                        {
                            UnityManifests[mod] = Object.Instantiate<AssetBundleManifest>(umanis[0]);
                        }
                    }
                    mbinfo.AddRef();
                    mbinfo.Release();
                }

                var pre = "mani/m-" + lmod + "-";
                for (int j = 0; j < manibundlenames.Length; ++j)
                {
                    var bundle = manibundlenames[j];
                    if (bundle.StartsWith(pre) && bundle.EndsWith(".m.ab"))
                    {
                        var binfo = LoadAssetBundle(bundle);
                        if (binfo != null)
                        {
                            if (binfo.Bundle != null)
                            {
                                var dmanis = binfo.Bundle.LoadAllAssets<CapsResOnDiskManifest>();
                                if (dmanis != null)
                                {
                                    for (int k = 0; k < dmanis.Length; ++k)
                                    {
                                        var mani = CapsResManifest.Load(dmanis[k]);
                                        CollapsedManifest.MergeManifest(mani);
                                    }
                                }
                            }
                            binfo.AddRef();
                            binfo.Release();
                        }
                    }
                }
            }
            public static void ParseManifest()
            {
                DiscardUnityManifests();
                CollapsedManifest.DiscardAllNodes();

                var manibundlenames = GetAllResManiBundleNames();

                // the mod ""
                ParseManifest("", manibundlenames);

                var dflags = GetValidDistributeFlags();
                for (int i = 0; i < dflags.Length; ++i)
                {
                    ParseManifest(dflags[i], manibundlenames);
                }

                CollapsedManifest.CollapseManifest(dflags);
                CollapsedManifest.TrimExcess();
                ParseBuiltinScenes();
            }

            private static Dictionary<string, int> _BuiltinScenes = new Dictionary<string, int>();
            private static void ParseBuiltinScenes()
            {
                _BuiltinScenes.Clear();
                System.IO.StreamReader sr = null;
                System.IO.Stream stream = null;
                try
                {
                    stream = LoadFileInStreaming("res/builtin-scenes.txt");
                    if (stream != null)
                    {
                        sr = new System.IO.StreamReader(stream);
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            var items = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            if (items != null && items.Length > 0)
                            {
                                var scenepath = items[0];
                                int index = -1;
                                if (items.Length >= 2)
                                {
                                    if (!int.TryParse(items[1], out index))
                                    {
                                        index = -1;
                                    }
                                }
                                _BuiltinScenes[scenepath] = index;
                            }
                        }
                    }
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Dispose();
                    }
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }
            public static bool IsBuiltinScene(string path, out int index)
            {
                return _BuiltinScenes.TryGetValue(path, out index);
            }

            public interface IAssetInfo
            {
                Object Load(Type type);
                IEnumerator LoadAsync(CoroutineTasks.CoroutineWork req, Type type);
                void Unload();
                object Hold();
                void AddRef();
                bool Release();
                bool CheckAlive();
            }
            public abstract class AssetInfo_Base : IAssetInfo
            {
                public CapsResManifestItem ManiItem;
                public List<AssetBundleInfo> DepBundles = new List<AssetBundleInfo>();

                protected int RefCnt = 0;
                protected WeakReference HoldHandle;

                public string ConcatAssetPath()
                {
                    var node = ManiItem.Node;
                    System.Text.StringBuilder sbpath = new System.Text.StringBuilder();
                    while (node.Parent != null)
                    {
                        if (sbpath.Length > 0)
                        {
                            sbpath.Insert(0, '/');
                        }
                        sbpath.Insert(0, node.PPath.ToLower());
                        node = node.Parent;
                    }
                    var path = sbpath.ToString();
                    return path;
                    //var node = ManiItem.Node;
                    //System.Text.StringBuilder sbpath = new System.Text.StringBuilder();
                    //while (node.Parent != null)
                    //{
                    //    if (sbpath.Length > 0)
                    //    {
                    //        ++sbpath.Length;
                    //    }
                    //    sbpath.Length += node.PPath.Length;
                    //    node = node.Parent;
                    //}
                    //node = ManiItem.Node;
                    //int curIndex = sbpath.Length;
                    //while (node.Parent != null)
                    //{
                    //    curIndex -= node.PPath.Length;
                    //    for (int i = node.PPath.Length - 1; i >= 0; --i)
                    //    {
                    //        sbpath[--curIndex] = char.ToLower(node.PPath[i]);
                    //    }
                    //    if (--curIndex >= 0)
                    //    {
                    //        sbpath[curIndex] = '/';
                    //    }
                    //    node = node.Parent;
                    //}
                    //var path = sbpath.ToString();
                    //return path;
                }

                public abstract Object Load(Type type);
                public abstract IEnumerator LoadAsync(CoroutineTasks.CoroutineWork req, Type type);
                public abstract bool CheckRefAlive();
                public virtual void Unload()
                {
                    if (ManiItem.Attached != null)
                    {
                        foreach (var bundle in DepBundles)
                        {
                            bundle.Release();
                        }

                        ManiItem.Attached = null;
                    }
                }
                public object Hold()
                {
                    var handle = HoldHandle.GetWeakReference<object>();
                    if (handle == null)
                    {
                        handle = new object();
                        HoldHandle = new WeakReference(handle);
                    }
                    return handle;
                }
                public void AddRef()
                {
                    ++RefCnt;
                }
                public bool Release()
                {
                    --RefCnt;
                    return CheckAlive();
                }
                public bool CheckAlive()
                {
                    if (HoldHandle != null)
                    {
                        if (HoldHandle.GetWeakReference<object>() == null)
                        {
                            HoldHandle = null;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    if (RefCnt > 0 || CheckRefAlive())
                    {
                        return true;
                    }
                    Unload();
                    return false;
                }
            }

            public interface ITypedResLoader
            {
                IAssetInfo PreloadRes(CapsResManifestItem item);
                IEnumerator PreloadResAsync(CoroutineTasks.CoroutineWork req, CapsResManifestItem item);
            }
            public abstract class TypedResLoader_Base : ITypedResLoader
            {
                public abstract int ResItemType { get; }
                public abstract IAssetInfo PreloadRes(CapsResManifestItem item);
                public abstract IEnumerator PreloadResAsync(CoroutineTasks.CoroutineWork req, CapsResManifestItem item);

                public TypedResLoader_Base()
                {
                    TypedResLoaders = TypedResLoaders ?? new Dictionary<int, ITypedResLoader>();
                    TypedResLoaders[ResItemType] = this;
                }
            }
            public static Dictionary<int, ITypedResLoader> TypedResLoaders;

            public static IAssetInfo PreloadAsset(CapsResManifestItem item)
            {
                var restype = item.Type;
                ITypedResLoader loader;
                if (TypedResLoaders != null && TypedResLoaders.TryGetValue(restype, out loader) && loader != null)
                {
                    return loader.PreloadRes(item);
                }
                else
                {
                    return Instance_TypedResLoader_Normal.PreloadRes(item);
                }
            }

            private static Object LoadAsset(CapsResManifestItem item, Type type)
            {
                var ai = PreloadAsset(item);
                if (ai != null)
                {
                    return ai.Load(type);
                }
                else
                {
                    return null;
                }
            }
            public static Object LoadAsset(string asset, Type type)
            {
#if COMPATIBLE_RESMANAGER_V1
                asset = CompatibleAssetName(asset);
#endif
                CapsResManifestNode node;
                if (CollapsedManifest.TryGetItem(asset, out node) && node.Item != null)
                {
                    return LoadAsset(node.Item, type);
                }
                return null;
            }
            public static void LoadLevel(string name, bool additive)
            {
#if COMPATIBLE_RESMANAGER_V1
                name = CompatibleAssetName(name);
#endif
                CapsResManifestNode node;
                if (CollapsedManifest.TryGetItem(name, out node) && node.Item != null)
                {
                    var sceneitem = node.Item;
                    while (sceneitem.Ref != null)
                    {
                        sceneitem = sceneitem.Ref;
                    }
                    var scenepath = sceneitem.Node.GetFullPath();
                    int sceneindex;
                    if (IsBuiltinScene(scenepath, out sceneindex))
                    {
                        if (sceneindex >= 0)
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneindex, additive ? UnityEngine.SceneManagement.LoadSceneMode.Additive : UnityEngine.SceneManagement.LoadSceneMode.Single);
                        }
                        else
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(System.IO.Path.GetFileNameWithoutExtension(node.PPath), additive ? UnityEngine.SceneManagement.LoadSceneMode.Additive : UnityEngine.SceneManagement.LoadSceneMode.Single);
                        }
                    }
                    else
                    {
                        LoadAsset(node.Item, additive ? typeof(object) : null);
                    }
                }
                else
                {
                    int sceneindex;
                    if (IsBuiltinScene(name, out sceneindex))
                    {
                        if (sceneindex >= 0)
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneindex, additive ? UnityEngine.SceneManagement.LoadSceneMode.Additive : UnityEngine.SceneManagement.LoadSceneMode.Single);
                        }
                        else
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(System.IO.Path.GetFileNameWithoutExtension(name), additive ? UnityEngine.SceneManagement.LoadSceneMode.Additive : UnityEngine.SceneManagement.LoadSceneMode.Single);
                        }
                    }
                }
            }

            public class PreloadResResult
            {
                private IAssetInfo _AssetInfo;
                private object _Holder;

                public IAssetInfo AssetInfo
                {
                    get { return _AssetInfo; }
                    set
                    {
                        _AssetInfo = value;
                        if (value == null)
                        {
                            _Holder = null;
                        }
                        else
                        {
                            _Holder = value.Hold();
                        }
                    }
                }
            }
            private static CoroutineTasks.CoroutineWork PreloadAssetAsync(CapsResManifestItem item)
            {
                var restype = item.Type;
                ITypedResLoader loader;
                if (TypedResLoaders == null || !TypedResLoaders.TryGetValue(restype, out loader) || loader == null)
                {
                    loader = Instance_TypedResLoader_Normal;
                }

                var work = new CoroutineTasks.CoroutineWorkSingle();
                work.SetWork(loader.PreloadResAsync(work, item));
                item.Attached = work;
                work.StartCoroutine();
                return work;
            }
            private static IEnumerator LoadAssetAsyncWork(CoroutineTasks.CoroutineWork req, string asset, Type type)
            {
                while (AsyncWorkTimer.Check()) yield return null;
                while (ResManager.IsCollectingGarbage) yield return null;
                ResManager.DelayGarbageCollectTo(System.Environment.TickCount + 10000);

#if COMPATIBLE_RESMANAGER_V1
                asset = CompatibleAssetName(asset);
#endif
                CapsResManifestNode node;
                if (CollapsedManifest.TryGetItem(asset, out node) && node.Item != null)
                {
                    var sceneitem = node.Item;
                    while (sceneitem.Ref != null)
                    {
                        sceneitem = sceneitem.Ref;
                    }
                    var scenepath = sceneitem.Node.GetFullPath();
                    int sceneindex;
                    if (IsBuiltinScene(scenepath, out sceneindex))
                    {
                        if (sceneindex >= 0)
                        {
                            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneindex, type == null ? UnityEngine.SceneManagement.LoadSceneMode.Single : UnityEngine.SceneManagement.LoadSceneMode.Additive);
                        }
                        else
                        {
                            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(System.IO.Path.GetFileNameWithoutExtension(node.PPath), type == null ? UnityEngine.SceneManagement.LoadSceneMode.Single : UnityEngine.SceneManagement.LoadSceneMode.Additive);
                        }
                        ResManager.DelayGarbageCollectTo(int.MinValue);
                        //req.Result = ???
                        yield break;
                    }
                    var item = node.Item;
                    var ai = item.Attached as IAssetInfo;
                    if (ai != null)
                    {
                        var work = new CoroutineTasks.CoroutineWorkSingle();
                        work.SetWork(ai.LoadAsync(work, type));
                        ResManager.DelayGarbageCollectTo(System.Environment.TickCount + 10000);
                        yield return work;
                        req.Result = work.Result;
                    }
                    else
                    {
                        var loadwork = item.Attached as CoroutineTasks.CoroutineWork;
                        if (loadwork == null)
                        {
                            loadwork = PreloadAssetAsync(item);
                        }

                        while (true)
                        {
                            if (item.Attached is IAssetInfo)
                            {
                                ai = item.Attached as IAssetInfo;
                                if (loadwork.Done)
                                {
                                    var pr = loadwork.Result as PreloadResResult;
                                    if (pr != null && pr.AssetInfo != null)
                                    {
                                        pr.AssetInfo.Unload();
                                    }
                                }
                                else
                                {
                                    loadwork.Dispose();
                                }
                                break;
                            }
                            if (loadwork.Done)
                            {
                                var pr = loadwork.Result as PreloadResResult;
                                if (pr != null && pr.AssetInfo != null)
                                {
                                    ai = pr.AssetInfo;
                                    item.Attached = ai;
                                }
                                else
                                {
                                    ai = null;
                                    item.Attached = null;
                                }
                                break;
                            }
                            ResManager.DelayGarbageCollectTo(System.Environment.TickCount + 10000);
                            yield return null;
                        }

                        if (ai != null)
                        {
                            var work = new CoroutineTasks.CoroutineWorkSingle();
                            work.SetWork(ai.LoadAsync(work, type));
                            ResManager.DelayGarbageCollectTo(System.Environment.TickCount + 10000);
                            yield return work;
                            req.Result = work.Result;
                        }
                    }
                }
                else
                {
                    int sceneindex;
                    if (IsBuiltinScene(asset, out sceneindex))
                    {
                        if (sceneindex >= 0)
                        {
                            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneindex, type == null ? UnityEngine.SceneManagement.LoadSceneMode.Single : UnityEngine.SceneManagement.LoadSceneMode.Additive);
                        }
                        else
                        {
                            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(System.IO.Path.GetFileNameWithoutExtension(asset), type == null ? UnityEngine.SceneManagement.LoadSceneMode.Single : UnityEngine.SceneManagement.LoadSceneMode.Additive);
                        }
                        ResManager.DelayGarbageCollectTo(int.MinValue);
                        //req.Result = ???
                        yield break;
                    }
                }

                ResManager.DelayGarbageCollectTo(int.MinValue);
            }
            public static CoroutineTasks.CoroutineWork LoadAssetAsync(string asset, Type type)
            {
                var work = new CoroutineTasks.CoroutineWorkSingle();
                work.SetWork(LoadAssetAsyncWork(work, asset, type));
                return work;
            }
            public static IEnumerator LoadLevelAsync(string name, bool additive)
            {
                ResManager.StartGarbageCollectLite();
                var work = LoadAssetAsync(name, additive ? typeof(object) : null);
                work.OnDone += ResManager.StartGarbageCollectLite;
                return work;
            }

            public Object LoadRes(string asset, Type type)
            {
                return LoadAsset(asset, type);
            }
            public void LoadScene(string name, bool additive)
            {
                LoadLevel(name, additive);
            }
            public CoroutineTasks.CoroutineWork LoadResAsync(string asset, Type type)
            {
                return LoadAssetAsync(asset, type);
            }
            public IEnumerator LoadSceneAsync(string name, bool additive)
            {
                return LoadLevelAsync(name, additive);
            }
            public int Order { get { return LifetimeOrders.ResLoader; } }
            public void Prepare() { }
            public void Init()
            {
                ParseManifest();
            }
            public void Cleanup()
            {
                UnloadAllRes(true);
                CollapsedManifest.DiscardAllNodes();
                DiscardUnityManifests();
            }
            public void UnloadUnusedRes()
            {
                if (CollapsedManifest != null)
                {
                    foreach (var item in CollapsedManifest.Items)
                    {
                        var ai = item.Attached as IAssetInfo;
                        if (ai != null)
                        {
                            ai.CheckAlive();
                        }
                    }
                }
            }
            public void UnloadAllRes(bool unloadPermanentBundle)
            {
                if (unloadPermanentBundle)
                {
                    if (CollapsedManifest != null)
                    {
                        foreach (var item in CollapsedManifest.Items)
                        {
                            var ai = item.Attached as IAssetInfo;
                            if (ai != null)
                            {
                                ai.Unload();
                            }
                        }
                    }
                }
                else
                {
                    if (CollapsedManifest != null)
                    {
                        foreach (var item in CollapsedManifest.Items)
                        {
                            var ai = item.Attached as AssetInfo_Base;
                            if (ai != null)
                            {
                                var bundles = ai.DepBundles;
                                if (bundles == null || !bundles[bundles.Count - 1].Permanent)
                                {
                                    ai.Unload();
                                }
                            }
                        }
                        foreach (var item in CollapsedManifest.Items)
                        {
                            var ai = item.Attached as IAssetInfo;
                            if (ai != null && !(ai is AssetInfo_Base))
                            {
                                ai.CheckAlive();
                            }
                        }
                    }
                }
            }
            public void MarkPermanent(string assetname)
            {
                if (CollapsedManifest != null)
                {
                    CapsResManifestNode node;
                    CollapsedManifest.TryGetItem(assetname, out node);
                    if (node != null && node.Item != null)
                    {
                        var ai = node.Item.Attached as AssetInfo_Base;
                        if (ai != null)
                        {
                            var bundles = ai.DepBundles;
                            if (bundles != null)
                            {
                                foreach (var bundle in bundles)
                                {
                                    if (bundle != null)
                                    {
                                        bundle.Permanent = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public static ClientResLoader ClientResLoaderInstance = new ClientResLoader();
    }
}