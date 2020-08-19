using System;
using System.Collections;
using System.Collections.Generic;
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
#else
using System.IO.Compression;
#endif
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static partial class ResManager
    {
        private class ResManager_ABLoader : ILifetime
        {
            public ResManager_ABLoader()
            {
                AddInitItem(this);
#if !FORCE_DECOMPRESS_ASSETS_ON_ANDROID
                if (Application.platform == RuntimePlatform.Android)
                {
                    _LoadAssetsFromApk = true;
#if !FORCE_DECOMPRESS_ASSETS_FROM_OBB
                    _LoadAssetsFromObb = true;
#endif
                }
#endif
            }
            public int Order { get { return LifetimeOrders.ABLoader; } }
            public void Prepare()
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    _ObbPath = CrossEvent.TrigClrEvent<string>("GET_MAIN_OBB_PATH");
                }
            }
            public void Init() { }
            public void Cleanup()
            {
                UnloadAllBundle();
                ResManager.ReloadDistributeFlags();
            }
        }
#pragma warning disable 0414
        private static ResManager_ABLoader i_ResManager_ABLoader = new ResManager_ABLoader();
#pragma warning restore

        private static bool _LoadAssetsFromApk;
        public static bool LoadAssetsFromApk
        {
            get { return _LoadAssetsFromApk; }
        }
        private static bool _LoadAssetsFromObb;
        public static bool LoadAssetsFromObb
        {
            get { return _LoadAssetsFromObb; }
        }

        public class AssetBundleInfo
        {
            public AssetBundle Bundle = null;
            public string RealName;
            public int RefCnt = 0;
            public bool Permanent = false;
            public bool LeaveAssetOpen = false;

            public AssetBundleInfo(AssetBundle ab)
            {
                Bundle = ab;
                RefCnt = 0;
            }

            public int AddRef()
            {
                return ++RefCnt;
            }

            public int Release()
            {
                var rv = --RefCnt;
                if (rv <= 0 && !Permanent)
                {
                    UnloadBundle();
                }
                return rv;
            }
            public bool UnloadBundle()
            {
                if (Bundle != null)
                {
                    Bundle.Unload(!LeaveAssetOpen);
                    Bundle = null;
                    return true;
                }
                return false;
            }
        }
        public static Dictionary<string, AssetBundleInfo> LoadedAssetBundles = new Dictionary<string, AssetBundleInfo>();

        public static string GetLoadedBundleRealName(string bundle)
        {
            if (LoadedAssetBundles.ContainsKey(bundle))
            {
                var abi = LoadedAssetBundles[bundle];
                if (abi != null && abi.RealName != null)
                {
                    return abi.RealName;
                }
                return bundle;
            }
            return null;
        }

        public static bool SkipPending = true;
        public static bool SkipUpdate = false;
        public static bool SkipObb = false;
        public static bool SkipPackage = false;
        public static AssetBundleInfo LoadAssetBundle(string name, bool ignoreError)
        {
            return LoadAssetBundle(name, null, ignoreError);
        }
        public static AssetBundleInfo LoadAssetBundle(string name, string norm, bool ignoreError)
        {
            norm = norm ?? name;
            if (string.IsNullOrEmpty(name))
            {
                if (!ignoreError) PlatDependant.LogError("Loading an ab with empty name.");
                return null;
            }
            AssetBundleInfo abi = null;
            if (LoadedAssetBundles.TryGetValue(norm, out abi))
            {
                if (abi == null || abi.Bundle != null)
                {
                    if (abi != null && abi.RealName != null && abi.RealName != name)
                    {
                        //abi.Bundle.Unload(true);
                        //abi.Bundle = null;
                        if (!ignoreError) PlatDependant.LogWarning("Try load duplicated " + norm + ". Current: " + abi.RealName + ". Try: " + name);
                    }
                    //else
                    {
                        if (abi == null)
                        {
                            if (!ignoreError) PlatDependant.LogError("Cannot find (cached)ab: " + norm);
                        }
                        return abi;
                    }
                }
            }
            abi = null;

            AssetBundle bundle = null;
            if (!SkipPending)
            {
                if (PlatDependant.IsFileExist(ThreadSafeValues.UpdatePath + "/pending/res/ver.txt"))
                {
                    string path = ThreadSafeValues.UpdatePath + "/pending/res/" + name;
                    if (PlatDependant.IsFileExist(path))
                    {
                        try
                        {
                            bundle = AssetBundle.LoadFromFile(path);
                        }
                        catch (Exception e)
                        {
                            if (!ignoreError) PlatDependant.LogError(e);
                        }
                    }
                }
            }
            if (!SkipUpdate)
            {
                string path = ThreadSafeValues.UpdatePath + "/res/" + name;
                if (PlatDependant.IsFileExist(path))
                {
                    try
                    {
                        bundle = AssetBundle.LoadFromFile(path);
                    }
                    catch (Exception e)
                    {
                        if (!ignoreError) PlatDependant.LogError(e);
                    }
                }
            }
            if (bundle == null)
            {
                if (Application.streamingAssetsPath.Contains("://"))
                {
                    if (Application.platform == RuntimePlatform.Android && _LoadAssetsFromApk)
                    {
                        var realpath = "res/" + name;
                        if (!SkipObb && _LoadAssetsFromObb && ObbZipArchive != null && ObbEntryType(realpath) == ZipEntryType.Uncompressed)
                        {
                            string path = realpath;
                            int retryTimes = 10;
                            long offset = -1;
                            for (int i = 0; i < retryTimes; ++i)
                            {
                                Exception error = null;
                                do
                                {
                                    ZipArchive za = ObbZipArchive;
                                    if (za == null)
                                    {
                                        if (!ignoreError) PlatDependant.LogError("Obb Archive Cannot be read.");
                                        break;
                                    }
                                    try
                                    {
                                        var entry = za.GetEntry(path);
                                        using (var srcstream = entry.Open())
                                        {
                                            offset = ObbFileStream.Position;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        error = e;
                                        break;
                                    }
                                } while (false);
                                if (error != null)
                                {
                                    if (i == retryTimes - 1)
                                    {
                                        if (!ignoreError) PlatDependant.LogError(error);
                                    }
                                    else
                                    {
                                        if (!ignoreError) PlatDependant.LogError(error);
                                        if (!ignoreError) PlatDependant.LogInfo("Need Retry " + i);
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (offset >= 0)
                            {
                                bundle = AssetBundle.LoadFromFile(ObbPath, 0, (ulong)offset);
                            }
                        }
                        else if (!SkipPackage)
                        {
                            if (!ignoreError || AndroidApkZipArchive != null && AndroidApkZipArchive.GetEntry("assets/res/" + name) != null)
                            {
                                string path = Application.dataPath + "!assets/res/" + name;
                                try
                                {
                                    bundle = AssetBundle.LoadFromFile(path);
                                }
                                catch (Exception e)
                                {
                                    if (!ignoreError) PlatDependant.LogError(e);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!SkipPackage)
                    {
                        string path = Application.streamingAssetsPath + "/res/" + name;
                        if (PlatDependant.IsFileExist(path))
                        {
                            try
                            {
                                bundle = AssetBundle.LoadFromFile(path);
                            }
                            catch (Exception e)
                            {
                                if (!ignoreError) PlatDependant.LogError(e);
                            }
                        }
                    }
                }
            }

            if (bundle != null)
            {
                abi = new AssetBundleInfo(bundle) { RealName = name };
            }
            LoadedAssetBundles[norm] = abi;
            return abi;
        }
        public static AssetBundleInfo LoadAssetBundle(string name)
        {
            return LoadAssetBundle(name, false);
        }
        public static AssetBundleInfo LoadAssetBundle(string mod, string name)
        {
            return LoadAssetBundle(mod, name, null);
        }
        public static AssetBundleInfo LoadAssetBundle(string mod, string name, string norm)
        {
            if (string.IsNullOrEmpty(mod))
            {
                return LoadAssetBundle(name, norm, false);
            }
            else
            {
                return LoadAssetBundle("mod/" + mod + "/" + name, norm, false);
            }
        }

        // TODO: 1、mod and dist? 2、in server?
        public static System.IO.Stream LoadFileInStreaming(string file)
        {
            System.IO.Stream stream = null;
            if (!SkipPending)
            {
                stream = PlatDependant.OpenRead(ThreadSafeValues.UpdatePath + "/pending/" + file);
                if (stream != null)
                {
                    return stream;
                }
            }
            if (!SkipUpdate)
            {
                stream = PlatDependant.OpenRead(ThreadSafeValues.UpdatePath + "/" + file);
                if (stream != null)
                {
                    return stream;
                }
            }
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString() && _LoadAssetsFromApk)
                {
                    if (!SkipObb && _LoadAssetsFromObb && ObbZipArchive != null)
                    {
                        int retryTimes = 3;
                        for (int i = 0; i < retryTimes; ++i)
                        {
                            ZipArchive za = ObbZipArchive;
                            if (za == null)
                            {
                                PlatDependant.LogError("Obb Archive Cannot be read.");
                                if (i != retryTimes - 1)
                                {
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                                continue;
                            }

                            try
                            {
                                var entry = za.GetEntry(file);
                                if (entry != null)
                                {
                                    return entry.Open();
                                }
                                break;
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                                if (i != retryTimes - 1)
                                {
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                            }
                        }
                    }
                    if (!SkipPackage)
                    {
                        int retryTimes = 3;
                        for (int i = 0; i < retryTimes; ++i)
                        {
                            ZipArchive za = AndroidApkZipArchive;
                            if (za == null)
                            {
                                PlatDependant.LogError("Apk Archive Cannot be read.");
                                if (i != retryTimes - 1)
                                {
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                                continue;
                            }

                            try
                            {
                                var entry = za.GetEntry("assets/" + file);
                                if (entry != null)
                                {
                                    return entry.Open();
                                }
                                break;
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                                if (i != retryTimes - 1)
                                {
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (!SkipPackage)
                {
                    stream = PlatDependant.OpenRead(ThreadSafeValues.AppStreamingAssetsPath + "/" + file);
                    if (stream != null)
                    {
                        return stream;
                    }
                }
            }
            return null;
        }

        public static string FindUrlInStreaming(string file)
        {
            if (!SkipPending)
            {
                var path = ThreadSafeValues.UpdatePath + "/pending/" + file;
                if (PlatDependant.IsFileExist(path))
                {
                    return path;
                }
            }
            if (!SkipUpdate)
            {
                var path = ThreadSafeValues.UpdatePath + "/" + file;
                if (PlatDependant.IsFileExist(path))
                {
                    return path;
                }
            }
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString() && _LoadAssetsFromApk)
                {
                    if (!SkipObb && _LoadAssetsFromObb && ObbZipArchive != null)
                    {
                        int retryTimes = 3;
                        for (int i = 0; i < retryTimes; ++i)
                        {
                            ZipArchive za = ObbZipArchive;
                            if (za == null)
                            {
                                PlatDependant.LogError("Obb Archive Cannot be read.");
                                if (i != retryTimes - 1)
                                {
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                                continue;
                            }

                            try
                            {
                                var entry = za.GetEntry(file);
                                if (entry != null)
                                {
                                    return "jar:file://" + ObbPath + "!/" + file;
                                }
                                break;
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                                if (i != retryTimes - 1)
                                {
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                            }
                        }
                    }
                    if (!SkipPackage)
                    {
                        int retryTimes = 3;
                        for (int i = 0; i < retryTimes; ++i)
                        {
                            ZipArchive za = AndroidApkZipArchive;
                            if (za == null)
                            {
                                PlatDependant.LogError("Apk Archive Cannot be read.");
                                if (i != retryTimes - 1)
                                {
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                                continue;
                            }

                            try
                            {
                                var entry = za.GetEntry("assets/" + file);
                                if (entry != null)
                                {
                                    return ThreadSafeValues.AppStreamingAssetsPath + "/" + file;
                                }
                                break;
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                                if (i != retryTimes - 1)
                                {
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (!SkipPackage)
                {
                    var path = ThreadSafeValues.AppStreamingAssetsPath + "/" + file;
                    if (PlatDependant.IsFileExist(path))
                    {
                        return path;
                    }
                }
            }
            return null;
        }

        public interface IAssetBundleLoaderEx
        {
            bool LoadAssetBundle(string mod, string name, bool isContainingBundle, out AssetBundleInfo bi);
        }
        public static readonly List<IAssetBundleLoaderEx> AssetBundleLoaderEx = new List<IAssetBundleLoaderEx>();
        public static AssetBundleInfo LoadAssetBundleEx(string mod, string name, bool isContainingBundle)
        {
            for (int i = 0; i < AssetBundleLoaderEx.Count; ++i)
            {
                AssetBundleInfo bi;
                if (AssetBundleLoaderEx[i].LoadAssetBundle(mod, name, isContainingBundle, out bi))
                {
                    return bi;
                }
            }
            return LoadAssetBundle(mod, name);
        }
        public static string[] GetAllBundleNames(string pre)
        {
            pre = pre ?? "";
            var dir = pre;
            if (!pre.EndsWith("/"))
            {
                var index = pre.LastIndexOf('/');
                if (index < 0)
                {
                    dir = "";
                }
                else
                {
                    dir = pre.Substring(0, index);
                }
            }

            HashSet<string> foundSet = new HashSet<string>();
            List<string> found = new List<string>();

            if (!SkipPending)
            {
                if (PlatDependant.IsFileExist(ThreadSafeValues.UpdatePath + "/pending/res/ver.txt"))
                {
                    string resdir = ThreadSafeValues.UpdatePath + "/pending/res/";
                    string path = resdir + dir;
                    var files = PlatDependant.GetAllFiles(path);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var file = files[i].Substring(resdir.Length);
                        if (dir == pre || file.StartsWith(pre))
                        {
                            if (foundSet.Add(file))
                            {
                                found.Add(file);
                            }
                        }
                    }
                }
            }
            if (!SkipUpdate)
            {
                string resdir = ThreadSafeValues.UpdatePath + "/res/";
                string path = resdir + dir;
                var files = PlatDependant.GetAllFiles(path);
                for (int i = 0; i < files.Length; ++i)
                {
                    var file = files[i].Substring(resdir.Length);
                    if (dir == pre || file.StartsWith(pre))
                    {
                        if (foundSet.Add(file))
                        {
                            found.Add(file);
                        }
                    }
                }
            }

            if (Application.streamingAssetsPath.Contains("://"))
            {
                if (Application.platform == RuntimePlatform.Android && _LoadAssetsFromApk)
                {
                    if (!SkipObb && _LoadAssetsFromObb && ObbZipArchive != null)
                    {
                        int retryTimes = 10;
                        for (int i = 0; i < retryTimes; ++i)
                        {
                            Exception error = null;
                            do
                            {
                                ZipArchive za = ObbZipArchive;
                                if (za == null)
                                {
                                    PlatDependant.LogError("Obb Archive Cannot be read.");
                                    break;
                                }
                                try
                                {
                                    var entries = za.Entries;
                                    foreach (var entry in entries)
                                    {
                                        if (entry.CompressedLength == entry.Length)
                                        {
                                            var name = entry.FullName.Substring("res/".Length);
                                            if (name.StartsWith(pre))
                                            {
                                                if (foundSet.Add(name))
                                                {
                                                    found.Add(name);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    error = e;
                                    break;
                                }
                            } while (false);
                            if (error != null)
                            {
                                if (i == retryTimes - 1)
                                {
                                    PlatDependant.LogError(error);
                                }
                                else
                                {
                                    PlatDependant.LogError(error);
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (!SkipPackage)
                    {
                        int retryTimes = 10;
                        for (int i = 0; i < retryTimes; ++i)
                        {
                            Exception error = null;
                            do
                            {
                                ZipArchive za = AndroidApkZipArchive;
                                if (za == null)
                                {
                                    PlatDependant.LogError("Apk Archive Cannot be read.");
                                    break;
                                }
                                try
                                {
                                    var entries = za.Entries;
                                    foreach (var entry in entries)
                                    {
                                        var name = entry.FullName.Substring("assets/res/".Length);
                                        if (name.StartsWith(pre))
                                        {
                                            if (foundSet.Add(name))
                                            {
                                                found.Add(name);
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    error = e;
                                    break;
                                }
                            } while (false);
                            if (error != null)
                            {
                                if (i == retryTimes - 1)
                                {
                                    PlatDependant.LogError(error);
                                }
                                else
                                {
                                    PlatDependant.LogError(error);
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (!SkipPackage)
                {
                    string resdir = Application.streamingAssetsPath + "/res/";
                    string path = resdir + dir;
                    var files = PlatDependant.GetAllFiles(path);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var file = files[i].Substring(resdir.Length);
                        if (dir == pre || file.StartsWith(pre))
                        {
                            if (foundSet.Add(file))
                            {
                                found.Add(file);
                            }
                        }
                    }
                }
            }

            return found.ToArray();
        }
        public static string[] GetAllResManiBundleNames()
        {
            var dir = "mani/";
            HashSet<string> foundSet = new HashSet<string>();
            List<string> found = new List<string>();

            if (!SkipPending)
            {
                if (PlatDependant.IsFileExist(ThreadSafeValues.UpdatePath + "/pending/res/ver.txt"))
                {
                    string resdir = ThreadSafeValues.UpdatePath + "/pending/res/";
                    string path = resdir + dir;
                    var files = PlatDependant.GetAllFiles(path);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var file = files[i].Substring(resdir.Length);
                        if (file.EndsWith(".m.ab"))
                        {
                            if (foundSet.Add(file))
                            {
                                found.Add(file);
                            }
                        }
                    }
                }
            }
            if (!SkipUpdate)
            {
                string resdir = ThreadSafeValues.UpdatePath + "/res/";
                string path = resdir + dir;
                var files = PlatDependant.GetAllFiles(path);
                for (int i = 0; i < files.Length; ++i)
                {
                    var file = files[i].Substring(resdir.Length);
                    if (file.EndsWith(".m.ab"))
                    {
                        if (foundSet.Add(file))
                        {
                            found.Add(file);
                        }
                    }
                }
            }

            if (Application.streamingAssetsPath.Contains("://"))
            {
                if (Application.platform == RuntimePlatform.Android && _LoadAssetsFromApk)
                {
                    if (!SkipObb && _LoadAssetsFromObb && ObbZipArchive != null)
                    {
                        int retryTimes = 10;
                        for (int i = 0; i < retryTimes; ++i)
                        {
                            Exception error = null;
                            do
                            {
                                ZipArchive za = ObbZipArchive;
                                if (za == null)
                                {
                                    PlatDependant.LogError("Obb Archive Cannot be read.");
                                    break;
                                }
                                try
                                {
                                    var indexentry = za.GetEntry("res/index.txt");
                                    if (indexentry == null)
                                    {
                                        var entries = za.Entries;
                                        foreach (var entry in entries)
                                        {
                                            if (entry.CompressedLength == entry.Length)
                                            {
                                                var name = entry.FullName.Substring("res/".Length);
                                                if (name.StartsWith(dir) && name.EndsWith(".m.ab"))
                                                {
                                                    if (foundSet.Add(name))
                                                    {
                                                        found.Add(name);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (var stream = indexentry.Open())
                                        {
                                            using (var sr = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                                            {
                                                while (true)
                                                {
                                                    var line = sr.ReadLine();
                                                    if (line == null)
                                                    {
                                                        break;
                                                    }
                                                    if (line != "")
                                                    {
                                                        var name = dir + line.Trim() + ".m.ab";
                                                        if (foundSet.Add(name))
                                                        {
                                                            found.Add(name);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    error = e;
                                    break;
                                }
                            } while (false);
                            if (error != null)
                            {
                                if (i == retryTimes - 1)
                                {
                                    PlatDependant.LogError(error);
                                }
                                else
                                {
                                    PlatDependant.LogError(error);
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (!SkipPackage)
                    {
                        int retryTimes = 10;
                        for (int i = 0; i < retryTimes; ++i)
                        {
                            Exception error = null;
                            do
                            {
                                ZipArchive za = AndroidApkZipArchive;
                                if (za == null)
                                {
                                    PlatDependant.LogError("Apk Archive Cannot be read.");
                                    break;
                                }
                                try
                                {
                                    var indexentry = za.GetEntry("assets/res/index.txt");
                                    if (indexentry == null)
                                    {
                                        var entries = za.Entries;
                                        foreach (var entry in entries)
                                        {
                                            var name = entry.FullName.Substring("assets/res/".Length);
                                            if (name.StartsWith(dir) && name.EndsWith(".m.ab"))
                                            {
                                                if (foundSet.Add(name))
                                                {
                                                    found.Add(name);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (var stream = indexentry.Open())
                                        {
                                            using (var sr = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                                            {
                                                while (true)
                                                {
                                                    var line = sr.ReadLine();
                                                    if (line == null)
                                                    {
                                                        break;
                                                    }
                                                    if (line != "")
                                                    {
                                                        var name = dir + line.Trim() + ".m.ab";
                                                        if (foundSet.Add(name))
                                                        {
                                                            found.Add(name);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    error = e;
                                    break;
                                }
                            } while (false);
                            if (error != null)
                            {
                                if (i == retryTimes - 1)
                                {
                                    PlatDependant.LogError(error);
                                }
                                else
                                {
                                    PlatDependant.LogError(error);
                                    PlatDependant.LogInfo("Need Retry " + i);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (!SkipPackage)
                {
                    string resdir = Application.streamingAssetsPath + "/res/";
                    string path = resdir + dir;
                    var files = PlatDependant.GetAllFiles(path);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var file = files[i].Substring(resdir.Length);
                        if (file.EndsWith(".m.ab"))
                        {
                            if (foundSet.Add(file))
                            {
                                found.Add(file);
                            }
                        }
                    }
                }
            }

            return found.ToArray();
        }
        
        public static void UnloadUnusedBundle()
        {
            foreach (var kvpb in LoadedAssetBundles)
            {
                var abi = kvpb.Value;
                if (abi != null && !abi.Permanent && abi.RefCnt <= 0)
                {
                    abi.UnloadBundle();
                }
            }
        }
        public static void UnloadAllBundleSoft()
        {
            var newLoadedAssetBundles = new Dictionary<string, AssetBundleInfo>();
            foreach (var abi in LoadedAssetBundles)
            {
                if (abi.Value != null && !abi.Value.Permanent)
                {
                    if (abi.Value.Bundle != null)
                    {
                        abi.Value.Bundle.Unload(false);
                        abi.Value.Bundle = null;
                    }
                }
                else if (abi.Value != null)
                {
                    newLoadedAssetBundles[abi.Key] = abi.Value;
                }
            }
            LoadedAssetBundles = newLoadedAssetBundles;
        }
        public static void UnloadAllBundle()
        {
            foreach (var kvpb in LoadedAssetBundles)
            {
                var abi = kvpb.Value;
                if (abi != null)
                {
                    abi.UnloadBundle();
                }
            }
            LoadedAssetBundles.Clear();
        }
        public static void UnloadNonPermanentBundle()
        {
            var newLoadedAssetBundles = new Dictionary<string, AssetBundleInfo>();
            foreach (var abi in LoadedAssetBundles)
            {
                if (abi.Value != null && !abi.Value.Permanent)
                {
                    abi.Value.UnloadBundle();
                }
                else if (abi.Value != null)
                {
                    newLoadedAssetBundles[abi.Key] = abi.Value;
                }
            }
            LoadedAssetBundles = newLoadedAssetBundles;
        }

        #region Zip Archive on Android APK
        [ThreadStatic] private static System.IO.Stream _AndroidApkFileStream;
        [ThreadStatic] private static ZipArchive _AndroidApkZipArchive;
        public static System.IO.Stream AndroidApkFileStream
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    bool disposed = false;
                    try
                    {
                        if (_AndroidApkFileStream == null)
                        {
                            disposed = true;
                        }
                        else if (!_AndroidApkFileStream.CanSeek)
                        {
                            disposed = true;
                        }
                    }
                    catch
                    {
                        disposed = true;
                    }
                    if (disposed)
                    {
                        _AndroidApkFileStream = null;
                        _AndroidApkFileStream = PlatDependant.OpenRead(ThreadSafeValues.AppDataPath);
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
#endif
                return _AndroidApkFileStream;
            }
        }
        public static ZipArchive AndroidApkZipArchive
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    bool disposed = false;
                    try
                    {
                        if (_AndroidApkZipArchive == null)
                        {
                            disposed = true;
                        }
                        else
                        {
#if !NET_4_6 && !NET_STANDARD_2_0
                            _AndroidApkZipArchive.ThrowIfDisposed();
#else
                            { var entries = _AndroidApkZipArchive.Entries; }
#endif
                            if (_AndroidApkZipArchive.Mode == ZipArchiveMode.Create)
                            {
                                disposed = true;
                            }
                        }
                    }
                    catch
                    {
                        disposed = true;
                    }
                    if (disposed)
                    {
                        _AndroidApkZipArchive = null;
                        _AndroidApkZipArchive = new ZipArchive(AndroidApkFileStream);
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
#endif
                return _AndroidApkZipArchive;
            }
        }

        private static string _ObbPath;
        public static string ObbPath
        {
            get { return _ObbPath; }
        }

        [ThreadStatic] private static System.IO.Stream _ObbFileStream;
        [ThreadStatic] private static ZipArchive _ObbZipArchive;
        public static System.IO.Stream ObbFileStream
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_ObbPath != null)
                {
                    try
                    {
                        bool disposed = false;
                        try
                        {
                            if (_ObbFileStream == null)
                            {
                                disposed = true;
                            }
                            else if (!_ObbFileStream.CanSeek)
                            {
                                disposed = true;
                            }
                        }
                        catch
                        {
                            disposed = true;
                        }
                        if (disposed)
                        {
                            _ObbFileStream = null;
                            _ObbFileStream = PlatDependant.OpenRead(_ObbPath);
                        }
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
                else
                {
                    _ObbFileStream = null;
                }
#endif
                return _ObbFileStream;
            }
        }
        public static ZipArchive ObbZipArchive
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_ObbPath != null && ObbFileStream != null)
                {
                    try
                    {
                        bool disposed = false;
                        try
                        {
                            if (_ObbZipArchive == null)
                            {
                                disposed = true;
                            }
                            else
                            {
#if !NET_4_6 && !NET_STANDARD_2_0
                                _ObbZipArchive.ThrowIfDisposed();
#else
                                { var entries = _ObbZipArchive.Entries; }
#endif
                                if (_ObbZipArchive.Mode == ZipArchiveMode.Create)
                                {
                                    disposed = true;
                                }
                            }
                        }
                        catch
                        {
                            disposed = true;
                        }
                        if (disposed)
                        {
                            _ObbZipArchive = null;
                            _ObbZipArchive = new ZipArchive(ObbFileStream);
                        }
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
                else
                {
                    _ObbZipArchive = null;
                }
#endif
                return _ObbZipArchive;
            }
        }

        public enum ZipEntryType
        {
            NonExist = 0,
            Compressed = 1,
            Uncompressed = 2,
        }
        public static ZipEntryType ObbEntryType(string file)
        {
            ZipEntryType result = ZipEntryType.NonExist;
            if (ObbZipArchive != null)
            {
                int retryTimes = 10;
                for (int i = 0; i < retryTimes; ++i)
                {
                    Exception error = null;
                    do
                    {
                        ZipArchive za = ObbZipArchive;
                        if (za == null)
                        {
                            error = new Exception("Apk Archive Cannot be read.");
                            break;
                        }

                        try
                        {
                            var entry = za.GetEntry(file);
                            if (entry != null)
                            {
                                result = ZipEntryType.Compressed;
                                if (entry.CompressedLength == entry.Length)
                                {
                                    result = ZipEntryType.Uncompressed;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            error = e;
                            break;
                        }
                    } while (false);
                    if (error != null)
                    {
                        if (i == retryTimes - 1)
                        {
                            PlatDependant.LogError(error);
                            throw error;
                        }
                        else
                        {
                            PlatDependant.LogError(error);
                            PlatDependant.LogInfo("Need Retry " + i);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return result;
        }
        public static bool IsFileInObb(string file)
        {
            return ObbEntryType(file) != ZipEntryType.NonExist;
        }
#endregion
    }
}