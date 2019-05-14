using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class UnityThreadDispatcher
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Init()
        {
            CheckAndInit();
        }
        public static void RunInUnityThread(Action act)
        {
            AddEvent(act);
        }
        public static void RunInUnityThreadAndWait(Action act)
        {
            if (act != null)
            {
                if (PlatDependant.GetThreadId() == ThreadSafeValues.UnityThreadID)
                {
                    act();
                }
                else
                {
                    System.Threading.ManualResetEvent waithandle = new System.Threading.ManualResetEvent(false);
                    AddEvent(() =>
                    {
                        act();
                        waithandle.Set();
                    });
                    waithandle.WaitOne();
                    waithandle.Close();
                }
            }
        }
        public static T RunInUnityThreadAndWait<T>(Func<T> func)
        {
            if (func != null)
            {
                if (PlatDependant.GetThreadId() == ThreadSafeValues.UnityThreadID)
                {
                    return func();
                }
                else
                {
                    T rv = default(T);
                    System.Threading.ManualResetEvent waithandle = new System.Threading.ManualResetEvent(false);
                    AddEvent(() =>
                    {
                        rv = func();
                        waithandle.Set();
                    });
                    waithandle.WaitOne();
                    waithandle.Close();
                    return rv;
                }
            }
            return default(T);
        }

#pragma warning disable 0414
        private static Unity.Collections.Concurrent.ConcurrentQueue<Action> ActionQueue = new Unity.Collections.Concurrent.ConcurrentQueue<Action>();
        private static bool _Inited = false;
        private static bool _UsingObjRunner = false;
        internal static GameObject _RunningObj = null;
#pragma warning restore

        private static void CheckAndInit()
        {
#if UNITY_EDITOR
            if (!_Inited)
            {
                _Inited = true;
                EditorBridge.WeakUpdate += HandleEvents;
            }
            return;
#else
#if MOD_NATIVEUNITYTHREADDISPATCHER
            if (Native.NativeUnityThreadDispatcher.Ready)
            {
                if (!_Inited)
                {
                    _Inited = true;
                    Native.NativeUnityThreadDispatcher.HandleEventsInUnityThread += HandleEvents;
                }
                return;
            }
#endif
            _UsingObjRunner = true;
            if (!_RunningObj)
            {
                _RunningObj = new GameObject();
                _RunningObj.AddComponent<UnityThreadDispatcherBehav>();
                GameObject.DontDestroyOnLoad(_RunningObj);
                _RunningObj.hideFlags = HideFlags.HideAndDontSave;
            }
#endif
        }
        private static void AddEvent(Action act)
        {
            ActionQueue.Enqueue(act);
            if (PlatDependant.GetThreadId() == ThreadSafeValues.UnityThreadID)
            {
                HandleEvents();
                return;
            }
#if !UNITY_EDITOR
#if MOD_NATIVEUNITYTHREADDISPATCHER
            if (_Inited && !_UsingObjRunner)
            {
                Native.NativeUnityThreadDispatcher.TrigEventInUnityThread();
            }
#endif
#endif
        }
        internal static void HandleEvents()
        {
            Action act = null;
            while (ActionQueue.TryDequeue(out act))
            {
                if (act != null)
                {
                    try
                    {
                        act();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
        }
    }
}