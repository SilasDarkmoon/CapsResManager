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

        //public static readonly HashSet<string> ScriptAssetExts = new HashSet<string>() { ".cs", ".js", ".boo" };
        //public static bool IsAssetScript(string path)
        //{
        //    if (string.IsNullOrEmpty(path)) return false;
        //    if (path.Contains("/CapsSpt/")) return true;
        //    var ext = System.IO.Path.GetExtension(path);
        //    return ScriptAssetExts.Contains(ext);
        //}
    }
}
