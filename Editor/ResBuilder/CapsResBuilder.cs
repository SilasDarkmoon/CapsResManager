using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
#else
using System.IO.Compression;
#endif

namespace Capstones.UnityEditorEx
{
    public static class CapsResBuilder
    {
        public class ResBuilderParams
        {
            public string timetoken;
            public int version = 0;
            public bool makezip = true;

            private ResBuilderParams() { }
            public static ResBuilderParams Create()
            {
                return new ResBuilderParams()
                {
                    timetoken = DateTime.Now.ToString("yyMMdd_HHmmss"),
                };
            }
        }
        public static ResBuilderParams BuildingParams = null;
        public interface IResBuilderEx
        {
            //IEnumerator CustomBuild();
            void Prepare(string output);
            string FormatBundleName(string asset, string mod, string dist, string norm);
            bool CreateItem(CapsResManifestNode node);
            void ModifyItem(CapsResManifestItem item);
            void GenerateBuildWork(string bundleName, IList<string> assets, ref AssetBundleBuild abwork, CapsResBuildWork modwork, int abindex);
            void Cleanup();
            void OnSuccess();
        }
        public static readonly List<IResBuilderEx> ResBuilderEx = new List<IResBuilderEx>();

        public class CapsResBuildWork
        {
            public AssetBundleBuild[] ABs;
            public CapsResManifest[] Manifests;
            public HashSet<int> ForceRefreshABs = new HashSet<int>(); // Stores the index in ABs array, which should be deleted before this build (in order to force it to update).
        }

