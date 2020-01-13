using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    public static class CapsResInfoEditor
    {
        [MenuItem("Assets/Get Selected Asset Path (Raw)", priority = 2027)]
        public static void GetSelectedAssetPathRaw()
        {
            if (Selection.assetGUIDs != null)
            {
                var guid = Selection.assetGUIDs.First();
                if (guid != null)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path != null)
                    {
                        GUIUtility.systemCopyBuffer = path;
                        Debug.Log(path);
                    }
                }
            }
        }

        [MenuItem("Assets/Get Selected Asset Path", priority = 2028)]
        public static void GetSelectedAssetPath()
        {
            if (Selection.assetGUIDs != null)
            {
                var guid = Selection.assetGUIDs.First();
                if (guid != null)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path != null)
                    {
                        string norm = GetAssetNormPath(path);
                        GUIUtility.systemCopyBuffer = norm;
                        Debug.Log(norm);
                    }
                }
            }
        }
        [MenuItem("Res/Ping Asset in Clipboard &_c", priority = 200200)]
        public static void PingAssetInClipboard()
        {
            string path = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Clipboard is empty.");
                return;
            }
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset)
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(asset);
                return;
            }
            var real = path;
#if COMPATIBLE_RESMANAGER_V1
            real = ResManager.CompatibleAssetName(path);
            if (real != path)
            {
                path = real;
                asset = AssetDatabase.LoadMainAssetAtPath(real);
                if (asset)
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(asset);
                    return;
                }
            }
#endif
            real = FindDistributeAsset(path);
            if (!string.IsNullOrEmpty(real))
            {
                asset = AssetDatabase.LoadMainAssetAtPath(real);
                if (asset)
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(asset);
                    return;
                }
            }
            real = path.Replace('.', '/');
            real = "CapsSpt/" + real + ".lua";
            real = ResManager.EditorResLoader.CheckDistributePath(real);
            if (!string.IsNullOrEmpty(real))
            {
                asset = AssetDatabase.LoadMainAssetAtPath(real);
                if (asset)
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(asset);
                    return;
                }
            }
            Debug.Log("Can not find asset: " + path);
        }

        [MenuItem("Res/Delete Empty Folders", priority = 200210)]
        public static void DeleteEmptyAssetFolders()
        {
            var assets = AssetDatabase.GetAllAssetPaths();
            Array.Sort(assets, (a, b) => -Comparer<string>.Default.Compare(a, b));
            HashSet<string> nonEmptyFolders = new HashSet<string>();
            bool dirty = false;
            for (int i = 0; i < assets.Length; ++i)
            {
                var path = assets[i];
                if (System.IO.Directory.Exists(path) && !nonEmptyFolders.Contains(path))
                {
                    dirty = true;
                    Debug.LogError("Delete: " + path);
                    System.IO.Directory.Delete(path, true);
                }
                else
                {
                    var dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                    nonEmptyFolders.Add(dir);
                }
            }
            if (dirty)
            {
                AssetDatabase.Refresh();
            }
        }

        public static string GetAssetNormPath(string path)
        {
            if (path != null)
            {
                string type, mod, dist;
                string norm = ResManager.GetAssetNormPath(path, out type, out mod, out dist);
                if (string.IsNullOrEmpty(norm))
                {
                    norm = path;
                }
                if (type == "spt")
                {
                    if (norm.EndsWith(".lua"))
                    {
                        norm = norm.Substring(0, norm.Length - ".lua".Length);
                    }
                    norm = norm.Replace('/', '.');
                }
                return norm;
            }
            return null;
        }

        public static string FindDistributeAsset(string norm)
        {
            var strval = norm;
            if (strval.StartsWith("Assets/"))
            {
                if (PlatDependant.IsFileExist(strval))
                {
                    return strval;
                }
            }
            string real = strval;
            if (!real.StartsWith("CapsSpt/") && !real.StartsWith("CapsRes/"))
            {
                real = "CapsRes/" + real;
            }
            real = ResManager.EditorResLoader.CheckDistributePath(real);
            return real;
        }

        public static readonly HashSet<string> ScriptAssetExts = new HashSet<string>() { ".cs", ".js", ".boo" };
        public static bool IsAssetScript(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            //if (path.Contains("/CapsSpt/")) return true;
            var ext = System.IO.Path.GetExtension(path);
            return ScriptAssetExts.Contains(ext);
        }
    }
}
