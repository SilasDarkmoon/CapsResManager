using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class ProfilerEx
    {
        static ProfilerEx()
        {
#if PROFILER_EX_FRAME_TIMER_AUTO_LOG
            FrameTimerAutoLog = true;
#endif
#if PROFILER_EX_FRAME_TIMER_AUTO_LOG_ONLY_LAG
            FrameTimerAutoLogOnlyLag = true;
#endif
        }

        public static bool IsProfilerEnabled()
        {
#if ENABLE_PROFILER
            return true;
#else
            return false;
#endif
        }

        public static bool IsDeepProfiling()
        {
#if ENABLE_PROFILER
            List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle> allrecorders = new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
            Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(allrecorders);
            for (int i = 0; i < allrecorders.Count; ++i)
            {
                var recorder = allrecorders[i];
                var desc = Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(recorder);
                if ((desc.Flags & Unity.Profiling.LowLevel.MarkerFlags.ScriptDeepProfiler) != 0)
                {
                    return true;
                }
            }
#endif
            return false;
        }

        public static bool FrameTimerAutoLog = false;
        public static bool FrameTimerAutoLogOnlyLag = false;

        private static int _FrameTimerFrameIndex = 0;
        private static double _FrameTimerLastInterval = 0;
        private static System.Diagnostics.Stopwatch _FrameTimerWatch;
        private static void FrameTimerUpdate()
        {
            if (_FrameTimerWatch == null)
            {
                _FrameTimerWatch = new System.Diagnostics.Stopwatch();
                _FrameTimerFrameIndex = UnityEngine.Time.frameCount;
                _FrameTimerWatch.Start();
            }
            else
            {
                _FrameTimerWatch.Stop();
                _FrameTimerLastInterval = _FrameTimerWatch.Elapsed.TotalMilliseconds;
                if (FrameTimerAutoLog)
                {
                    if (!FrameTimerAutoLogOnlyLag || Application.targetFrameRate <= 0 || _FrameTimerLastInterval > 1050.0 / Application.targetFrameRate)
                    {
                        Debug.LogFormat("Frame time {0}: {1} ms.", _FrameTimerFrameIndex, _FrameTimerLastInterval);
                    }
                }
                _FrameTimerFrameIndex = UnityEngine.Time.frameCount;
                _FrameTimerWatch.Restart();
            }
        }

        public static void InitFrameTimer()
        {
            var oldloop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            var subs = oldloop.subSystemList;
            for (int i = 0; i < subs.Length; ++i)
            {
                var oldsub = subs[i];
                if (oldsub.type == typeof(ProfilerEx))
                {
                    return;
                }
            }

            var newsubs = new UnityEngine.LowLevel.PlayerLoopSystem[subs.Length + 1];
            for (int i = 0; i < subs.Length; ++i)
            {
                newsubs[i + 1] = subs[i];
            }
            newsubs[0] = new UnityEngine.LowLevel.PlayerLoopSystem()
            {
                type = typeof(ProfilerEx),
                updateDelegate = FrameTimerUpdate,
            };

            oldloop.subSystemList = newsubs;
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(oldloop);
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitFrameTimerConditional()
        {
#if PROFILER_EX_FRAME_TIMER
            InitFrameTimer();
#endif
        }
    }

    public struct ProfilerContext : IDisposable
    {
        public ProfilerContext(string name)
        {
#if ENABLE_PROFILER
            UnityEngine.Profiling.Profiler.BeginSample(name);
#endif
        }

        public static ProfilerContext Create(string name)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(name);
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<T>(T name)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(name.ToString());
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P>(string nameformat, P p)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2>(string nameformat, P1 p1, P2 p2)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3>(string nameformat, P1 p1, P2 p2, P3 p3)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4, P5>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4, p5));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4, P5, P6>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4, p5, p6));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4, P5, P6, P7>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4, p5, p6, p7));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4, P5, P6, P7, P8>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4, p5, p6, p7, p8));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create(string nameformat, params object[] args)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, args));
#else
            return default(ProfilerContext);
#endif
        }

        public void Dispose()
        {
#if ENABLE_PROFILER
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }
    }
}