using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static partial class ResManager
    {
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
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
            }
            return null;
        }
    }
}