        public static IEnumerator GenerateBuildWorkAsync(Dictionary<string, CapsResBuildWork> result, IList<string> assets, IEditorWorkProgressShower winprog)
        {
            return GenerateBuildWorkAsync(result, assets, winprog, null);
        }
        public static IEnumerator GenerateBuildWorkAsync(Dictionary<string, CapsResBuildWork> result, IList<string> assets, IEditorWorkProgressShower winprog, IList<IResBuilderEx> runOnceExBuilder)
        {
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            logger.Log("(Start) Generate Build Work.");
            if (winprog != null && AsyncWorkTimer.Check()) yield return null;

            if (result == null)
            {
                logger.Log("(Error) You have to provide container to retrive the result.");
                yield break;
            }
            result.Clear();

            if (assets == null)
            {
                logger.Log("(Option) Get All Assets.");
                assets = AssetDatabase.GetAllAssetPaths();
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
            }

            if (assets != null)
            {
                List<IResBuilderEx> allExBuilders = new List<IResBuilderEx>(ResBuilderEx);
                if (runOnceExBuilder != null)
                {
                    allExBuilders.AddRange(runOnceExBuilder);
                }

                Dictionary<string, Dictionary<string, List<string>>> mod2build = new Dictionary<string, Dictionary<string, List<string>>>();
                Dictionary<string, Dictionary<string, CapsResManifest>> mod2mani = new Dictionary<string, Dictionary<string, CapsResManifest>>();
                for (int i = 0; i < assets.Count; ++i)
                {
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                    var asset = assets[i];
                    logger.Log(asset);

                    if (string.IsNullOrEmpty(asset))
                    {
                        logger.Log("Empty Path.");
                        continue;
                    }
                    if (System.IO.Directory.Exists(asset))
                    {
                        logger.Log("Folder.");
                        continue;
                    }
                    if (CapsResInfoEditor.IsAssetScript(asset))
                    {
                        logger.Log("Script.");
                        continue;
                    }

                    string mod = null;
                    string opmod = null;
                    string dist = null;
                    string norm = asset;
                    bool inPackage = false;
                    if (asset.StartsWith("Assets/Mods/") || (inPackage = asset.StartsWith("Packages/")))
                    {
                        string sub;
                        if (inPackage)
                        {
                            sub = asset.Substring("Packages/".Length);
                        }
                        else
                        {
                            sub = asset.Substring("Assets/Mods/".Length);
                        }
                        var index = sub.IndexOf('/');
                        if (index < 0)
                        {
                            logger.Log("Cannot Parse Module.");
                            continue;
                        }
                        mod = sub.Substring(0, index);
                        if (inPackage)
                        {
                            mod = CapsModEditor.GetPackageModName(mod);
                        }
                        if (string.IsNullOrEmpty(mod))
                        {
                            logger.Log("Empty Module.");
                            continue;
                        }
                        sub = sub.Substring(index + 1);
                        if (!sub.StartsWith("CapsRes/"))
                        {
                            logger.Log("Should Ignore This Asset.");
                            continue;
                        }
                        var moddesc = ResManager.GetDistributeDesc(mod);
                        bool isMainPackage = inPackage && !CapsModEditor.ShouldTreatPackageAsMod(CapsModEditor.GetPackageName(mod));
                        if (moddesc == null || moddesc.InMain || isMainPackage)
                        {
                            mod = "";
                            if (moddesc != null && moddesc.IsOptional && !isMainPackage)
                            {
                                opmod = moddesc.Mod;
                            }
                        }

                        sub = sub.Substring("CapsRes/".Length);
                        norm = sub;
                        if (sub.StartsWith("dist/"))
                        {
                            sub = sub.Substring("dist/".Length);
                            index = sub.IndexOf('/');
                            if (index > 0)
                            {
                                dist = sub.Substring(0, index);
                                norm = sub.Substring(index + 1);
                            }
                        }
                    }
                    else
                    {
                        if (asset.StartsWith("Assets/CapsRes/"))
                        {
                            mod = "";
                            var sub = asset.Substring("Assets/CapsRes/".Length);
                            norm = sub;
                            if (sub.StartsWith("dist/"))
                            {
                                sub = sub.Substring("dist/".Length);
                                var index = sub.IndexOf('/');
                                if (index > 0)
                                {
                                    dist = sub.Substring(0, index);
                                    norm = sub.Substring(index + 1);
                                }
                            }
                        }
                        else
                        {
                            logger.Log("Should Ignore This Asset.");
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(norm))
                    {
                        logger.Log("Normallized Path Empty.");
                        continue;
                    }
                    mod = mod ?? "";
                    dist = dist ?? "";
                    logger.Log("Mod " + mod + "; Dist " + dist + "; Norm " + norm);

                    Dictionary<string, List<string>> builds;
                    if (!mod2build.TryGetValue(mod, out builds))
                    {
                        builds = new Dictionary<string, List<string>>();
                        mod2build[mod] = builds;
                    }

                    Dictionary<string, CapsResManifest> manis;
                    if (!mod2mani.TryGetValue(opmod ?? mod, out manis))
                    {
                        manis = new Dictionary<string, CapsResManifest>();
                        mod2mani[opmod ?? mod] = manis;
                    }
                    CapsResManifest mani;
                    if (!manis.TryGetValue(dist, out mani))
                    {
                        mani = new CapsResManifest();
                        mani.MFlag = opmod ?? mod;
                        mani.DFlag = dist;
                        if (opmod != null)
                        {
                            mani.InMain = true;
                        }
                        manis[dist] = mani;
                    }

                    string bundle = null;
                    bool shouldWriteBRef = false;
                    for (int j = 0; j < allExBuilders.Count; ++j)
                    {
                        bundle = allExBuilders[j].FormatBundleName(asset, opmod ?? mod, dist, norm);
                        if (bundle != null)
                        {
                            break;
                        }
                    }
                    if (bundle == null)
                    {
                        bundle = FormatBundleName(asset, opmod ?? mod, dist, norm);
                    }
                    else
                    {
                        shouldWriteBRef = true;
                    }

                    List<string> build;
                    if (!builds.TryGetValue(bundle, out build))
                    {
                        build = new List<string>();
                        builds[bundle] = build;
                    }
                    build.Add(asset);

                    var node = mani.AddOrGetItem(asset);
                    for (int j = 0; j < allExBuilders.Count; ++j)
                    {
                        if (allExBuilders[j].CreateItem(node))
                        {
                            break;
                        }
                    }
                    if (node.Item == null)
                    {
                        var item = new CapsResManifestItem(node);
                        if (asset.EndsWith(".prefab"))
                        {
                            item.Type = (int)CapsResManifestItemType.Prefab;
                        }
                        else if (asset.EndsWith(".unity"))
                        {
                            item.Type = (int)CapsResManifestItemType.Scene;
                        }
                        else
                        {
                            item.Type = (int)CapsResManifestItemType.Normal;
                        }
                        if (shouldWriteBRef)
                        {
                            item.BRef = bundle;
                        }
                        node.Item = item;
                    }
                    for (int j = 0; j < allExBuilders.Count; ++j)
                    {
                        allExBuilders[j].ModifyItem(node.Item);
                    }
                }

                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                logger.Log("(Phase) Combine the final result.");

                foreach (var kvpbuild in mod2build)
                {
                    var mod = kvpbuild.Key;
                    var builds = kvpbuild.Value;
                    CapsResBuildWork work = new CapsResBuildWork();
                    if (mod == "")
                    {
                        List<CapsResManifest> manis = new List<CapsResManifest>(mod2mani[mod].Values);
                        foreach (var kvpmm in mod2mani)
                        {
                            if (!mod2build.ContainsKey(kvpmm.Key))
                            {
                                manis.AddRange(kvpmm.Value.Values);
                            }
                        }
                        work.Manifests = manis.ToArray();
                    }
                    else
                    {
                        work.Manifests = mod2mani[mod].Values.ToArray();
                    }

                    work.ABs = new AssetBundleBuild[builds.Count];
                    int index = 0;
                    foreach (var kvpbundle in builds)
                    {
                        var bundleName = kvpbundle.Key;
                        var bundleAssets = kvpbundle.Value;
                        AssetBundleBuild build = new AssetBundleBuild();
                        build.assetBundleName = kvpbundle.Key;
                        build.assetNames = kvpbundle.Value.ToArray();
                        for (int j = 0; j < allExBuilders.Count; ++j)
                        {
                            allExBuilders[j].GenerateBuildWork(bundleName, bundleAssets, ref build, work, index);
                        }
                        work.ABs[index++] = build;
                    }

                    result[mod] = work;
                }
            }

            logger.Log("(Done) Generate Build Work.");
        }

        public static TaskProgress MakeZipBackground(string zipFile, string srcDir, IList<string> entries, System.Threading.EventWaitHandle waithandle)
        {
            return PlatDependant.RunBackground(progress =>
            {
                try
                {
                    if (string.IsNullOrEmpty(zipFile) || entries == null || entries.Count == 0 || !System.IO.Directory.Exists(srcDir))
                    {
                        return;
                    }
                    progress.Total = entries.Count;
                    using (var stream = PlatDependant.OpenWrite(zipFile))
                    {
                        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                        {
                            if (!srcDir.EndsWith("/") && !srcDir.EndsWith("\\"))
                            {
                                srcDir += "/";
                            }
                            for (int i = 0; i < entries.Count; ++i)
                            {
                                progress.Length = i;
                                var entry = entries[i];
                                if (string.IsNullOrEmpty(entry))
                                {
                                    continue;
                                }

                                var src = srcDir + entry;
                                if (PlatDependant.IsFileExist(src))
                                {
                                    try
                                    {
                                        using (var srcstream = PlatDependant.OpenRead(src))
                                        {
                                            var zentry = zip.CreateEntry(entry.Replace('\\', '/'));
                                            using (var dststream = zentry.Open())
                                            {
                                                srcstream.CopyTo(dststream);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError("zip entry FAIL! " + entry);
                                        PlatDependant.LogError(e);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError("Build zip FAIL! " + zipFile);
                    PlatDependant.LogError(e);
                }
                finally
                {
                    if (waithandle != null)
                    {
                        waithandle.Set();
                    }
                }
            });
        }
        public static IEnumerator MakeZipsBackground(List<Pack<string, string, IList<string>>> zips, IEditorWorkProgressShower winprog)
        {
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            if (zips != null)
            {
                System.Threading.EventWaitHandle waithandle = null;
                if (winprog == null)
                {
                    waithandle = new System.Threading.ManualResetEvent(true);
                }
                int next = 0;
                int done = 0;
                int cpucnt = System.Environment.ProcessorCount;
                Pack<string, TaskProgress>[] working = new Pack<string, TaskProgress>[cpucnt];
                while (done < zips.Count)
                {
                    for (int i = 0; i < cpucnt; ++i)
                    {
                        var info = working[i];
                        if (info.t2 == null)
                        {
                            if (next < zips.Count)
                            {
                                var zip = zips[next++];
                                if (winprog == null)
                                {
                                    waithandle.Reset();
                                }
                                working[i] = new Pack<string, TaskProgress>(zip.t1, MakeZipBackground(zip.t1, zip.t2, zip.t3, waithandle));
                            }
                        }
                        else
                        {
                            if (info.t2.Done)
                            {
                                ++done;
                                logger.Log("Zip file DONE! " + info.t1);
                                working[i].t2 = null;
                            }
                        }
                    }
                    if (done >= zips.Count)
                    {
                        break;
                    }
                    if (winprog == null)
                    {
                        waithandle.WaitOne();
                    }
                    else
                    {
                        yield return null;
                    }
                }
                logger.Log("Zip ALL DONE!");
            }
            else
            {
                logger.Log("Zip - No file to zip.");
            }
        }
        public static IEnumerator MakeZipAsync(string zipFile, string srcDir, IList<string> entries, IEditorWorkProgressShower winprog)
        {
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            logger.Log("Zipping: " + zipFile);
            if (string.IsNullOrEmpty(zipFile) || entries == null || entries.Count == 0 || !System.IO.Directory.Exists(srcDir))
            {
                logger.Log("Nothing to zip");
                yield break;
            }

            var stream = PlatDependant.OpenWrite(zipFile);
            if (stream == null)
            {
                logger.Log("Cannot create zip file.");
                yield break;
            }

            var zip = new ZipArchive(stream, ZipArchiveMode.Create);

            try
            {
                if (!srcDir.EndsWith("/") && !srcDir.EndsWith("\\"))
                {
                    srcDir += "/";
                }
                for (int i = 0; i < entries.Count; ++i)
                {
                    var entry = entries[i];
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                    logger.Log(entry);
                    if (string.IsNullOrEmpty(entry))
                    {
                        continue;
                    }

                    var src = srcDir + entry;
                    if (PlatDependant.IsFileExist(src))
                    {
                        try
                        {
                            using (var srcstream = PlatDependant.OpenRead(src))
                            {
                                var zentry = zip.CreateEntry(entry.Replace('\\', '/'));
                                using (var dststream = zentry.Open())
                                {
                                    srcstream.CopyTo(dststream);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log("(Error)(Not Critical)");
                            logger.Log(e.ToString());
                        }
                    }
                }
            }
            finally
            {
                zip.Dispose();
                stream.Dispose();
            }
        }

        public static IEnumerator BuildResAsync(IList<string> assets, IEditorWorkProgressShower winprog)
        {
            return BuildResAsync(assets, winprog, null);
        }
        public static IEnumerator BuildResAsync(IList<string> assets, IEditorWorkProgressShower winprog, IList<IResBuilderEx> runOnceExBuilder)
        {
            bool isDefaultBuild = assets == null;
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            bool shouldCreateBuildingParams = BuildingParams == null;
            BuildingParams = BuildingParams ?? ResBuilderParams.Create();
            var timetoken = BuildingParams.timetoken;
            var makezip = BuildingParams.makezip;
            string outputDir = "Latest";
            if (!isDefaultBuild)
            {
                outputDir = timetoken + "/build";
            }
            outputDir = "EditorOutput/Build/" + outputDir;

            System.IO.StreamWriter swlog = null;
            try
            {
                System.IO.Directory.CreateDirectory(outputDir + "/log/");
                swlog = new System.IO.StreamWriter(outputDir + "/log/ResBuildLog.txt", false, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            EditorApplication.LockReloadAssemblies();
            List<IResBuilderEx> allExBuilders = new List<IResBuilderEx>(ResBuilderEx);
            if (runOnceExBuilder != null)
            {
                allExBuilders.AddRange(runOnceExBuilder);
            }
            Application.LogCallback LogToFile = (message, stack, logtype) =>
            {
                swlog.WriteLine(message);
                swlog.Flush();
            };
            if (swlog != null)
            {
                Application.logMessageReceived += LogToFile;
            }
            for (int i = 0; i < allExBuilders.Count; ++i)
            {
                allExBuilders[i].Prepare(outputDir);
            }
            bool cleanupDone = false;
            Action BuilderCleanup = () =>
            {
                if (!cleanupDone)
                {
                    logger.Log("(Phase) Build Res Cleaup.");
                    cleanupDone = true;
                    for (int i = 0; i < allExBuilders.Count; ++i)
                    {
                        allExBuilders[i].Cleanup();
                    }
                    logger.Log("(Done) Build Res Cleaup.");
                    if (swlog != null)
                    {
                        Application.logMessageReceived -= LogToFile;
                        swlog.Flush();
                        swlog.Dispose();

                        if (isDefaultBuild)
                        {
                            var logdir = "EditorOutput/Build/" + timetoken + "/log/";
                            System.IO.Directory.CreateDirectory(logdir);
                            System.IO.File.Copy(outputDir + "/log/ResBuildLog.txt", logdir + "ResBuildLog.txt", true);
                        }
                    }
                    if (shouldCreateBuildingParams)
                    {
                        BuildingParams = null;
                    }
                    EditorApplication.UnlockReloadAssemblies();
                }
            };
            if (winprog != null) winprog.OnQuit += BuilderCleanup;

            try
            {
                logger.Log("(Start) Build Res.");
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;

                //logger.Log("(Phase) Ex Full Build System.");
                //for (int i = 0; i < allExBuilders.Count; ++i)
                //{
                //    IEnumerator exwork = allExBuilders[i].CustomBuild();
                //    if (exwork != null)
                //    {
                //        while (exwork.MoveNext())
                //        {
                //            if (winprog != null)
                //            {
                //                yield return exwork.Current;
                //            }
                //        }
                //    }
                //    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                //}

                // Generate Build Work
                Dictionary<string, CapsResBuildWork> works = new Dictionary<string, CapsResBuildWork>();
                var work = GenerateBuildWorkAsync(works, assets, winprog, runOnceExBuilder);
                while (work.MoveNext())
                {
                    if (winprog != null)
                    {
                        yield return work.Current;
                    }
                }

                logger.Log("(Phase) Write Manifest.");
                var managermod = CapsEditorUtils.__MOD__;
                var manidir = "Assets/Mods/" + managermod + "/Build/";
                System.IO.Directory.CreateDirectory(manidir);
                List<AssetBundleBuild> listManiBuilds = new List<AssetBundleBuild>();
                HashSet<string> maniFileNames = new HashSet<string>();
                foreach (var kvp in works)
                {
                    foreach (var mani in kvp.Value.Manifests)
                    {
                        var mod = mani.MFlag;
                        var dist = mani.DFlag;
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        logger.Log("Mod " + mod + "; Dist " + dist);

                        var dmani = CapsResManifest.Save(mani);
                        var filename = "m-" + mod + "-d-" + dist;
                        var manipath = manidir + filename + ".m.asset";
                        AssetDatabase.CreateAsset(dmani, manipath);

                        maniFileNames.Add(filename.ToLower());
                        listManiBuilds.Add(new AssetBundleBuild() { assetBundleName = filename + ".m.ab", assetNames = new[] { manipath } });
                    }
                }

                logger.Log("(Phase) Build Manifest.");
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                var buildopt = BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression;
                BuildTarget buildtar = EditorUserBuildSettings.activeBuildTarget;
                var outmanidir = outputDir + "/res/mani";
                System.IO.Directory.CreateDirectory(outmanidir);
                BuildPipeline.BuildAssetBundles(outmanidir, listManiBuilds.ToArray(), buildopt, buildtar);

                logger.Log("(Phase) Delete Unused Manifest.");
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                var manifiles = PlatDependant.GetAllFiles(outmanidir);
                for (int i = 0; i < manifiles.Length; ++i)
                {
                    var file = manifiles[i];
                    if (file.EndsWith(".m.ab"))
                    {
                        var filename = file.Substring(outmanidir.Length + 1, file.Length - outmanidir.Length - 1 - ".m.ab".Length);
                        if (!maniFileNames.Contains(filename))
                        {
                            PlatDependant.DeleteFile(file);
                            PlatDependant.DeleteFile(file + ".manifest");
                        }
                    }
                }

                logger.Log("(Phase) Real Build.");
                foreach (var kvp in works)
                {
                    var mod = kvp.Key;
                    var abs = kvp.Value.ABs;
                    logger.Log("Mod " + mod);
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;

                    var dest = outputDir + "/res";
                    if (!string.IsNullOrEmpty(mod))
                    {
                        dest += "/mod/" + mod;
                    }

                    System.IO.Directory.CreateDirectory(dest);
                    // delete the unused ab
                    HashSet<string> buildFiles = new HashSet<string>();
                    for (int i = 0; i < abs.Length; ++i)
                    {
                        if (!kvp.Value.ForceRefreshABs.Contains(i))
                        {
                            if (string.IsNullOrEmpty(abs[i].assetBundleVariant))
                            {
                                buildFiles.Add(abs[i].assetBundleName.ToLower());
                            }
                            else
                            {
                                buildFiles.Add(abs[i].assetBundleName.ToLower() + "." + abs[i].assetBundleVariant.ToLower());
                            }
                        }
                    }
                    var files = System.IO.Directory.GetFiles(dest);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var file = files[i];
                        if (!file.EndsWith(".ab"))
                        {
                            var sub = System.IO.Path.GetFileName(file);
                            var split = sub.LastIndexOf(".ab.");
                            if (split < 0)
                            {
                                continue;
                            }
                            var ext = sub.Substring(split + ".ab.".Length);
                            if (ext.Contains("."))
                            {
                                continue;
                            }
                            if (ext == "manifest")
                            {
                                continue;
                            }
                        }
                        {
                            var fileName = System.IO.Path.GetFileName(file);
                            if (!buildFiles.Contains(fileName))
                            {
                                PlatDependant.DeleteFile(file);
                                PlatDependant.DeleteFile(file + ".manifest");
                            }
                        }
                    }

                    BuildPipeline.BuildAssetBundles(dest, abs, buildopt, buildtar);
                }

                logger.Log("(Phase) Delete Mod Folder Not Built.");
                var outmoddir = outputDir + "/res/mod/";
                if (System.IO.Directory.Exists(outmoddir))
                {
                    var builtMods = new HashSet<string>(works.Keys);
                    var allModFolders = System.IO.Directory.GetDirectories(outmoddir);
                    int deletedModFolderCnt = 0;
                    for (int i = 0; i < allModFolders.Length; ++i)
                    {
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        var modfolder = allModFolders[i];
                        logger.Log(modfolder);
                        var mod = modfolder.Substring(outmoddir.Length);
                        if (!builtMods.Contains(mod))
                        {
                            System.IO.Directory.Delete(modfolder, true);
                            ++deletedModFolderCnt;
                        }
                    }
                    if (deletedModFolderCnt == allModFolders.Length)
                    {
                        System.IO.Directory.Delete(outmoddir, true);
                    }
                }

                if (isDefaultBuild)
                {
                    logger.Log("(Phase) Write Version.");
                    var outverdir = outputDir + "/res/version.txt";
                    int version = GetResVersion();
                    System.IO.File.WriteAllText(outverdir, version.ToString());
                }

                logger.Log("(Phase) Copy.");
                var outresdir = outputDir + "/res/";
                var allbuildfiles = PlatDependant.GetAllFiles(outresdir);
                if (System.IO.Directory.Exists("Assets/StreamingAssets/res/"))
                {
                    logger.Log("Delete old.");
                    var allexistfiles = PlatDependant.GetAllFiles("Assets/StreamingAssets/res/");
                    for (int i = 0; i < allexistfiles.Length; ++i)
                    {
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        PlatDependant.DeleteFile(allexistfiles[i]);
                    }
                }
                for (int i = 0; i < allbuildfiles.Length; ++i)
                {
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                    var srcfile = allbuildfiles[i];
                    if (srcfile.EndsWith(".manifest"))
                    {
                        continue;
                    }
                    var part = srcfile.Substring(outresdir.Length);
                    if (part == "mani/mani")
                    {
                        continue;
                    }
                    logger.Log(part);
                    var destfile = "Assets/StreamingAssets/res/" + part;
                    PlatDependant.CreateFolder(System.IO.Path.GetDirectoryName(destfile));
                    System.IO.File.Copy(srcfile, destfile);
                }

                if (System.IO.Directory.Exists("Assets/StreamingAssets/res/mod/"))
                {
                    logger.Log("(Phase) Delete StreamingAssets Mod Folder Not Built.");
                    var builtMods = new HashSet<string>(works.Keys);
                    var allModFolders = System.IO.Directory.GetDirectories("Assets/StreamingAssets/res/mod/");
                    int deletedModFolderCnt = 0;
                    for (int i = 0; i < allModFolders.Length; ++i)
                    {
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        var modfolder = allModFolders[i];
                        logger.Log(modfolder);
                        var mod = modfolder.Substring("Assets/StreamingAssets/res/mod/".Length);
                        if (!builtMods.Contains(mod))
                        {
                            System.IO.Directory.Delete(modfolder, true);
                            ++deletedModFolderCnt;
                        }
                    }
                    if (deletedModFolderCnt == allModFolders.Length)
                    {
                        System.IO.Directory.Delete("Assets/StreamingAssets/res/mod/", true);
                    }
                }

                if (isDefaultBuild && makezip)
                {
                    logger.Log("(Phase) Zip.");
                    List<Pack<string, string, IList<string>>> zips = new List<Pack<string, string, IList<string>>>();
                    var outzipdir = "EditorOutput/Build/" + timetoken + "/whole/res/";
                    System.IO.Directory.CreateDirectory(outzipdir);
                    foreach (var kvp in works)
                    {
                        var mod = kvp.Key;
                        var manis = kvp.Value.Manifests;
                        for (int i = 0; i < manis.Length; ++i)
                        {
                            var opmod = manis[i].MFlag;
                            var dist = manis[i].DFlag;
                            if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                            logger.Log("Mod " + opmod + "; Dist " + dist);

                            List<string> entries = new List<string>();
                            // abs
                            var abdir = outputDir + "/res";
                            if (!string.IsNullOrEmpty(mod))
                            {
                                abdir += "/mod/" + mod;
                            }

                            if (System.IO.Directory.Exists(abdir))
                            {
                                try
                                {
                                    var files = System.IO.Directory.GetFiles(abdir);
                                    for (int j = 0; j < files.Length; ++j)
                                    {
                                        var file = files[j];
                                        if (!file.EndsWith(".ab"))
                                        {
                                            var sub = System.IO.Path.GetFileName(file);
                                            var split = sub.LastIndexOf(".ab.");
                                            if (split < 0)
                                            {
                                                continue;
                                            }
                                            var ext = sub.Substring(split + ".ab.".Length);
                                            if (ext.Contains("."))
                                            {
                                                continue;
                                            }
                                            if (ext == "manifest")
                                            {
                                                continue;
                                            }
                                        }
                                        {
                                            var bundle = file.Substring(abdir.Length + 1);
                                            if (IsBundleInModAndDist(bundle, opmod, dist))
                                            {
                                                var entry = file.Substring(outputDir.Length + 1);
                                                entries.Add(entry);
                                                entries.Add(entry + ".manifest");
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    logger.Log("(Error)(Not Critical)");
                                    logger.Log(e.ToString());
                                }
                            }
                            if (entries.Count > 0)
                            {
                                // unity build mani
                                var umani = abdir + "/" + (string.IsNullOrEmpty(mod) ? "res" : mod);
                                umani = umani.Substring(outputDir.Length + 1);
                                entries.Add(umani);
                                entries.Add(umani + ".manifest");
                                // mani
                                var mani = "m-" + opmod.ToLower() + "-d-" + dist.ToLower() + ".m.ab";
                                mani = "res/mani/" + mani;
                                entries.Add(mani);
                                entries.Add(mani + ".manifest");
                                entries.Add("res/mani/mani");
                                entries.Add("res/mani/mani.manifest");
                                // version
                                entries.Add("res/version.txt");

                                var zipfile = outzipdir + "m-" + opmod + "-d-" + dist + ".zip";
                                zips.Add(new Pack<string, string, IList<string>>(zipfile, outputDir, entries));
                                //var workz = MakeZipAsync(zipfile, outputDir, entries, winprog);
                                //while (workz.MoveNext())
                                //{
                                //    if (winprog != null)
                                //    {
                                //        yield return workz.Current;
                                //    }
                                //}
                            }
                        }
                    }
                    if (zips.Count > 0)
                    {
                        var workz = CapsResBuilder.MakeZipsBackground(zips, winprog);
                        while (workz.MoveNext())
                        {
                            if (winprog != null)
                            {
                                yield return workz.Current;
                            }
                        }
                    }
                }

                for (int i = 0; i < allExBuilders.Count; ++i)
                {
                    allExBuilders[i].OnSuccess();
                }
            }
            finally
            {
                BuilderCleanup();
                logger.Log("(Done) Build Res.");
            }
        }

        public static int GetResVersion()
        {
            int version = 0;
            if (BuildingParams != null)
            {
                version = BuildingParams.version;
            }
            if (version <= 0)
            {
                int lastBuildVersion = 0;
                int streamingVersion = 0;
                var outverdir = "EditorOutput/Build/Latest/res/version.txt";

                if (System.IO.File.Exists("Assets/StreamingAssets/res/version.txt"))
                {
                    var lines = System.IO.File.ReadAllLines("Assets/StreamingAssets/res/version.txt");
                    if (lines != null && lines.Length > 0)
                    {
                        int.TryParse(lines[0], out streamingVersion);
                    }
                }
                if (System.IO.File.Exists(outverdir))
                {
                    var lines = System.IO.File.ReadAllLines(outverdir);
                    if (lines != null && lines.Length > 0)
                    {
                        int.TryParse(lines[0], out lastBuildVersion);
                    }
                }
                if (streamingVersion > 0 || lastBuildVersion <= 0)
                {
                    version = Math.Max(lastBuildVersion, streamingVersion) + 10;
                }
                else
                {
                    version = lastBuildVersion;
                }

                if (BuildingParams != null)
                {
                    BuildingParams.version = version;
                }
            }
            return version;
        }

        public static string FormatBundleName(string asset, string mod, string dist, string norm)
        {
            System.Text.StringBuilder sbbundle = new System.Text.StringBuilder();
            sbbundle.Append("m-");
            sbbundle.Append(mod ?? "");
            sbbundle.Append("-d-");
            sbbundle.Append(dist ?? "");
            sbbundle.Append("-");
            sbbundle.Append(System.IO.Path.GetDirectoryName(norm));
            sbbundle.Replace('\\', '-');
            sbbundle.Replace('/', '-');
            if (norm.EndsWith(".unity"))
            {
                sbbundle.Append("-");
                sbbundle.Append(System.IO.Path.GetFileNameWithoutExtension(norm));
                sbbundle.Append(".s");
            }
            else if (norm.EndsWith(".prefab"))
            {
                sbbundle.Append(".o");
            }
            sbbundle.Append(".ab");
            return sbbundle.ToString();
        }

        public static bool IsBundleInModAndDist(string bundle, string mod, string dist)
        {
            var mdstr = "m-" + (mod ?? "") + "-d-" + (dist ?? "");
            var keypre = mdstr + "-";
            var keypost = ".ab." + mdstr;
            bundle = bundle ?? "";
            return bundle.StartsWith(keypre, StringComparison.InvariantCultureIgnoreCase) || bundle.EndsWith(keypost, StringComparison.InvariantCultureIgnoreCase);
        }

        private class CapsResBuilderPreExport : UnityEditor.Build.IPreprocessBuild
        {
            public int callbackOrder { get { return 0; } }

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                using (var sw = PlatDependant.OpenWriteText("Assets/StreamingAssets/res/index.txt"))
                {
                    var files = PlatDependant.GetAllFiles("Assets/StreamingAssets/res/mani/");
                    if (files != null)
                    {
                        for (int i = 0; i < files.Length; ++i)
                        {
                            var file = files[i];
                            if (file.EndsWith(".m.ab"))
                            {
                                var key = file.Substring("Assets/StreamingAssets/res/mani/".Length, file.Length - "Assets/StreamingAssets/res/mani/".Length - ".m.ab".Length);
                                sw.WriteLine(key);
                            }
                        }
                    }
                }
                using (var sw = PlatDependant.OpenWriteText("Assets/StreamingAssets/res/builtin-scenes.txt"))
                {
                    var scenes = EditorBuildSettings.scenes;
                    int index = 0;
                    for (int i = 0; i < scenes.Length; ++i)
                    {
                        var sceneinfo = scenes[i];
                        if (sceneinfo.enabled)
                        {
                            var guid = sceneinfo.guid.ToString();
                            var scenepath = AssetDatabase.GUIDToAssetPath(guid);
                            sw.Write(scenepath);
                            sw.Write("|");
                            sw.WriteLine(index++);
                        }
                    }
                }
            }
        }
    }
}
