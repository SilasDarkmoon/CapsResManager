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
#if DEBUG_OBB_IN_DOWNLOAD_PATH
#if UNITY_ANDROID
                    if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
                    {
                        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
                    }
                    if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
                    {
                        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
                    }
#endif
                    _ObbPath = "/storage/emulated/0/Download/default.obb";
                    var obb2path = "/storage/emulated/0/Download/obb2.obb";
                    _AllObbPaths = new[] { _ObbPath, obb2path };
                    _AllObbNames = new[] { "testobb", "testobb2" };
#else
                    bool hasobb = false;
                    string mainobbpath = null;
                    List<Pack<string, string>> obbs = new List<Pack<string, string>>();

                    using (var stream = LoadFileInStreaming("hasobb.flag.txt"))
                    {
                        if (stream != null)
                        {
                            hasobb = true;

                            string appid = Application.identifier;
                            string obbroot = Application.persistentDataPath;
                            int obbrootindex = obbroot.IndexOf(appid);
                            if (obbrootindex > 0)
                            {
                                obbroot = obbroot.Substring(0, obbrootindex);
                            }
                            obbrootindex = obbroot.LastIndexOf("/Android");
                            if (obbrootindex > 0)
                            {
                                obbroot = obbroot.Substring(0, obbrootindex);
                            }
                            if (!obbroot.EndsWith("/") && !obbroot.EndsWith("\\"))
                            {
                                obbroot += "/";
                            }
                            obbroot += "Android/obb/" + appid + "/";

                            using (var sr = new System.IO.StreamReader(stream))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    var parts = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts != null && parts.Length > 0)
                                    {
                                        var obbname = parts[0];
                                        string obbpath = null;
                                        int obbver = 0;
                                        if (parts.Length > 1)
                                        {
                                            var val = parts[1];
                                            if (!int.TryParse(val, out obbver))
                                            {
                                                obbpath = val;
                                            }
                                        }
                                        if (obbpath == null)
                                        {
                                            if (obbver <= 0)
                                            {
                                                obbver = AppVer;
                                            }
                                            obbpath = obbname + "." + obbver + "." + appid + ".obb";
                                        }
                                        if (!obbpath.Contains("/") && !obbpath.Contains("\\"))
                                        {
                                            obbpath = obbroot + obbpath;
                                        }

                                        if (!PlatDependant.IsFileExist(obbpath))
                                        { // use updatepath as obb path
                                            obbpath = ThreadSafeValues.UpdatePath + "/obb/" + obbname + "." + obbver + ".obb";
                                        }

                                        obbs.Add(new Pack<string, string>(obbname, obbpath));
                                        if (obbname == "main")
                                        {
                                            mainobbpath = obbpath;
                                        }
                                    }
                                }
                            }

                            if (mainobbpath == null)
                            {
                                mainobbpath = obbroot + "main." + AppVer + "." + appid + ".obb";

                                if (!PlatDependant.IsFileExist(mainobbpath))
                                { // use updatepath as obb path
                                    mainobbpath = ThreadSafeValues.UpdatePath + "/obb/main." + AppVer + ".obb";
                                }
                                
                                obbs.Insert(0, new Pack<string, string>("main", mainobbpath));
                            }
                        }
                    }

                    if (hasobb)
                    {
                        _ObbPath = mainobbpath;
                        _AllObbPaths = new string[obbs.Count];
                        _AllObbNames = new string[obbs.Count];
                        for (int i = 0; i < obbs.Count; ++i)
                        {
                            _AllObbPaths[i] = obbs[i].t2;
                            _AllObbNames[i] = obbs[i].t1;
                        }
                    }
                    else
                    {
                        _ObbPath = null;
                        _AllObbPaths = null;
                        _AllObbNames = null;
                    }
