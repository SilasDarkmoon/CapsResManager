﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static partial class ResManager
    {
        public static class GarbageCollector
        {
            public class GarbageCollectorEvent
            {
                public struct GarbageCollectorDelegate
                {
                    public Func<IEnumerator> FuncAsyc;
                    public Action Func;
                }
                protected readonly List<GarbageCollectorDelegate> _CollectGarbageFuncs = new List<GarbageCollectorDelegate>();

                public static GarbageCollectorEvent operator +(GarbageCollectorEvent e, Func<IEnumerator> func)
                {
                    if (e != null && func != null)
                    {
                        e._CollectGarbageFuncs.Add(new GarbageCollectorDelegate() { FuncAsyc = func });
                    }
                    return e;
                }
                public static GarbageCollectorEvent operator -(GarbageCollectorEvent e, Func<IEnumerator> func)
                {
                    if (e != null && func != null)
                    {
                        for (int i = 0; i < e._CollectGarbageFuncs.Count; ++i)
                        {
                            if (e._CollectGarbageFuncs[i].FuncAsyc == func)
                            {
                                e._CollectGarbageFuncs.RemoveAt(i--);
                            }
                        }
                    }
                    return e;
                }
                public static GarbageCollectorEvent operator +(GarbageCollectorEvent e, Action act)
                {
                    if (e != null && act != null)
                    {
                        e._CollectGarbageFuncs.Add(new GarbageCollectorDelegate() { Func = act });
                    }
                    return e;
                }
                public static GarbageCollectorEvent operator -(GarbageCollectorEvent e, Action act)
                {
                    if (e != null && act != null)
                    {
                        for (int i = 0; i < e._CollectGarbageFuncs.Count; ++i)
                        {
                            if (e._CollectGarbageFuncs[i].Func == act)
                            {
                                e._CollectGarbageFuncs.RemoveAt(i--);
                            }
                        }
                    }
                    return e;
                }
                public void Insert(int pos, Func<IEnumerator> func)
                {
                    if (func != null)
                    {
                        if (pos < 0) pos = 0;
                        else if (pos > _CollectGarbageFuncs.Count) pos = _CollectGarbageFuncs.Count;
                        _CollectGarbageFuncs.Insert(pos, new GarbageCollectorDelegate() { FuncAsyc = func });
                    }
                }
                public void Insert(int pos, Action act)
                {
                    if (act != null)
                    {
                        if (pos < 0) pos = 0;
                        else if (pos > _CollectGarbageFuncs.Count) pos = _CollectGarbageFuncs.Count;
                        _CollectGarbageFuncs.Insert(pos, new GarbageCollectorDelegate() { Func = act });
                    }
                }
            }

            private class CallableGarbageCollectorEvent : GarbageCollectorEvent
            {
                public IEnumerator Invoke()
                {
                    for (int i = 0; i < _CollectGarbageFuncs.Count; ++i)
                    {
                        if (_CollectGarbageFuncs[i].Func != null)
                        {
                            _CollectGarbageFuncs[i].Func();
                        }
                        if (_CollectGarbageFuncs[i].FuncAsyc != null)
                        {
                            var subwork = _CollectGarbageFuncs[i].FuncAsyc();
                            if (subwork != null)
                            {
                                while (subwork.MoveNext())
                                {
                                    yield return subwork.Current;
                                }
                            }
                        }
                    }
                }
            }

            public enum GarbageCollectorLevel
            {
                CodeOnly = 0,
                CodeAndRes,
                Deep,
                _COUNT
            }
            public const int GarbageCollectorLevelCount = (int)GarbageCollectorLevel._COUNT;
            private static readonly CallableGarbageCollectorEvent[] _GarbageCollectorEvents = new CallableGarbageCollectorEvent[GarbageCollectorLevelCount];
            public class IgnoreWriteList<T> : IList<T>
            {
                protected IList<T> _Inner;

                public IgnoreWriteList(IList<T> inner)
                {
                    _Inner = inner;
                }

                public T this[int index] { get { return _Inner[index]; } set { } }

                public int Count { get { return _Inner.Count; } }

                public bool IsReadOnly { get { return true; } }

                public void Add(T item)
                {
                }

                public void Clear()
                {
                }

                public bool Contains(T item)
                {
                    return _Inner.Contains(item);
                }

                public void CopyTo(T[] array, int arrayIndex)
                {
                    _Inner.CopyTo(array, arrayIndex);
                }

                public IEnumerator<T> GetEnumerator()
                {
                    return _Inner.GetEnumerator();
                }

                public int IndexOf(T item)
                {
                    return _Inner.IndexOf(item);
                }

                public void Insert(int index, T item)
                {
                }

                public bool Remove(T item)
                {
                    return false;
                }

                public void RemoveAt(int index)
                {
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return _Inner.GetEnumerator();
                }
            }
            public static readonly IgnoreWriteList<GarbageCollectorEvent> GarbageCollectorEvents = new IgnoreWriteList<GarbageCollectorEvent>(_GarbageCollectorEvents);

            private static bool _IsGarbageCollectorRunning = false;
            private static bool _IsGarbageCollectorWorking = false;
            private static int _NextGarbageCollectTick = int.MinValue;
            private static int _LastGarbageCollectLevel = -1;
            private static int _NextGarbageCollectLevel = -1;
            public static bool IsCollectingGarbage { get { return _IsGarbageCollectorWorking; } }

            private class GarbageCollectorYieldable : CustomYieldInstruction
            {
                public override bool keepWaiting { get { return _NextGarbageCollectLevel < 0 || _NextGarbageCollectLevel <= _LastGarbageCollectLevel && System.Environment.TickCount < _NextGarbageCollectTick; } }
            }
            private static GarbageCollectorYieldable _GarbageCollectorIndicator = new GarbageCollectorYieldable();

            static GarbageCollector()
            {
                for (int i = 0; i < GarbageCollectorLevelCount; ++i)
                {
                    _GarbageCollectorEvents[i] = new CallableGarbageCollectorEvent();
                }
            }

            private static IEnumerator CollectGarbageWork()
            {
                try
                {
                    _IsGarbageCollectorRunning = true;
                    yield return _GarbageCollectorIndicator;
                    while (true)
                    {
                        int curlevel = _NextGarbageCollectLevel;
                        _NextGarbageCollectLevel = -1;
                        _LastGarbageCollectLevel = curlevel;
                        _IsGarbageCollectorWorking = true;
                        int startTick = System.Environment.TickCount;
                        Debug.LogWarning("CollectGarbageWork Begin -lvl: " + curlevel);
                        for (int j = 0; j <= curlevel; ++j)
                        {
                            for (int lvl = 0; lvl <= curlevel && lvl < GarbageCollectorLevelCount; ++lvl)
                            {
                                var gcevent = _GarbageCollectorEvents[lvl];
                                var subwork = gcevent.Invoke();
                                while (subwork.MoveNext())
                                {
                                    yield return subwork.Current;
                                    if (_NextGarbageCollectLevel > curlevel)
                                    {
                                        break;
                                    }
                                }
                                if (_NextGarbageCollectLevel > curlevel)
                                {
                                    break;
                                }
                            }
                            if (_NextGarbageCollectLevel > curlevel)
                            {
                                break;
                            }
                        }
                        if (_NextGarbageCollectLevel > curlevel)
                        {
                            continue;
                        }
                        int finishTick = System.Environment.TickCount;
                        _NextGarbageCollectTick = finishTick + 2 * (finishTick - startTick);
                        _IsGarbageCollectorWorking = false;
                        yield return _GarbageCollectorIndicator;
                    }
                }
                finally
                {
                    _IsGarbageCollectorWorking = false;
                    _IsGarbageCollectorRunning = false;
                }
            }
            public static void StartGarbageCollect(int lvl)
            {
                lvl = Math.Min(Math.Max(lvl, 0), GarbageCollectorLevelCount - 1);
                if (lvl > _NextGarbageCollectLevel)
                {
                    _NextGarbageCollectLevel = lvl;
                }
                if (!_IsGarbageCollectorRunning)
                {
                    _IsGarbageCollectorRunning = true;
                    CoroutineRunner.StartCoroutine(CollectGarbageWork());
                }
            }
            public static void DelayGarbageCollectTo(int lvl, int tick)
            {
                _LastGarbageCollectLevel = lvl;
                _NextGarbageCollectTick = tick;
            }
        }

        public static bool IsCollectingGarbage { get { return GarbageCollector.IsCollectingGarbage; } }
        public static void DelayGarbageCollectTo(int tick)
        {
            GarbageCollector.DelayGarbageCollectTo(GarbageCollector.GarbageCollectorLevelCount, tick);
        }
        public static void StartGarbageCollectLite()
        {
            GarbageCollector.StartGarbageCollect(0);
        }
        public static void StartGarbageCollectNorm()
        {
            GarbageCollector.StartGarbageCollect(1);
        }
        public static void StartGarbageCollectDeep()
        {
            GarbageCollector.StartGarbageCollect(2);
        }
    }
}