using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using Object = UnityEngine.Object;
#endif

namespace Capstones.UnityEngineEx
{
    public static partial class ResManager
    {
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        public static string FindFileRelative(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (path[0] != '\\' && path[0] != '/')
            {
                path = "/" + path;
            }
            var spath = ThreadSafeValues.UpdatePath + path;
            if (PlatDependant.IsFileExist(spath))
            {
                return spath;
            }
            spath = ThreadSafeValues.AppStreamingAssetsPath + path;
            if (PlatDependant.IsFileExist(spath))
            {
                return spath;
            }
            return null;
        }
        private static string FindFileInMod(string path, string mod)
        {
            var realpath = path;
            if (!string.IsNullOrEmpty(mod))
            {
                realpath = "/mod/" + mod + path;
            }
            return FindFileRelative(realpath);
        }
        private static string FindFileInMods(string path, out string foundmod)
        {
            var flags = ResManager.GetValidDistributeFlags();
            for (int j = flags.Length - 1; j >= 0; --j)
            {
                var mod = flags[j];
                var found = FindFileInMod(path, mod);
                if (found != null)
                {
                    foundmod = mod;
                    return found;
                }
            }
            {
                var found = FindFileInMod(path, null);
                if (found != null)
                {
                    foundmod = null;
                    return found;
                }
            }
            foundmod = null;
            return null;
        }
        private static string FindFileInDist(string path, string dist, out string mod)
        {
            var realpath = path;
            if (!string.IsNullOrEmpty(dist))
            {
                realpath = "/dist/" + dist + path;
            }
            return FindFileInMods(realpath, out mod);
        }
        private static string FindFileInDists(string path, out string mod, out string founddist)
        {
            var flags = ResManager.GetValidDistributeFlags();
            for (int i = flags.Length - 1; i >= 0; --i)
            {
                var dist = flags[i];
                var found = FindFileInDist(path, dist, out mod);
                if (found != null)
                {
                    founddist = dist;
                    return found;
                }
            }
            {
                var found = FindFileInDist(path, null, out mod);
                if (found != null)
                {
                    founddist = null;
                    return found;
                }
            }
            mod = null;
            founddist = null;
            return null;
        }
        public static string FindFile(string path, out string mod, out string dist)
        {
            if (string.IsNullOrEmpty(path))
            {
                mod = null;
                dist = null;
                return null;
            }
            if (path[0] != '\\' && path[0] != '/')
            {
                path = "/" + path;
            }
            return FindFileInDists(path, out mod, out dist);
        }
        public static string FindFile(string path)
        {
            string mod, dist;
            return FindFile(path, out mod, out dist);
        }
        public static System.IO.Stream LoadFileRelative(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (path[0] != '\\' && path[0] != '/')
            {
                path = "/" + path;
            }
            var spath = ThreadSafeValues.UpdatePath + path;
            try
            {
                if (PlatDependant.IsFileExist(spath))
                {
                    return PlatDependant.OpenRead(spath);
                }
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
            spath = ThreadSafeValues.AppStreamingAssetsPath + path;
            try
            {
                if (PlatDependant.IsFileExist(spath))
                {
                    return PlatDependant.OpenRead(spath);
                }
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
            return null;
        }
        private static System.IO.Stream LoadFileInMod(string path, string mod)
        {
            var realpath = path;
            if (!string.IsNullOrEmpty(mod))
            {
                realpath = "/mod/" + mod + path;
            }
            return LoadFileRelative(realpath);
        }
        private static System.IO.Stream LoadFileInMods(string path)
        {
            var flags = ResManager.GetValidDistributeFlags();
            for (int j = flags.Length - 1; j >= 0; --j)
            {
                var mod = flags[j];
                var stream = LoadFileInMod(path, mod);
                if (stream != null)
                {
                    return stream;
                }
            }
            {
                var stream = LoadFileInMod(path, null);
                if (stream != null)
                {
                    return stream;
                }
            }
            return null;
        }
        private static System.IO.Stream LoadFileInDist(string path, string dist)
        {
            var realpath = path;
            if (!string.IsNullOrEmpty(dist))
            {
                realpath = "/dist/" + dist + path;
            }
            return LoadFileInMods(realpath);
        }
        private static System.IO.Stream LoadFileInDists(string path)
        {
            var flags = ResManager.GetValidDistributeFlags();
            for (int i = flags.Length - 1; i >= 0; --i)
            {
                var dist = flags[i];
                var stream = LoadFileInDist(path, dist);
                if (stream != null)
                {
                    return stream;
                }
            }
            {
                var stream = LoadFileInDist(path, null);
                if (stream != null)
                {
                    return stream;
                }
            }
            return null;
        }
        public static System.IO.Stream LoadFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (path[0] != '\\' && path[0] != '/')
            {
                path = "/" + path;
            }
            return LoadFileInDists(path);
        }
        public static string LoadText(string path)
        {
            using (var stream = LoadFile(path))
            {
                if (stream != null)
                {
                    try
                    {
                        var sr = new System.IO.StreamReader(stream);
                        return sr.ReadToEnd();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
            return null;
        }
#endif
        public static string LoadConfig(string file, out Dictionary<string, string> config)
        {
            config = null;
            if (string.IsNullOrEmpty(file))
            {
                return "LoadConfig - filename is empty";
            }
            else
            {
                try
                {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    TextAsset txt = ResManager.LoadResDeep(file, typeof(TextAsset)) as TextAsset;
                    if (txt == null)
                    {
                        return "LoadConfig - cannot load file: " + file;
                    }
                    else
                    {
                        JSONObject json = new JSONObject(txt.text);
                        config = json.ToDictionary();
                        return null;
                    }
#else
                    var text = LoadText(file);
                    if (string.IsNullOrEmpty(text))
                    {
                        return "LoadConfig - cannot load file: " + file;
                    }
                    else
                    {
                        JSONObject json = new JSONObject(text);
                        config = json.ToDictionary();
                        return null;
                    }
#endif
                }
                catch (Exception e)
                {
                    return e.ToString();
                }
            }
        }
        public static Dictionary<string, string> LoadConfig(string file)
        {
            Dictionary<string, string> config;
            var error = LoadConfig(file, out config);
            if (error != null)
            {
                PlatDependant.LogError(error);
            }
            return config;
        }
        public static Dictionary<string, string> TryLoadConfig(string file)
        {
            Dictionary<string, string> config;
            LoadConfig(file, out config);
            return config;
        }

        public static Dictionary<string, object> LoadFullConfig(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                PlatDependant.LogError("LoadConfig - filename is empty");
            }
            else
            {
                try
                {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    TextAsset txt = ResManager.LoadResDeep(file, typeof(TextAsset)) as TextAsset;
                    if (txt == null)
                    {
                        PlatDependant.LogError("LoadConfig - cannot load file: " + file);
                    }
                    else
                    {
                        JSONObject json = new JSONObject(txt.text);
                        if (json.IsObject)
                        {
                            return json.ToObject() as Dictionary<string, object>;
                        }
                    }
#else
                    var text = LoadText(file);
                    if (string.IsNullOrEmpty(text))
                    {
                        PlatDependant.LogError("LoadConfig - cannot load file: " + file);
                    }
                    else
                    {
                        JSONObject json = new JSONObject(text);
                        if (json.IsObject)
                        {
                            return json.ToObject() as Dictionary<string, object>;
                        }
                    }
#endif
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
            }
            return null;
        }
    }

    public static class ConfigManager
    {
        public static Dictionary<string, object> LoadConfig(string file)
        {
            return ResManager.LoadFullConfig(file);
        }

        public static bool ToBoolean(object config)
        {
            if (config == null)
            {
                return false;
            }
            if (config is bool)
            {
                return (bool)config;
            }
            if (config is string)
            {
                var str = (string)config;
                str = str.ToLower().Trim();
                if (str == "" || str == "n" || str == "no" || str == "f" || str == "false")
                {
                    return false;
                }
                return true;
            }
            else if (config is IntPtr)
            {
                return ((IntPtr)config) != IntPtr.Zero;
            }
            else if (config is UIntPtr)
            {
                return ((UIntPtr)config) != UIntPtr.Zero;
            }
            if (PlatDependant.IsObjIConvertible(config))
            {
                return Convert.ToBoolean(config);
            }
            return true;
        }
        public static string ToString(object config)
        {
            if (config == null)
            {
                return null;
            }
            if (config is string)
            {
                return (string)config;
            }
            return config.ToString();
        }
        public static int ToInt32(object config)
        {
            if (config == null)
            {
                return 0;
            }
            if (config is int)
            {
                return (int)config;
            }
            if (config is string)
            {
                var str = (string)config;
                int rv;
                int.TryParse(str, out rv);
                return rv;
            }
            else if (config is IntPtr)
            {
                return (int)(IntPtr)config;
            }
            else if (config is UIntPtr)
            {
                return (int)(UIntPtr)config;
            }
            if (PlatDependant.IsObjIConvertible(config))
            {
                return Convert.ToInt32(config);
            }
            return 0;
        }

        public static object GetObject(this IDictionary<string, object> dict, string key)
        {
            if (dict != null)
            {
                object config;
                if (dict.TryGetValue(key, out config))
                {
                    return config;
                }
            }
            return null;
        }
        public static bool GetBoolean(this IDictionary<string, object> dict, string key)
        {
            if (dict != null)
            {
                object config;
                if (dict.TryGetValue(key, out config))
                {
                    return ToBoolean(config);
                }
            }
            return false;
        }
        public static string GetString(this IDictionary<string, object> dict, string key)
        {
            if (dict != null)
            {
                object config;
                if (dict.TryGetValue(key, out config))
                {
                    return ToString(config);
                }
            }
            return null;
        }
        public static int GetInt32(this IDictionary<string, object> dict, string key)
        {
            if (dict != null)
            {
                object config;
                if (dict.TryGetValue(key, out config))
                {
                    return ToInt32(config);
                }
            }
            return 0;
        }
    }
}