#endif
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
            if (bundle == null)
            {
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
            }
            if (bundle == null)
            {
                if (Application.streamingAssetsPath.Contains("://"))
                {
                    if (Application.platform == RuntimePlatform.Android && _LoadAssetsFromApk)
                    {
                        var realpath = "res/" + name;
                        if (!SkipObb && _LoadAssetsFromObb && ObbEntryType(realpath) == ZipEntryType.Uncompressed)
                        {
                            string path = realpath;

                            var allobbs = ResManager.AllObbZipArchives;
                            for (int z = allobbs.Length - 1; z >= 0; --z)
                            {
                                if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                { // means the obb is to be downloaded.
                                    continue;
                                }

                                var zip = allobbs[z];
                                int retryTimes = 10;
                                long offset = -1;
                                for (int i = 0; i < retryTimes; ++i)
                                {
                                    Exception error = null;
                                    do
                                    {
                                        ZipArchive za = zip;
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
                                                offset = ResManager.AllObbFileStreams[z].Position;
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
                                    bundle = AssetBundle.LoadFromFile(ResManager.AllObbPaths[z], 0, (ulong)offset);
                                    break;
                                }
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
        public static bool FindLoadedAssetBundle(string name, string norm, out AssetBundleInfo abi)
        {
            norm = norm ?? name;
            if (string.IsNullOrEmpty(name))
            {
                abi = null;
                return false;
            }
            abi = null;
            if (LoadedAssetBundles.TryGetValue(norm, out abi))
            {
                if (abi == null || abi.Bundle != null)
                {
                    return true;
                }
            }
            abi = null;
            return false;
        }
        public static bool FindLoadedAssetBundle(string mod, string name, string norm, out AssetBundleInfo abi)
        {
            if (string.IsNullOrEmpty(mod))
            {
                return FindLoadedAssetBundle(name, norm, out abi);
            }
            else
            {
                return FindLoadedAssetBundle("mod/" + mod + "/" + name, norm, out abi);
            }
        }

        // TODO: in server?
        public static System.IO.Stream LoadFileInStreaming(string file)
        {
            return LoadFileInStreaming("", file, false, false);
        }
        public static System.IO.Stream LoadFileInStreaming(string prefix, string file, bool variantModAndDist, bool ignoreHotUpdate)
        {
            List<string> allflags;
            if (variantModAndDist)
            {
                var flags = ResManager.GetValidDistributeFlags();
                allflags = new List<string>(flags.Length + 1);
                allflags.Add(null);
                allflags.AddRange(flags);
            }
            else
            {
                allflags = new List<string>(1) { null };
            }

            if (!SkipPending && !ignoreHotUpdate)
            {
                string root = ThreadSafeValues.UpdatePath + "/pending/";
                for (int n = allflags.Count - 1; n >= 0; --n)
                {
                    var dist = allflags[n];
                    for (int m = allflags.Count - 1; m >= 0; --m)
                    {
                        var mod = allflags[m];
                        var moddir = "";
                        if (mod != null)
                        {
                            moddir = "mod/" + mod + "/";
                        }
                        if (dist != null)
                        {
                            moddir += "dist/" + dist + "/";
                        }
                        var path = root + prefix + moddir + file;
                        if (PlatDependant.IsFileExist(path))
                        {
                            return PlatDependant.OpenRead(path);
                        }
                    }
                }
            }
            if (!SkipUpdate && !ignoreHotUpdate)
            {
                string root = ThreadSafeValues.UpdatePath + "/";
                for (int n = allflags.Count - 1; n >= 0; --n)
                {
                    var dist = allflags[n];
                    for (int m = allflags.Count - 1; m >= 0; --m)
                    {
                        var mod = allflags[m];
                        var moddir = "";
                        if (mod != null)
                        {
                            moddir = "mod/" + mod + "/";
                        }
                        if (dist != null)
                        {
                            moddir += "dist/" + dist + "/";
                        }
                        var path = root + prefix + moddir + file;
                        if (PlatDependant.IsFileExist(path))
                        {
                            return PlatDependant.OpenRead(path);
                        }
                    }
                }
            }
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString() && _LoadAssetsFromApk)
                {
                    var allobbs = AllObbZipArchives;
                    if (!SkipObb && _LoadAssetsFromObb && allobbs != null)
                    {
                        for (int n = allflags.Count - 1; n >= 0; --n)
                        {
                            var dist = allflags[n];
                            for (int m = allflags.Count - 1; m >= 0; --m)
                            {
                                var mod = allflags[m];
                                var moddir = "";
                                if (mod != null)
                                {
                                    moddir = "mod/" + mod + "/";
                                }
                                if (dist != null)
                                {
                                    moddir += "dist/" + dist + "/";
                                }
                                var entryname = prefix + moddir + file;

                                for (int z = allobbs.Length - 1; z >= 0; --z)
                                {
                                    if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                    { // means the obb is to be downloaded.
                                        continue;
                                    }
                                    
                                    var zip = allobbs[z];
                                    int retryTimes = 3;
                                    for (int i = 0; i < retryTimes; ++i)
                                    {
                                        ZipArchive za = zip;
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
                                            var entry = za.GetEntry(entryname);
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
                    }
                    if (!SkipPackage)
                    {
                        for (int n = allflags.Count - 1; n >= 0; --n)
                        {
                            var dist = allflags[n];
                            for (int m = allflags.Count - 1; m >= 0; --m)
                            {
                                var mod = allflags[m];
                                var moddir = "";
                                if (mod != null)
                                {
                                    moddir = "mod/" + mod + "/";
                                }
                                if (dist != null)
                                {
                                    moddir += "dist/" + dist + "/";
                                }
                                var entryname = prefix + moddir + file;

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
                                        var entry = za.GetEntry("assets/" + entryname);
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
                }
            }
            else
            {
                if (!SkipPackage)
                {
                    string root = ThreadSafeValues.AppStreamingAssetsPath + "/";
                    for (int n = allflags.Count - 1; n >= 0; --n)
                    {
                        var dist = allflags[n];
                        for (int m = allflags.Count - 1; m >= 0; --m)
                        {
                            var mod = allflags[m];
                            var moddir = "";
                            if (mod != null)
                            {
                                moddir = "mod/" + mod + "/";
                            }
                            if (dist != null)
                            {
                                moddir += "dist/" + dist + "/";
                            }
                            var path = root + prefix + moddir + file;
                            if (PlatDependant.IsFileExist(path))
                            {
                                return PlatDependant.OpenRead(path);
                            }
                        }
                    }
                }
            }
            return null;
        }
        public static string FindUrlInStreaming(string file)
        {
            return FindUrlInStreaming("", file, false, false);
        }
        public static string FindUrlInStreaming(string prefix, string file, bool variantModAndDist, bool ignoreHotUpdate)
        {
            List<string> allflags;
            if (variantModAndDist)
            {
                var flags = ResManager.GetValidDistributeFlags();
                allflags = new List<string>(flags.Length + 1);
                allflags.Add(null);
                allflags.AddRange(flags);
            }
            else
            {
                allflags = new List<string>(1) { null };
            }

            if (!SkipPending && !ignoreHotUpdate)
            {
                string root = ThreadSafeValues.UpdatePath + "/pending/";
                for (int n = allflags.Count - 1; n >= 0; --n)
                {
                    var dist = allflags[n];
                    for (int m = allflags.Count - 1; m >= 0; --m)
                    {
                        var mod = allflags[m];
                        var moddir = "";
                        if (mod != null)
                        {
                            moddir = "mod/" + mod + "/";
                        }
                        if (dist != null)
                        {
                            moddir += "dist/" + dist + "/";
                        }
                        var path = root + prefix + moddir + file;
                        if (PlatDependant.IsFileExist(path))
                        {
                            return path;
                        }
                    }
                }
            }
            if (!SkipUpdate && !ignoreHotUpdate)
            {
                string root = ThreadSafeValues.UpdatePath + "/";
                for (int n = allflags.Count - 1; n >= 0; --n)
                {
                    var dist = allflags[n];
                    for (int m = allflags.Count - 1; m >= 0; --m)
                    {
                        var mod = allflags[m];
                        var moddir = "";
                        if (mod != null)
                        {
                            moddir = "mod/" + mod + "/";
                        }
                        if (dist != null)
                        {
                            moddir += "dist/" + dist + "/";
                        }
                        var path = root + prefix + moddir + file;
                        if (PlatDependant.IsFileExist(path))
                        {
                            return path;
                        }
                    }
                }
            }
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString() && _LoadAssetsFromApk)
                {
                    var allobbs = AllObbZipArchives;
                    if (!SkipObb && _LoadAssetsFromObb && allobbs != null)
                    {
                        for (int n = allflags.Count - 1; n >= 0; --n)
                        {
                            var dist = allflags[n];
                            for (int m = allflags.Count - 1; m >= 0; --m)
                            {
                                var mod = allflags[m];
                                var moddir = "";
                                if (mod != null)
                                {
                                    moddir = "mod/" + mod + "/";
                                }
                                if (dist != null)
                                {
                                    moddir += "dist/" + dist + "/";
                                }
                                var entryname = prefix + moddir + file;

                                for (int z = allobbs.Length - 1; z >= 0; --z)
                                {
                                    if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                    { // means the obb is to be downloaded.
                                        continue;
                                    }
                                    
                                    var zip = allobbs[z];
                                    int retryTimes = 3;
                                    for (int i = 0; i < retryTimes; ++i)
                                    {
                                        ZipArchive za = zip;
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
                                            var entry = za.GetEntry(entryname);
                                            if (entry != null)
                                            {
                                                return "jar:file://" + AllObbPaths[z] + "!/" + entryname;
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
                    }
                    if (!SkipPackage)
                    {
                        for (int n = allflags.Count - 1; n >= 0; --n)
                        {
                            var dist = allflags[n];
                            for (int m = allflags.Count - 1; m >= 0; --m)
                            {
                                var mod = allflags[m];
                                var moddir = "";
                                if (mod != null)
                                {
                                    moddir = "mod/" + mod + "/";
                                }
                                if (dist != null)
                                {
                                    moddir += "dist/" + dist + "/";
                                }
                                var entryname = prefix + moddir + file;

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
                                        var entry = za.GetEntry("assets/" + entryname);
                                        if (entry != null)
                                        {
                                            return ThreadSafeValues.AppStreamingAssetsPath + "/" + entryname;
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
                }
            }
            else
            {
                if (!SkipPackage)
                {
                    string root = ThreadSafeValues.AppStreamingAssetsPath + "/";
                    for (int n = allflags.Count - 1; n >= 0; --n)
                    {
                        var dist = allflags[n];
                        for (int m = allflags.Count - 1; m >= 0; --m)
                        {
                            var mod = allflags[m];
                            var moddir = "";
                            if (mod != null)
                            {
                                moddir = "mod/" + mod + "/";
                            }
                            if (dist != null)
                            {
                                moddir += "dist/" + dist + "/";
                            }
                            var path = root + prefix + moddir + file;
                            if (PlatDependant.IsFileExist(path))
                            {
                                return path;
                            }
                        }
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
            AssetBundleInfo bi;
            if (FindLoadedAssetBundle(mod, name, null, out bi))
            {
                return bi;
            }
            for (int i = 0; i < AssetBundleLoaderEx.Count; ++i)
            {
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
                    if (!SkipObb && _LoadAssetsFromObb)
                    {
                        var allobbs = ResManager.AllObbZipArchives;
                        if (allobbs != null)
                        {
                            for (int z = 0; z < allobbs.Length; ++z)
                            {
                                if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                { // means the obb is to be downloaded.
                                    continue;
                                }

                                var zip = allobbs[z];
                                int retryTimes = 10;
                                for (int i = 0; i < retryTimes; ++i)
                                {
                                    Exception error = null;
                                    do
                                    {
                                        ZipArchive za = zip;
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
                    if (!SkipObb && _LoadAssetsFromObb)
                    {
                        var allobbs = ResManager.AllObbZipArchives;
                        if (allobbs != null)
                        {
                            for (int z = 0; z < allobbs.Length; ++z)
                            {
                                if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                { // means the obb is to be downloaded.
                                    continue;
                                }

                                var zip = allobbs[z];
                                int retryTimes = 10;
                                for (int i = 0; i < retryTimes; ++i)
                                {
                                    Exception error = null;
                                    do
                                    {
                                        ZipArchive za = zip;
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

        public static int GetAppVer()
        {
            int versionCode = CrossEvent.TrigClrEvent<int>("SDK_GetAppVerCode");
            if (versionCode <= 0)
            { // the cross call failed. we parse it from the string like "1.0.0.25"
                var vername = ThreadSafeValues.AppVerName;
                if (!int.TryParse(vername, out versionCode))
                {
                    int split = vername.LastIndexOf(".");
                    if (split > 0)
                    {
                        var verlastpart = vername.Substring(split + 1);
                        int.TryParse(verlastpart, out versionCode);
                    }
                }
            }
            return versionCode;
        }
        private static int? _cached_AppVer;
        public static int AppVer
        {
            get
            {
                if (_cached_AppVer == null)
                {
                    _cached_AppVer = GetAppVer();
                }
                return (int)_cached_AppVer;
            }
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
        private static string[] _AllObbPaths;
        public static string[] AllObbPaths
        {
            get { return _AllObbPaths; }
        }
        private static string[] _AllObbNames;
        public static string[] AllObbNames
        {
            get { return _AllObbNames; }
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
        [ThreadStatic] private static System.IO.Stream[] _AllObbFileStreams;
        [ThreadStatic] private static ZipArchive[] _AllObbZipArchives;
        public static System.IO.Stream[] AllObbFileStreams
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_AllObbPaths != null)
                {
                    if (_AllObbFileStreams == null)
                    {
                        _AllObbFileStreams = new System.IO.Stream[_AllObbPaths.Length];
                    }
                    for (int i = 0; i < _AllObbFileStreams.Length; ++i)
                    {
                        try
                        {
                            bool disposed = false;
                            try
                            {
                                if (_AllObbFileStreams[i] == null)
                                {
                                    disposed = true;
                                }
                                else if (!_AllObbFileStreams[i].CanSeek)
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
                                _AllObbFileStreams[i] = null;
                                _AllObbFileStreams[i] = PlatDependant.OpenRead(_AllObbPaths[i]);
                            }
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                else
                {
                    _AllObbFileStreams = null;
                }
#endif
                return _AllObbFileStreams;
            }
        }
        public static ZipArchive[] AllObbZipArchives
        {
            get
            {
                var filestreams = AllObbFileStreams;
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_AllObbPaths != null && filestreams != null)
                {
                    if (_AllObbZipArchives == null)
                    {
                        _AllObbZipArchives = new ZipArchive[filestreams.Length];
                    }
                    for (int i = 0; i < _AllObbZipArchives.Length; ++i)
                    {
                        try
                        {
                            bool disposed = false;
                            try
                            {
                                if (_AllObbZipArchives[i] == null)
                                {
                                    disposed = true;
                                }
                                else
                                {
#if !NET_4_6 && !NET_STANDARD_2_0
                                    _AllObbZipArchives[i].ThrowIfDisposed();
#else
                                    { var entries = _AllObbZipArchives[i].Entries; }
#endif
                                    if (_AllObbZipArchives[i].Mode == ZipArchiveMode.Create)
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
                                _AllObbZipArchives[i] = null;
                                _AllObbZipArchives[i] = new ZipArchive(filestreams[i]);
                            }
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                else
                {
                    _AllObbZipArchives = null;
                }
#endif
                return _AllObbZipArchives;
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
            var allarchives = AllObbZipArchives;
            if (allarchives != null)
            {
                for (int n = allarchives.Length - 1; n >= 0; --n)
                {
                    if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[n]))
                    { // means the obb is to be downloaded.
                        continue;
                    }

                    var archive = allarchives[n];
                    int retryTimes = 10;
                    for (int i = 0; i < retryTimes; ++i)
                    {
                        Exception error = null;
                        do
                        {
                            ZipArchive za = archive;
                            if (za == null)
                            {
                                error = new Exception("Obb Archive Cannot be read.");
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

                    if (result != ZipEntryType.NonExist)
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