﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
#if UNITY_EDITOR
    public static partial class EditorToClientUtils
    {
        public static bool Ready = false;

        public static Func<string[]> GetAllModsFunc { set; private get; }
        public static Func<string, bool> CheckModOptionalFunc { set; private get; }
        public static Func<string, string> ModNameToPackageName { set; private get; }
        public static Func<string, string> PackageNameToModName { set; private get; }
        public static Func<string, string> AssetNameToPath { set; private get; }
        public static Func<string, string> PathToAssetName { set; private get; }

        public static string[] GetCriticalMods()
        {
            List<string> mods = new List<string>();
            if (GetAllModsFunc != null && CheckModOptionalFunc != null)
            {
                var allmods = GetAllModsFunc();
                for (int i = 0; i < allmods.Length; ++i)
                {
                    var mod = allmods[i];
                    if (!CheckModOptionalFunc(mod))
                    {
                        mods.Add(mod);
                    }
                }
                mods.Sort();
            }
            return mods.ToArray();
        }

        public static string GetModNameFromPackageName(string package)
        {
            if (PackageNameToModName != null)
            {
                return PackageNameToModName(package);
            }
            return null;
        }
        public static string GetPackageNameFromModName(string mod)
        {
            if (ModNameToPackageName != null)
            {
                return ModNameToPackageName(mod);
            }
            return null;
        }
        public static string GetAssetNameFromPath(string path)
        {
            if (PathToAssetName != null)
            {
                return PathToAssetName(path);
            }
            return path;
        }
        public static string GetPathFromAssetName(string asset)
        {
            if (AssetNameToPath != null)
            {
                return AssetNameToPath(asset);
            }
            return asset;
        }
    }
