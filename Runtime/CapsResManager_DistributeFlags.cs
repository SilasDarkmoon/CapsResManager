using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static partial class ResManager
    {
        private static List<string> _PreRuntimeDFlags;
        public static List<string> PreRuntimeDFlags
        {
            get
            {
                if (_PreRuntimeDFlags == null)
                {
                    _PreRuntimeDFlags = new List<string>();
                    TextAsset txt = Resources.Load<TextAsset>("DistributeFlags");
                    if (txt != null)
                    {
                        var strflags = txt.text;
                        if (!string.IsNullOrEmpty(strflags))
                        {
                            var cflags = strflags.Split(new[] { '<' }, System.StringSplitOptions.RemoveEmptyEntries);
                            if (cflags != null)
                            {
                                _PreRuntimeDFlags.AddRange(cflags);
                            }
                        }
                    }
                }
                return _PreRuntimeDFlags;
            }
#if UNITY_EDITOR
            set
            {
                _PreRuntimeDFlags = value;
                _RuntimeCachedDFlags = null;
            }
#endif
        }

        private static HashSet<string> _RuntimeForbiddenDFlags;
        public static HashSet<string> RuntimeForbiddenDFlags
        {
            get
            {
                if (_RuntimeForbiddenDFlags == null)
                {
                    _RuntimeForbiddenDFlags = new HashSet<string>();
#if !UNITY_EDITOR
                    var strflags = PlayerPrefs.GetString("___Pref__ForbiddenDistributeFlags");
                    if (!string.IsNullOrEmpty(strflags))
                    {
                        var cflags = strflags.Split(new[] { '<' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (cflags != null)
                        {
                            _RuntimeForbiddenDFlags.UnionWith(cflags);
                        }
                    }
#endif
                }
                return _RuntimeForbiddenDFlags;
            }
        }

        private static List<string> _RuntimeExDFlags;
        public static List<string> RuntimeExDFlags
        {
            get
            {
                if (_RuntimeExDFlags == null)
                {
                    _RuntimeExDFlags = new List<string>();
#if !UNITY_EDITOR
                    var strflags = PlayerPrefs.GetString("___Pref__OptionalDistributeFlags");
                    if (!string.IsNullOrEmpty(strflags))
                    {
                        var cflags = strflags.Split(new[] { '<' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (cflags != null)
                        {
                            var forbiddenFlags = RuntimeForbiddenDFlags;
                            for (int i = 0; i < cflags.Length; ++i)
                            {
                                var nflag = cflags[i];
                                if (!forbiddenFlags.Contains(nflag))
                                {
                                    _RuntimeExDFlags.Add(nflag);
                                }
                            }
                        }
                    }
#endif
                }
                return _RuntimeExDFlags;
            }
        }

        private static string[] _RuntimeCachedDFlags;
        private static HashSet<string> _RuntimeCachedDFlagsSet = new HashSet<string>();
        private static string[] _RuntimeCachedValidDFlags;
        private static HashSet<string> _RuntimeCachedValidDFlagsSet = new HashSet<string>();
        public static string[] GetDistributeFlags()
        {
            if (_RuntimeCachedDFlags == null)
            {
                _RuntimeCachedValidDFlags = null;
                _RuntimeCachedValidDFlagsSet.Clear();
#if UNITY_EDITOR
                if (!EditorOnPlayModeChange_ClearRuntimeCachedDFlags_Listening)
                {
                    EditorBridge.PrePlayModeChange += () =>
                    {
                        _RuntimeCachedDFlags = null;
                        _RuntimeExDFlags = null;
                        _RuntimeForbiddenDFlags = null;
                    };
                    EditorOnPlayModeChange_ClearRuntimeCachedDFlags_Listening = true;
                }
#endif
                var fflags = RuntimeForbiddenDFlags;
                var exflags = RuntimeExDFlags;
                var pflags = PreRuntimeDFlags;

                HashSet<string> ignoreflags = new HashSet<string>(fflags);
                ignoreflags.UnionWith(exflags);

                List<string> flags = new List<string>();
                for (int i = 0; i < pflags.Count; ++i)
                {
                    var nflag = pflags[i];
                    if (!ignoreflags.Contains(nflag))
                    {
                        flags.Add(nflag);
                    }
                }
                flags.AddRange(exflags);
                _RuntimeCachedDFlags = flags.ToArray();
                _RuntimeCachedDFlagsSet.Clear();
                _RuntimeCachedDFlagsSet.UnionWith(_RuntimeCachedDFlags);
            }
            return _RuntimeCachedDFlags;
        }
        public static HashSet<string> GetDistributeFlagsSet()
        {
            GetDistributeFlags();
            return _RuntimeCachedDFlagsSet;
        }
        public static string[] GetValidDistributeFlags()
        {
            var dflags = GetDistributeFlags();
            if (_RuntimeCachedValidDFlags == null)
            {
                List<string> validflags = new List<string>(dflags.Length);
                for (int i = 0; i < dflags.Length; ++i)
                {
                    var flag = dflags[i];
                    if (CheckDistributeDep(flag))
                    {
                        validflags.Add(flag);
                    }
                }
                _RuntimeCachedValidDFlags = validflags.ToArray();
                _RuntimeCachedValidDFlagsSet.Clear();
                _RuntimeCachedValidDFlagsSet.UnionWith(validflags);
            }
            return _RuntimeCachedValidDFlags;
        }
        public static HashSet<string> GetValidDistributeFlagsSet()
        {
            GetValidDistributeFlags();
            return _RuntimeCachedValidDFlagsSet;
        }
#if UNITY_EDITOR
        private static bool EditorOnPlayModeChange_ClearRuntimeCachedDFlags_Listening = false;
#endif

        public static void RemoveDistributeFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag))
            {
                return;
            }
            var fflags = RuntimeForbiddenDFlags;
            if (fflags.Contains(flag))
            {
                return;
            }
            var exflags = RuntimeExDFlags;
            bool exflagsChanged = false;
            for (int i = 0; i < exflags.Count; ++i)
            {
                if (exflags[i] == flag)
                {
                    exflagsChanged = true;
                    exflags.RemoveAt(i--);
                }
            }
            if (exflagsChanged)
            {
#if !UNITY_EDITOR
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < exflags.Count; ++i)
                {
                    sb.Append('<');
                    sb.Append(exflags[i]);
                }
                PlayerPrefs.SetString("___Pref__OptionalDistributeFlags", sb.ToString());
                PlayerPrefs.Save();
#endif
                _RuntimeCachedDFlags = null;
            }

            var pflags = PreRuntimeDFlags;
            for (int i = 0; i < pflags.Count; ++i)
            {
                if (pflags[i] == flag)
                {
                    fflags.Add(flag);
#if !UNITY_EDITOR
                    var sb = new System.Text.StringBuilder();
                    foreach (var nflag in fflags)
                    {
                        sb.Append('<');
                        sb.Append(nflag);
                    }
                    PlayerPrefs.SetString("___Pref__ForbiddenDistributeFlags", sb.ToString());
                    PlayerPrefs.Save();
#endif
                    _RuntimeCachedDFlags = null;
                    break;
                }
            }
        }
        public static void AddDistributeFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag))
            {
                return;
            }
            var flags = GetDistributeFlags();
            if (flags.Length > 0)
            {
                if (flags[flags.Length - 1] == flag)
                {
                    return;
                }
            }
            var fflags = RuntimeForbiddenDFlags;
