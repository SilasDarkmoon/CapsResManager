using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class ThreadSafeValues
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            _UpdatePath = IsolatedPrefs.GetIsolatedPath();
            _cached_Application_platform = Application.platform.ToString();
            _cached_Application_streamingAssetsPath = Application.streamingAssetsPath;
            _cached_Application_temporaryCachePath = Application.temporaryCachePath;
            _cached_Application_persistentDataPath = Application.persistentDataPath;
            _cached_Application_dataPath = Application.dataPath;
            _cached_Capid = IsolatedPrefs.IsolatedID;
            _UnityThreadID = ThreadLocalObj.GetThreadId();
        }

        private static string _UpdatePath;
        private static string _cached_Application_platform;
        private static string _cached_Application_streamingAssetsPath;
        private static string _cached_Application_temporaryCachePath;
        private static string _cached_Application_persistentDataPath;
        private static string _cached_Application_dataPath;
        private static string _cached_Capid;
        private static ulong _UnityThreadID;

        public static string UpdatePath { get { return _UpdatePath; } }
        public static string AppPlatform { get { return _cached_Application_platform; } }
        public static string AppStreamingAssetsPath { get { return _cached_Application_streamingAssetsPath; } }
        public static string AppTemporaryCachePath { get { return _cached_Application_temporaryCachePath; } }
        public static string AppPersistentDataPath { get { return _cached_Application_persistentDataPath; } }
        public static string AppDataPath { get { return _cached_Application_dataPath; } }
        public static string Capid { get { return _cached_Capid; } }
        public static ulong UnityThreadID { get { return _UnityThreadID; } }
    }
}