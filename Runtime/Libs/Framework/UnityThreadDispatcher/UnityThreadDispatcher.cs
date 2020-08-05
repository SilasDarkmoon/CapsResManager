using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class UnityThreadDispatcher
    {
        public interface INativeUnityThreadDispatcher
        {
            bool Ready { get; }
            event Action HandleEventsInUnityThread;
            void TrigEventInUnityThread();
        }
        public static INativeUnityThreadDispatcher NativeUnityThreadDispatcherWrapper;

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
                if (ThreadSafeValues.IsMainThread)
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
                if (ThreadSafeValues.IsMainThread)
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
#if !NET_4_6 && !NET_STANDARD_2_0
        private static Unity.Collections.Concurrent.ConcurrentQueue<Action> ActionQueue = new Unity.Collections.Concurrent.ConcurrentQueue<Action>();
#else
        private static System.Collections.Concurrent.ConcurrentQueue<Action> ActionQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();
#endif
        private static bool _Inited = false;
        private static bool _UsingObjRunner = false;
        internal static GameObject _RunningObj = null;
        private static System.Threading.SynchronizationContext _MainThreadSyncContext;
#pragma warning restore

        private static void CheckAndInit()
        {
#if UNITY_2017_1_OR_NEWER
            try
            {
                _MainThreadSyncContext = System.Threading.SynchronizationContext.Current;
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
            if (_MainThreadSyncContext != null)
            {
                return;
            }
#endif
#if UNITY_EDITOR
            if (!_Inited)
            {
                _Inited = true;
                EditorBridge.WeakUpdate += HandleEvents;
            }
            return;
#else
#if MOD_NATIVEUNITYTHREADDISPATCHER
            if (NativeUnityThreadDispatcherWrapper != null && NativeUnityThreadDispatcherWrapper.Ready)
            {
                if (!_Inited)
                {
                    _Inited = true;
                    NativeUnityThreadDispatcherWrapper.HandleEventsInUnityThread += HandleEvents;
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
            if (_MainThreadSyncContext != null)
            {
                if (act != null)
                {
                    if (ThreadSafeValues.IsMainThread)
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
                    else
                    {
                        _MainThreadSyncContext.Post(state =>
                        {
                            try
                            {
                                act();
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }, null);
                    }
                }
                return;
            }
            ActionQueue.Enqueue(act);
            if (ThreadSafeValues.IsMainThread)
            {
                HandleEvents();
                return;
            }
#if !UNITY_EDITOR
#if MOD_NATIVEUNITYTHREADDISPATCHER
            if (_Inited && !_UsingObjRunner)
            {
                NativeUnityThreadDispatcherWrapper.TrigEventInUnityThread();
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