using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public static class CoroutineRunner
    {
        public class CoroutineInfo
        {
            public MonoBehaviour behav;
            public IEnumerator work;
        }
        public static readonly HashSet<CoroutineInfo> RunningCoroutines = new HashSet<CoroutineInfo>();

        private static GameObject CoroutineRunnerObj;
        private static CoroutineRunnerBehav CoroutineRunnerBehav;

        public static Coroutine StartCoroutine(this IEnumerator work)
        {
            if (CoroutineRunnerObj != null && !CoroutineRunnerObj.activeInHierarchy)
            {
                Object.Destroy(CoroutineRunnerObj);
                CoroutineRunnerObj = null;
            }
            if (!CoroutineRunnerObj)
            {
                CoroutineRunnerObj = new GameObject();
                CoroutineRunnerObj.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(CoroutineRunnerObj);
            }
            if (!CoroutineRunnerBehav)
            {
                CoroutineRunnerBehav = CoroutineRunnerObj.AddComponent<CoroutineRunnerBehav>();
            }
            if (work is IDisposable)
            {
                var info = new CoroutineInfo() { behav = CoroutineRunnerBehav, work = work };
                return CoroutineRunnerBehav.StartCoroutine(SafeEnumerator(work, info));
            }
            else
            {
                return CoroutineRunnerBehav.StartCoroutine(work);
            }
        }
        public static Coroutine StartCoroutine(this IEnumerable work)
        {
            if (work == null)
            {
                return null;
            }
            return StartCoroutine(work.GetEnumerator());
        }
        public static void StopCoroutine(Coroutine c)
        {
            if (CoroutineRunnerBehav)
            {
                CoroutineRunnerBehav.StopCoroutine(c);
            }
            DisposeDeadCoroutines();
        }
        public static IEnumerator SafeEnumerator(this IEnumerator work, CoroutineInfo info)
        {
            RunningCoroutines.Add(info);
            if (work != null)
            {
                while (work.MoveNext())
                {
                    yield return work.Current;
                }
            }
            RunningCoroutines.Remove(info);
        }

        public static void DisposeDeadCoroutines()
        {
            if (RunningCoroutines.Count > 0)
            {
                RunningCoroutines.RemoveWhere(CheckDeadCoroutine);
            }
        }
        public static bool CheckDeadCoroutine(CoroutineInfo info)
        {
            if (!info.behav)
            {
                if (info.work is IDisposable)
                {
                    ((IDisposable)info.work).Dispose();
                    info.work = null;
                }
                return true;
            }
            return false;
        }
        public static void DisposeAllCoroutines(MonoBehaviour onbehav)
        {
            foreach (var info in RunningCoroutines)
            {
                if (info.work is IDisposable && info.behav == onbehav)
                {
                    ((IDisposable)info.work).Dispose();
                }
            }
            RunningCoroutines.RemoveWhere(info => info.behav == onbehav);
        }

        public static IEnumerable GetEnumerable(this IEnumerator work)
        {
            try
            {
                if (work != null)
                {
                    while (work.MoveNext())
                    {
                        yield return work.Current;
                    }
                }
            }
            finally
            {
                if (work is IDisposable)
                {
                    ((IDisposable)work).Dispose();
                }
            }
        }
        public static IEnumerator GetEmptyEnumerator()
        {
            yield break;
        }
    }

    namespace CoroutineTasks
    {
        // Work - Wrap a IEnumerator with return value and done flag.
        // Await - Wait for an started work.
        // Monitor - Starts inner work and monitor the started work on another coroutine.
        // Task - Is monitor, the inner work is changing, but the task obj itself is not changed, and can be created by concat and concurrent.

        public abstract class CoroutineWork : IEnumerator, IDisposable
        {
            protected bool _Started = false;
            protected bool _Done = false;
            protected object _Result = null;
            protected bool _Suspended = false;

            public abstract object Current { get; }
            public abstract bool MoveNext();
            public virtual void Reset() { }
            public abstract void Dispose();

            public event Action OnDone = () => { };

            protected bool TryStart()
            {
                if (!_Started)
                {
                    _Started = true;
                    Start();
                    return true;
                }
                return false;
            }
            protected virtual void Start() { }
            public bool Done {
                get { return _Done; }
                protected set
                {
                    var old = _Done;
                    _Done = value;
                    if (!old && value)
                    {
                        OnDone();
                    }
                }
            }
            public virtual object Result
            {
                get { return _Result; }
                set { _Result = value; }
            }
        }
        public class CoroutineWorkSingle : CoroutineWork
        {
            protected IEnumerator _Inner;

            public override object Current
            {
                get
                {
                    if (_Suspended || _Inner == null)
                    {
                        return null;
                    }
                    else
                    {
                        return _Inner.Current;
                    }
                }
            }
            public override bool MoveNext()
            {
                if (Done)
                {
                    return false;
                }
                if (_Inner == null)
                {
                    Done = true;
                    return false;
                }
                if (_Suspended)
                {
                    return true;
                }
                if (_Inner.MoveNext())
                {
                    return true;
                }
                else
                {
                    Done = true;
                    return false;
                }
            }
            public override void Dispose()
            {
                var dis = _Inner as IDisposable;
                if (dis != null)
                {
                    dis.Dispose();
                }
            }

            public void SetWork(IEnumerator work)
            {
                _Inner = work;
            }
        }
        public class CoroutineWorkQueue : CoroutineWork
        {
            protected readonly List<CoroutineWork> _Works = new List<CoroutineWork>();
            protected int _CurWorkIndex = 0;
            protected CoroutineWork Work
            {
                get
                {
                    if (_Works.Count > _CurWorkIndex)
                    {
                        return _Works[_CurWorkIndex];
                    }
                    return null;
                }
            }

            public override object Current
            {
                get
                {
                    if (_Suspended || Done)
                    {
                        return null;
                    }
                    var work = Work;
                    if (work == null)
                    {
                        return null;
                    }
                    else
                    {
                        return work.Current;
                    }
                }
            }
            public override bool MoveNext()
            {
                if (Done)
                {
                    return false;
                }
                var work = Work;
                if (work == null)
                {
                    Done = true;
                    return false;
                }
                if (_Suspended)
                {
                    return true;
                }
                TryStart();
                while (true)
                {
                    if (work.MoveNext())
                    {
                        return true;
                    }
                    else
                    {
                        var partResult = work.Result;
                        ++_CurWorkIndex;
                        work = Work;
                        if (work == null)
                        {
                            _Result = partResult;
                            break;
                        }
                        else
                        {
                            work.Result = partResult;
                        }
                    }
                }
                Done = true;
                return false;
            }
            public override void Dispose()
            {
                for (int i = 0; i < _Works.Count; ++i)
                {
                    _Works[i].Dispose();
                }
            }
            protected override void Start()
            {
                if (_Result != null)
                {
                    var work = Work;
                    if (work != null)
                    {
                        work.Result = _Result;
                    }
                }
            }

            public void AddWork(CoroutineWork work)
            {
                if (work != null)
                {
                    _Works.Add(work);
                }
            }
            public CoroutineWork FirstWork
            {
                get
                {
                    if (_Works.Count > 0)
                    {
                        return _Works[0];
                    }
                    return null;
                }
            }
            public CoroutineWork LastWork
            {
                get
                {
                    if (_Works.Count > 0)
                    {
                        return _Works[_Works.Count - 1];
                    }
                    return null;
                }
            }
        }
        public class CoroutineAwait : CoroutineWork
        {
            protected CoroutineWork _Inner;

            public override object Current
            {
                get
                {
                    return null;
                }
            }
            public override bool MoveNext()
            {
                if (Done)
                {
                    return false;
                }
                if (_Inner == null)
                {
                    Done = true;
                    return false;
                }
                if (_Suspended)
                {
                    return true;
                }
                if (_Inner.Done)
                {
                    Done = true;
                    _Result = _Inner.Result;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public override void Dispose()
            {
            }

            public void SetWork(CoroutineWork work)
            {
                _Inner = work;
            }
        }

        public abstract class CoroutineMonitor : CoroutineWork
        {
            public override object Current
            {
                get
                {
                    return null;
                }
            }

        }
        public class CoroutineMonitorSingle : CoroutineMonitor
        {
            protected CoroutineWork _Inner;

            public override bool MoveNext()
            {
                TryStart();
                if (Done)
                {
                    return false;
                }
                if (_Inner == null)
                {
                    Done = true;
                    return false;
                }
                if (_Suspended)
                {
                    return true;
                }
                if (_Inner.Done)
                {
                    Done = true;
                    _Result = _Inner.Result;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public override void Dispose()
            {
                if (_Inner != null)
                {
                    _Inner.Dispose();
                }
            }
            protected override void Start()
            {
                if (_Inner != null)
                {
                    if (_Result != null)
                    {
                        _Inner.Result = _Result;
                    }
                    _Inner.StartCoroutine();
                }
            }

            public void SetWork(CoroutineWork work)
            {
                if (!_Started)
                {
                    _Inner = work;
                }
            }
        }
        public class CoroutineMonitorConcurrent : CoroutineMonitor
        {
            protected readonly List<CoroutineWork> _Works = new List<CoroutineWork>();

            public override bool MoveNext()
            {
                TryStart();
                if (Done)
                {
                    return false;
                }
                if (_Works.Count == 0)
                {
                    Done = true;
                    return false;
                }
                if (_Suspended)
                {
                    return true;
                }
                for (int i = 0; i < _Works.Count; ++i)
                {
                    if (!_Works[i].Done)
                    {
                        return true;
                    }
                }
                Done = true;
                return false;
            }
            public override void Dispose()
            {
                for (int i = 0; i < _Works.Count; ++i)
                {
                    _Works[i].Dispose();
                }
            }
            protected override void Start()
            {
                for (int i = 0; i < _Works.Count; ++i)
                {
                    _Works[i].StartCoroutine();
                }
            }
            public override object Result
            {
                get
                {
                    if (_Result == null)
                    {
                        var result = new object[_Works.Count];
                        _Result = result;
                        for (int i = 0; i < _Works.Count; ++i)
                        {
                            result[i] = _Works[i].Result;
                        }
                    }
                    return _Result;
                }
                set
                {
                    for (int i = 0; i < _Works.Count; ++i)
                    {
                        _Works[i].Result = value;
                    }
                }
            }

            public void AddWork(CoroutineWork work)
            {
                if (!_Started && work != null)
                {
                    _Works.Add(work);
                }
            }
        }

        public class CoroutineTask : CoroutineMonitorSingle
        {
            protected CoroutineWork GetRealWorkFromSubWork(IEnumerator work)
            {
                var realwork = work as CoroutineWork;
                if (realwork == null)
                {
                    var worksingle = new CoroutineWorkSingle();
                    worksingle.SetWork(work);
                    realwork = worksingle;
                }
                else
                {
                    var rtask = work as CoroutineTask;
                    if (rtask != null)
                    {
                        realwork = rtask._Inner;
                        var rawait = new CoroutineAwait();
                        rawait.SetWork(realwork);
                        rtask.SetWork(rawait);
                    }
                }
                return realwork;
            }
            protected CoroutineWorkQueue MakeInnerQueue()
            {
                var queue = _Inner as CoroutineWorkQueue;
                if (queue == null)
                {
                    queue = new CoroutineWorkQueue();
                    queue.AddWork(_Inner);
                    _Inner = queue;
                }
                return queue;
            }
            protected CoroutineMonitorConcurrent MakeInnerConcurrent()
            {
                var con = _Inner as CoroutineMonitorConcurrent;
                if (con == null)
                {
                    con = new CoroutineMonitorConcurrent();
                    con.AddWork(_Inner);
                    _Inner = con;
                }
                return con;
            }

            public void Concat(IEnumerator work)
            {
                if (work == null)
                {
                    return;
                }
                var realwork = GetRealWorkFromSubWork(work);

                if (_Inner == null)
                {
                    _Inner = realwork;
                }
                else
                {
                    var queue = MakeInnerQueue();
                    queue.AddWork(realwork);
                }
            }
            public void Concurrent(IEnumerator work)
            {
                if (work == null)
                {
                    return;
                }
                var realwork = GetRealWorkFromSubWork(work);

                if (_Inner == null)
                {
                    _Inner = realwork;
                }
                else
                {
                    var queue = MakeInnerConcurrent();
                    queue.AddWork(realwork);
                }
            }
            public void ConcurrentLast(IEnumerator work)
            {
                if (work == null)
                {
                    return;
                }
                var realwork = GetRealWorkFromSubWork(work);

                if (_Inner == null)
                {
                    _Inner = realwork;
                }
                else
                {
                    var queue = _Inner as CoroutineWorkQueue;
                    if (queue == null)
                    {
                        var queuec = MakeInnerConcurrent();
                        queuec.AddWork(realwork);
                    }
                    else
                    {
                        var last = queue.LastWork;
                        if (last == null)
                        {
                            queue.AddWork(realwork);
                        }
                        else
                        {
                            var queuec = last as CoroutineMonitorConcurrent;
                            if (queuec != null)
                            {
                                queuec.AddWork(realwork);
                            }
                            else
                            {
                                queuec = new CoroutineMonitorConcurrent();
                                queuec.AddWork(last);
                                queuec.AddWork(realwork);
                            }
                        }
                    }
                }
            }
        }
    }

    public class WaitForTickCount : CustomYieldInstruction
    {
        private int _ToTick;

        public WaitForTickCount(int delta)
        {
            SetDelta(delta);
        }
        public void SetDelta(int delta)
        {
            _ToTick = Environment.TickCount + delta;
        }

        public override bool keepWaiting
        {
            get
            {
                return _ToTick - Environment.TickCount > 0;
            }
        }
    }
}