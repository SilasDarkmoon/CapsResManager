using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    using CoroutineTasks;

    public static partial class ResManager
    {
        public interface IResLoader : ILifetime
        {
            //void OnEnable();
            void BeforeLoadFirstScene();
            void AfterLoadFirstScene();

            Object LoadRes(string asset, Type type);
            void LoadScene(string name, bool additive);

            CoroutineWork LoadResAsync(string asset, Type type);
            IEnumerator LoadSceneAsync(string name, bool additive);

            void UnloadUnusedRes();
            void UnloadAllRes(bool unloadPermanentBundle);
            void MarkPermanent(string assetname);
        }
        private static IResLoader _ResLoader;
        public static IResLoader ResLoader
        {
            get { return _ResLoader; }
            set
            {
                _ResLoader = value;
                AddInitItem(value);
            }
        }
        public interface ISceneDestroyHandler
        {
            void PreDestroy(GameObject[] objs);
            void PostDestroy();
        }
        private static List<ISceneDestroyHandler> _DestroyHandlers = new List<ISceneDestroyHandler>();
        public static List<ISceneDestroyHandler> DestroyHandlers { get { return _DestroyHandlers; } }

        public static Object LoadFromResource(string name, Type type)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var ext = System.IO.Path.GetExtension(name);
                name = name.Substring(0, name.Length - ext.Length);

                string[] distributeFlags = GetValidDistributeFlags();
                if (distributeFlags != null)
                {
                    for (int i = distributeFlags.Length - 1; i >= 0; --i)
                    {
                        var flag = distributeFlags[i];
                        var real = "dist/" + flag + "/" + name;
                        Object loaded;
                        if (type == null)
                        {
                            loaded = Resources.Load(real);
                            if (loaded is Texture2D)
                            {
                                var sprite = Resources.Load<Sprite>(real);
                                if (sprite)
                                {
                                    loaded = sprite;
                                }
                            }
                        }
                        else
                        {
                            loaded = Resources.Load(real, type);
                        }
                        if (loaded)
                        {
                            return loaded;
                        }
                    }
                }
                if (type == null)
                {
                    var loaded = Resources.Load(name);
                    if (loaded is Texture2D)
                    {
                        var sprite = Resources.Load<Sprite>(name);
                        if (sprite)
                        {
                            loaded = sprite;
                        }
                    }
                    return loaded;
                }
                else
                {
                    return Resources.Load(name, type);
                }
            }
            return null;
        }
        public static Object LoadFromResource(string name)
        {
            return LoadFromResource(name, null);
        }
        public static Object LoadRes(string asset, Type type)
        {
            return ResLoader.LoadRes(asset, type);
        }
        public static Object LoadRes(string asset)
        {
            return LoadRes(asset, null);
        }
        public static Object LoadResDeep(string asset, Type type)
        {
            var obj = LoadRes(asset, type);
            if (obj)
            {
                return obj;
            }
            return LoadFromResource(asset, type);
        }
        public static Object LoadResDeep(string asset)
        {
            return LoadResDeep(asset, null);
        }
        public static void LoadScene(string name, bool additive)
        {
            ResLoader.LoadScene(name, additive);
        }
        public static void LoadScene(string name)
        {
            LoadScene(name, false);
        }
        public static void RebuildRuntimeResCache()
        {
            OnRebuildRuntimeResCache();
        }
        public static event Action OnRebuildRuntimeResCache = () => { };

        private static IEnumerator LoadFromResourceWork(CoroutineWork req, string name, Type type)
        {
            if (req.Result != null)
            {
                yield break;
            }
            while (AsyncWorkTimer.Check()) yield return null;
            if (!string.IsNullOrEmpty(name))
            {
                var ext = System.IO.Path.GetExtension(name);
                name = name.Substring(0, name.Length - ext.Length);

                string[] distributeFlags = GetValidDistributeFlags();
                if (distributeFlags != null)
                {
                    for (int i = distributeFlags.Length - 1; i >= 0; --i)
                    {
                        while (AsyncWorkTimer.Check()) yield return null;

                        var flag = distributeFlags[i];
                        ResourceRequest rawreq = null;
                        if (type == null)
                        {
                            try
                            {
                                rawreq = Resources.LoadAsync("dist/" + flag + "/" + name);
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                        else
                        {
                            try
                            {
                                rawreq = Resources.LoadAsync("dist/" + flag + "/" + name, type);
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                        if (rawreq != null)
                        {
                            yield return rawreq;
                            if (rawreq.asset != null)
                            {
                                req.Result = rawreq.asset;
                                if (type == null && rawreq.asset is Texture2D)
                                {
                                    rawreq = null;
                                    try
                                    {
                                        rawreq = Resources.LoadAsync("dist/" + flag + "/" + name, typeof(Sprite));
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                    }
                                    if (rawreq != null)
                                    {
                                        yield return rawreq;
                                        if (rawreq.asset != null)
                                        {
                                            req.Result = rawreq.asset;
                                        }
                                    }
                                }
                                yield break;
                            }
                        }
                    }
                }
                {
                    while (AsyncWorkTimer.Check()) yield return null;
                    ResourceRequest rawreq = null;
                    if (type == null)
                    {
                        try
                        {
                            rawreq = Resources.LoadAsync(name);
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                    else
                    {
                        try
                        {
                            rawreq = Resources.LoadAsync(name, type);
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                    if (rawreq != null)
                    {
                        yield return rawreq;
                        if (rawreq.asset != null)
                        {
                            req.Result = rawreq.asset;
                            if (type == null && rawreq.asset is Texture2D)
                            {
                                rawreq = null;
                                try
                                {
                                    rawreq = Resources.LoadAsync(name, typeof(Sprite));
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                }
                                if (rawreq != null)
                                {
                                    yield return rawreq;
                                    if (rawreq.asset != null)
                                    {
                                        req.Result = rawreq.asset;
                                    }
                                }
                            }
                            yield break;
                        }
                    }
                }
            }
        }
        public static CoroutineWork LoadFromResourceAsync(string name, Type type)
        {
            var work = new CoroutineWorkSingle();
            work.SetWork(LoadFromResourceWork(work, name, type));
            return work;
        }
        public static CoroutineWork LoadFromResourceAsync(string name)
        {
            return LoadFromResourceAsync(name, null);
        }
        public static CoroutineWork LoadResAsync(string name, Type type)
        {
            return ResLoader.LoadResAsync(name, type);
        }
        public static CoroutineWork LoadResAsync(string name)
        {
            return LoadResAsync(name, null);
        }
        public static CoroutineWork LoadResDeepAsync(string name, Type type)
        {
            var queue = new CoroutineWorkQueue();
            queue.AddWork(LoadResAsync(name, type));

            var work = new CoroutineWorkSingle();
            work.SetWork(LoadFromResourceWork(work, name, type));
            queue.AddWork(work);
            return queue;
        }
        public static CoroutineWork LoadResDeepAsync(string name)
        {
            return LoadResDeepAsync(name, null);
        }
        public static IEnumerator LoadSceneAsync(string name, bool additive)
        {
            return ResLoader.LoadSceneAsync(name, additive);
        }
        public static IEnumerator LoadSceneAsync(string name)
        {
            return LoadSceneAsync(name, false);
        }

        public static void UnloadUnusedRes()
        {
            Resources.UnloadUnusedAssets();
            ResLoader.UnloadUnusedRes();
        }
        public static IEnumerator UnloadUnusedResAsync()
        {
            yield return Resources.UnloadUnusedAssets();
            ResLoader.UnloadUnusedRes();
        }
        public static IEnumerator UnloadUnusedResDeep()
        {
            for (int i = 0; i < 3; ++i)
            {
                yield return UnloadUnusedResStep();
            }
        }
        public static IEnumerator UnloadUnusedResStep()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            yield return UnloadUnusedResAsync();
        }
        public static void UnloadAllRes()
        {
            ResLoader.UnloadAllRes(false);
        }
        public static void UnloadAllRes(bool unloadPermanentBundle)
        {
            ResLoader.UnloadAllRes(unloadPermanentBundle);
        }
        public static void MarkPermanent(string assetname)
        {
            ResLoader.MarkPermanent(assetname);
        }

        public static GameObject[] FindAllGameObject()
        {
            var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            UnityEngine.SceneManagement.Scene sceneItem;
            List<GameObject> list = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                sceneItem = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                list.AddRange(sceneItem.GetRootGameObjects());
            }
            return list.ToArray();
        }
        public static void DestroyAllHard()
        {
            UnloadAllRes(true);

            var oldObjs = FindAllGameObject();
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PreDestroy(oldObjs);
            }
            foreach (var obj in oldObjs) GameObject.Destroy(obj);

            var ddols = DontDestroyOnLoadManager.GetAllDontDestroyOnLoadObjs();
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PreDestroy(ddols);
            }
            foreach (var obj in ddols) GameObject.Destroy(obj);

            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PostDestroy();
            }
        }
        public static void DestroyAll()
        {
            UnloadAllRes();

            var oldObjs = FindAllGameObject();
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PreDestroy(oldObjs);
            }
            foreach (var obj in oldObjs) GameObject.Destroy(obj);

            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PostDestroy();
            }
        }

#if COMPATIBLE_RESMANAGER_V1
        public static string CompatibleAssetName(string asset)
        {
            if (asset != null && asset.StartsWith("Assets/CapstonesRes/"))
            {
                return asset.Substring("Assets/CapstonesRes/".Length);
            }
            return asset;
        }
#endif

        private class GarbageCollectorYieldable : CustomYieldInstruction
        {
            public override bool keepWaiting { get { return !_NeedGarbageCollect || System.Environment.TickCount < _NextGarbageCollectTick; } }
        }
        private static GarbageCollectorYieldable _GarbageCollectorIndicator = new GarbageCollectorYieldable();
        private static bool _IsGarbageCollectorRunning = false;
        private static bool _IsGarbageCollectorWorking = false;
        private static int _NextGarbageCollectTick = int.MinValue;
        private static bool _NeedGarbageCollect = false;
        private static IEnumerator CollectGarbageWork()
        {
            try
            {
                yield return _GarbageCollectorIndicator;
                while (true)
                {
                    _NeedGarbageCollect = false;
                    _IsGarbageCollectorWorking = true;
                    int startTick = System.Environment.TickCount;
                    Debug.LogWarning("CollectGarbageWork Begin");
                    for (int j = 0; j < 3; ++j)
                    {
                        for (int i = 0; i < _CollectGarbageFuncs.Count; ++i)
                        {
                            var subwork = _CollectGarbageFuncs[i]();
                            if (subwork != null)
                            {
                                yield return subwork;
                            }
                        }
                    }
                    int finishTick = System.Environment.TickCount;
                    _NextGarbageCollectTick = finishTick + 2 * (finishTick - startTick);
                    _IsGarbageCollectorWorking = false;
                    yield return _GarbageCollectorIndicator;
                }
            }
            finally
            {
                _IsGarbageCollectorWorking = false;
                _IsGarbageCollectorRunning = false;
            }
        }
        public static void StartGarbageCollect()
        {
            _NeedGarbageCollect = true;
            if (!_IsGarbageCollectorRunning)
            {
                _IsGarbageCollectorRunning = true;
                CoroutineRunner.StartCoroutine(CollectGarbageWork());
            }
        }
        public static bool IsCollectingGarbage { get { return _IsGarbageCollectorWorking; } }

        private static readonly List<Func<IEnumerator>> _CollectGarbageFuncs = new List<Func<IEnumerator>>();
        public static event Func<IEnumerator> OnCollectGarbage
        {
            add
            {
                if (value != null)
                {
                    _CollectGarbageFuncs.Add(value);
                }
            }
            remove
            {
                for (int i = 0; i < _CollectGarbageFuncs.Count; ++i)
                {
                    if (_CollectGarbageFuncs[i] == value)
                    {
                        _CollectGarbageFuncs.RemoveAt(i--);
                    }
                }
            }
        }
        public static void InsertCollectGarbageFunc(int pos, Func<IEnumerator> func)
        {
            if (func != null)
            {
                if (pos < 0) pos = 0;
                else if (pos > _CollectGarbageFuncs.Count) pos = _CollectGarbageFuncs.Count;
                _CollectGarbageFuncs.Insert(pos, func);
            }
        }
        public static void DelayGarbageCollectTo(int tick)
        {
            _NextGarbageCollectTick = tick;
        }

        public static event Action OnCollectGarbageLite = () => { Debug.LogWarning("CollectGarbageWork Lite"); };
        public static void StartGarbageCollectLite()
        {
            OnCollectGarbageLite();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void BeforeLoadFirstScene()
        {
            ResLoader.BeforeLoadFirstScene();
            OnCollectGarbage += UnloadUnusedResStep;
            OnCollectGarbageLite += System.GC.Collect;
#if !UNITY_EDITOR
            Application.lowMemory += StartGarbageCollect;
#endif
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AfterLoadFirstScene()
        {
            if (CapsUnityMainBehav.MainBehavInstance == null)
            {
                var inititems = GetInitItems(int.MinValue, int.MaxValue);
                for (int i = 0; i < inititems.Length; ++i)
                {
                    inititems[i].Init();
                }
            }
            ResLoader.AfterLoadFirstScene();
        }
    }
}