#endif

    public static partial class ResManager
    {
#if UNITY_EDITOR
        public class EditorResLoader : IResLoader
        {
            public EditorResLoader()
            {
#if !FORCE_USE_CLIENT_RESLOADER
                ResLoader = this;
#endif
            }

            public static string CheckModPath(string path)
            {
                string found = null;
                Func<string, bool> checkFile = file =>
                {
                    bool exist = false;
                    if (file.StartsWith("Packages/"))
                    {
                        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(file);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            exist = true;
                        }
                    }
                    else
                    {
                        exist = PlatDependant.IsFileExist(file);
                    }
                    if (exist)
                    {
#if EDITOR_LOADER_NO_CHECK
                        found = file;
#endif
                        if (found == null)
                        {
                            found = file;
                        }
                        else
                        {
                            Debug.LogWarning("Duplicated item: " + found + "\nReplaces: " + file);
                        }
                        return true;
                    }
                    return false;
                };

                var dflags = GetValidDistributeFlags();
                for (int i = dflags.Length - 1; i >= 0; --i)
                {
                    var dflag = dflags[i];
                    var package = EditorToClientUtils.GetPackageNameFromModName(dflag);
                    if (!string.IsNullOrEmpty(package))
                    {
#if EDITOR_LOAD_RAW_RES
                        {
                            var realpath = "Packages/" + package + "/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                            if (checkFile(realpath))
                            {
                                return found;
                            }
#else
                            checkFile(realpath);
#endif
                        }
#endif
                        {
                            var realpath = "Packages/" + package + "/" + path;
#if EDITOR_LOADER_NO_CHECK
                            if (checkFile(realpath))
                            {
                                return found;
                            }
#else
                            checkFile(realpath);
#endif
                        }
                    }
#if EDITOR_LOAD_RAW_RES
                    {
                        var realpath = "Assets/Mods/" + dflag + "/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                        if (checkFile(realpath))
                        {
                            return found;
                        }
#else
                        checkFile(realpath);
#endif
                    }
#endif
                    {
                        var realpath = "Assets/Mods/" + dflag + "/" + path;
#if EDITOR_LOADER_NO_CHECK
                        if (checkFile(realpath))
                        {
                            return found;
                        }
#else
                        checkFile(realpath);
#endif
                    }
                }
                var cflags = EditorToClientUtils.GetCriticalMods();
                for (int i = cflags.Length - 1; i >= 0; --i)
                {
                    var dflag = cflags[i];
                    var package = EditorToClientUtils.GetPackageNameFromModName(dflag);
                    if (!string.IsNullOrEmpty(package))
                    {
#if EDITOR_LOAD_RAW_RES
                        {
                            var realpath = "Packages/" + package + "/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                            if (checkFile(realpath))
                            {
                                return found;
                            }
#else
                            checkFile(realpath);
#endif
                        }
#endif
                        {
                            var realpath = "Packages/" + package + "/" + path;
#if EDITOR_LOADER_NO_CHECK
                            if (checkFile(realpath))
                            {
                                return found;
                            }
#else
                            checkFile(realpath);
#endif
                        }
                    }
#if EDITOR_LOAD_RAW_RES
                    {
                        var realpath = "Assets/Mods/" + dflag + "/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                        if (checkFile(realpath))
                        {
                            return found;
                        }
#else
                        checkFile(realpath);
#endif
                    }
#endif
                    {
                        var realpath = "Assets/Mods/" + dflag + "/" + path;
#if EDITOR_LOADER_NO_CHECK
                        if (checkFile(realpath))
                        {
                            return found;
                        }
#else
                        checkFile(realpath);
#endif
                    }
                }
#if EDITOR_LOAD_RAW_RES
                {
                    var realpath = "Assets/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                    if (checkFile(realpath))
                    {
                        return found;
                    }
#else
                    checkFile(realpath);
#endif
                }
#endif
                {
                    var realpath = "Assets/" + path;
#if EDITOR_LOADER_NO_CHECK
                    if (checkFile(realpath))
                    {
                        return found;
                    }
#else
                    checkFile(realpath);
#endif
                }
                return found;
            }
            private static string[] _DistributeFolderNames = new[] { "CapsRes/", "CapsSpt/", "Resources/" };
            public static string CheckDistributePath(string path)
            {
                return CheckDistributePath(path, false);
            }
            public static string CheckDistributePath(string path, bool noWarningWhenNotFound)
            {
                string found = null;
                string distFolderName = null;
                for (int i = 0; i < _DistributeFolderNames.Length; ++i)
                {
                    var folder = _DistributeFolderNames[i];
                    if (path.StartsWith(folder))
                    {
                        distFolderName = folder;
                        break;
                    }
                }

                if (distFolderName != null)
                {
                    var dflags = GetValidDistributeFlags();
                    for (int i = dflags.Length - 1; i >= 0; --i)
                    {
                        var dflag = dflags[i];
                        var realpath = distFolderName + "dist/" + dflag + path.Substring(distFolderName.Length - 1);
                        var dfound = CheckModPath(realpath);
                        if (dfound != null)
                        {
#if EDITOR_LOADER_NO_CHECK
                            return dfound;
#endif
                            if (found == null)
                            {
                                found = dfound;
                            }
                            else
                            {
                                Debug.LogWarning("Duplicated item: " + found + "\nReplaces: " + dfound);
                            }
                        }
                    }
                }
                {
                    var dfound = CheckModPath(path);
                    if (dfound != null)
                    {
#if EDITOR_LOADER_NO_CHECK
                        return dfound;
#endif
                        if (found == null)
                        {
                            found = dfound;
                        }
                        else
                        {
                            Debug.LogWarning("Duplicated item: " + found + "\nReplaces: " + dfound);
                        }
                    }
                }
#if EDITOR_LOADER_NO_CHECK
                return null;
#endif
                if (found == null)
                {
                    if (!noWarningWhenNotFound)
                    {
                        Debug.LogWarning("Not found: " + path);
                    }
                }
                else
                {
                    var guid = UnityEditor.AssetDatabase.AssetPathToGUID(found);
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.LogError("Unable to find asset (case error?): " + found);
                    }
                    var ondisk = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (found != ondisk)
                    {
                        Debug.LogError("File name case error. Loading: " + found + "\nOnDisk: " + (ondisk ?? "??"));
                    }
                }
                return found;
            }

            public static Object LoadMainAsset(string name)
            {
                Object rv = null;
                try
                {
                    rv = UnityEditor.AssetDatabase.LoadMainAssetAtPath(name);
                }
                catch { }
                if (rv == null || rv is GameObject || rv is Font)
                {
                    return rv;
                }
                if (rv is Texture2D)
                {
                    var assets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(name);
                    if (assets != null && assets.Length > 0)
                    {
                        return assets[0];
                    }
                }
                return rv;
            }
            public static Object LoadAsset(string asset, Type type)
            {
#if COMPATIBLE_RESMANAGER_V1
                asset = CompatibleAssetName(asset);
#endif
                var found = CheckDistributePath("CapsRes/" + asset);
                if (found != null)
                {
                    if (type == null)
                    {
                        return LoadMainAsset(found);
                    }
                    else
                    {
                        return UnityEditor.AssetDatabase.LoadAssetAtPath(found, type);
                    }
                }
                return null;
            }
            public static void LoadSceneImmediate(string name, bool additive)
            {
#if COMPATIBLE_RESMANAGER_V1
                name = CompatibleAssetName(name);
#endif
                var found = CheckDistributePath("CapsRes/" + name);
                if (found != null)
                {
                    if (additive)
                    {
                        UnityEditor.EditorApplication.LoadLevelAdditiveInPlayMode(found);
                    }
                    else
                    {
                        UnityEditor.EditorApplication.LoadLevelInPlayMode(found);
                    }
                }
            }

            public static void EditorStartupPrepare()
            {
                // Currently, we need to do nothing.
            }

            public Object LoadRes(string asset, Type type)
            {
                return LoadAsset(asset, type);
            }
            public void LoadScene(string name, bool additive)
            {
                LoadSceneImmediate(name, additive);
            }

            public CoroutineTasks.CoroutineWork LoadResAsync(string asset, Type type)
            {
                var work = new CoroutineTasks.CoroutineWorkSingle();
                work.Result = LoadRes(asset, type);
                return work;
            }
            public IEnumerator LoadSceneAsync(string name, bool additive)
            {
                LoadScene(name, additive);
                yield break;
            }

            public int Order { get { return LifetimeOrders.ResLoader; } }
            public void Prepare() { }
            public void Init()
            {
                EditorStartupPrepare();
            }
            public void Cleanup()
            {
            }
            public void UnloadUnusedRes()
            {
                UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
            }
            public void UnloadAllRes(bool unloadPermanentBundle)
            {
                UnloadUnusedRes();
            }
            public void MarkPermanent(string assetname)
            {
            }
        }
        public static EditorResLoader EditorResLoaderInstance = new EditorResLoader();
#endif
    }
}