#if UNITY_EDITOR
            fflags.Remove(flag);
#else
            if (fflags.Remove(flag))
            {
                var sb = new System.Text.StringBuilder();
                foreach (var nflag in fflags)
                {
                    sb.Append('<');
                    sb.Append(nflag);
                }
                PlayerPrefs.SetString("___Pref__ForbiddenDistributeFlags", sb.ToString());
                PlayerPrefs.Save();
            }
#endif
            var exflags = RuntimeExDFlags;
            for (int i = 0; i < exflags.Count; ++i)
            {
                if (exflags[i] == flag)
                {
                    exflags.RemoveAt(i--);
                }
            }
            exflags.Add(flag);
#if !UNITY_EDITOR
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < exflags.Count; ++i)
                {
                    sb.Append('<');
                    sb.Append(exflags[i]);
                }
                PlayerPrefs.SetString("___Pref__OptionalDistributeFlags", sb.ToString());
                PlayerPrefs.Save();
            }
#endif
            _RuntimeCachedDFlags = null;
        }
        public static void ReloadDistributeFlags()
        {
            _RuntimeCachedDFlags = null;
        }

        private readonly static Dictionary<string, CapsModDesc> _LoadedDistributeDescs = new Dictionary<string, CapsModDesc>();
        public static void ClearLoadedDistributeDescs()
        {
            _LoadedDistributeDescs.Clear();
        }
        public static CapsModDesc GetDistributeDesc(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
            {
#if !UNITY_EDITOR
                CapsModDesc cacheddesc;
                if (_LoadedDistributeDescs.TryGetValue(flag, out cacheddesc) && (cacheddesc != null || ReferenceEquals(cacheddesc, null)))
                {
                    return cacheddesc;
                }
                try
                {
                    var udescdir = ThreadSafeValues.UpdatePath + "/res/moddesc/";
                    var udescfile = udescdir + flag + ".md.txt";
                    if (PlatDependant.IsFileExist(udescfile))
                    {
                        using (var sr = PlatDependant.OpenReadText(udescfile))
                        {
                            var json = sr.ReadToEnd();
                            var desc = ScriptableObject.CreateInstance<CapsModDesc>();
                            JsonUtility.FromJsonOverwrite(json, desc);
                            if (desc.Mod != null)
                            {
                                _LoadedDistributeDescs[flag] = desc;
                                return desc;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
#endif
                var descs = Resources.LoadAll<CapsModDesc>("resdesc");
                if (descs != null)
                {
#if UNITY_EDITOR
                    try
                    {
#endif
                        for (int i = 0; i < descs.Length; ++i)
                        {
                            var desc = descs[i];
                            if (desc != null && desc.Mod == flag)
                            {
#if UNITY_EDITOR
                                return uobj.Instantiate<CapsModDesc>(desc);
#else
                                _LoadedDistributeDescs[flag] = desc;
                                return desc;
#endif
                            }
                        }
#if UNITY_EDITOR
                    }
                    finally
                    {
                        for (int i = 0; i < descs.Length; ++i)
                        {
                            var desc = descs[i];
                            if (desc != null)
                            {
                                Resources.UnloadAsset(desc);
                            }
                        }
                    }
#endif
                }
            }
#if !UNITY_EDITOR
            _LoadedDistributeDescs[flag] = null;
#endif
            return null;
        }
        public static bool CheckDistributeDep(string flag)
        {
            var desc = GetDistributeDesc(flag);
            if (desc != null)
            {
                if (desc.Deps != null)
                {
                    var dflags = GetDistributeFlagsSet();
                    for (int i = 0; i < desc.Deps.Length; ++i)
                    {
                        var dep = desc.Deps[i];
                        if (!string.IsNullOrEmpty(dep))
                        {
                            if (!dflags.Contains(dep))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        public static string GetAssetNormPath(string rawpath, out string type, out string mod, out string dist)
        {
            if (rawpath != null)
            {
                rawpath = rawpath.Replace('\\', '/');
                string norm = rawpath;
                mod = "";
                if (rawpath.StartsWith("Assets/Mods/"))
                {
                    var sub = rawpath.Substring("Assets/Mods/".Length);
                    var index = sub.IndexOf('/');
                    if (index < 0)
                    {
                        mod = sub;
                        type = null;
                        dist = "";
                        return "";
                    }
                    mod = sub.Substring(0, index);
                    norm = sub.Substring(index + 1);
                }
                else if (rawpath.StartsWith("Packages/"))
                {
                    var sub = rawpath.Substring("Packages/".Length);
                    var index = sub.IndexOf('/');
                    if (index < 0)
                    {
                        mod = sub;
                    }
                    else
                    {
                        mod = sub.Substring(0, index);
                        norm = sub.Substring(index + 1);
                    }
#if UNITY_EDITOR
                    mod = EditorToClientUtils.GetModNameFromPackageName(mod);
#endif
                    if (index < 0)
                    {
                        type = null;
                        dist = "";
                        return "";
                    }
                }
                else if (rawpath.StartsWith("Assets/"))
                {
                    mod = "";
                    norm = rawpath.Substring("Assets/".Length);
                }
                else
                {
                    mod = "";
                    dist = "";
                    type = null;
                    return rawpath;
                }
                if (norm.StartsWith("CapsSpt/"))
                {
                    type = "spt";
                    norm = norm.Substring("CapsSpt/".Length);
                }
                else if (norm.StartsWith("CapsRes/"))
                {
                    type = "res";
                    norm = norm.Substring("CapsRes/".Length);
                }
                else
                {
                    dist = "";
                    type = null;
                    if (string.IsNullOrEmpty(mod))
                    {
                        return rawpath;
                    }
                    else
                    {
                        return norm;
                    }
                }
                if (norm.StartsWith("dist/"))
                {
                    var sub = norm.Substring("dist/".Length);
                    var index = sub.IndexOf('/');
                    if (index < 0)
                    {
                        dist = sub;
                        return "";
                    }
                    dist = sub.Substring(0, index);
                    norm = sub.Substring(index + 1);
                }
                else
                {
                    dist = "";
                }
                return norm;
            }
            else
            {
                type = null;
                mod = null;
                dist = null;
                return null;
            }
        }
    }
}