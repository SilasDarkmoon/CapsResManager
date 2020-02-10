using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using uobj = UnityEngine.Object;
#endif

namespace Capstones.UnityEngineEx
{
    public static class ThreadSafeValues
    {
        static ThreadSafeValues()
        {
            Init();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void Init()
        {
            _UpdatePath = IsolatedPrefs.GetUpdatePath();
            _IsolatedPath = IsolatedPrefs.GetIsolatedPath();
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            _cached_Application_platform = Application.platform.ToString();
            _cached_Application_streamingAssetsPath = Application.streamingAssetsPath;
            _cached_Application_temporaryCachePath = Application.temporaryCachePath;
            _cached_Application_persistentDataPath = Application.persistentDataPath;
            _cached_Application_dataPath = Application.dataPath;
            _cached_Capid = IsolatedPrefs.IsolatedID;
            _UnityThreadID = ThreadLocalObj.GetThreadId();
#else
#if NETCOREAPP
            _cached_Application_platform = "DotNetCore";
#else
            _cached_Application_platform = "DotNet";
#endif
            _cached_Application_streamingAssetsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "streaming");
            _cached_Application_temporaryCachePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
            _cached_Application_persistentDataPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime");
            _cached_Application_dataPath = AppDomain.CurrentDomain.BaseDirectory;
            _cached_Capid = IsolatedPrefs.IsolatedID;
            _UnityThreadID = (ulong)System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
            _IsMainThread = true;
        }

        private static string _UpdatePath;
        private static string _IsolatedPath;
        private static string _cached_Application_platform;
        private static string _cached_Application_streamingAssetsPath;
        private static string _cached_Application_temporaryCachePath;
        private static string _cached_Application_persistentDataPath;
        private static string _cached_Application_dataPath;
        private static string _cached_Capid;
        private static ulong _UnityThreadID;
        [ThreadStatic] private static bool _IsMainThread;

        public static string UpdatePath { get { return _UpdatePath; } }
        public static string IsolatedPath { get { return _IsolatedPath; } }
        public static string AppPlatform { get { return _cached_Application_platform; } }
        public static string AppStreamingAssetsPath { get { return _cached_Application_streamingAssetsPath; } }
        public static string AppTemporaryCachePath { get { return _cached_Application_temporaryCachePath; } }
        public static string AppPersistentDataPath { get { return _cached_Application_persistentDataPath; } }
        public static string AppDataPath { get { return _cached_Application_dataPath; } }
        public static string Capid { get { return _cached_Capid; } }
        public static ulong UnityThreadID { get { return _UnityThreadID; } }
        public static bool IsMainThread { get { return _IsMainThread; } }
        public static ulong ThreadId
        {
            get
            {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                return ThreadLocalObj.GetThreadId();
#else
                return (ulong)System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
            }
        }
    }
}