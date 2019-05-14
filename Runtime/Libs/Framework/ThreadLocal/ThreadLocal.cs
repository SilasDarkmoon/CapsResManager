using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace Capstones.UnityEngineEx
{
    public class ThreadLocalObj
    {
        private class ThreadLocalObjectContainer
        {
            public object Target;
        }
        private class ThreadInfo
        {
            public List<ThreadLocalObjectContainer> Storage = new List<ThreadLocalObjectContainer>();
        }
        [ThreadStatic] private static ThreadInfo _ThreadInfo;
        private static ThreadInfo GetThreadInfo()
        {
#if MOD_NATIVETHREADLOCAL
            if (Native.NativeThreadLocal.Ready)
            {
                var info = Native.NativeThreadLocal.GetContainer<ThreadInfo>();
                if (info == null)
                {
                    info = new ThreadInfo();
                    Native.NativeThreadLocal.SetContainer(info);
                }
                return info;
            }
#endif
            if (_ThreadInfo == null)
            {
                _ThreadInfo = new ThreadInfo();
            }
            return _ThreadInfo;
        }

        private static int _NextSlotId = 0;
        private int _SlotId = System.Threading.Interlocked.Increment(ref _NextSlotId) - 1;

        protected Func<object> _InitFunc;
        public ThreadLocalObj() { }
        public ThreadLocalObj(Func<object> initFunc)
        {
            _InitFunc = initFunc;
        }

        public object Value
        {
            get
            {
                var con = GetThreadInfo();
                var list = con.Storage;
                while (_SlotId >= list.Count)
                {
                    list.Add(null);
                }
                var ocon = list[_SlotId];
                if (ocon == null)
                {
                    ocon = new ThreadLocalObjectContainer();
                    list[_SlotId] = ocon;
                    if (_InitFunc != null)
                    {
                        ocon.Target = _InitFunc();
                    }
                }
                return ocon.Target;
            }
            set
            {
                var con = GetThreadInfo();
                var list = con.Storage;
                while (_SlotId >= list.Count)
                {
                    list.Add(null);
                }
                var ocon = list[_SlotId];
                if (ocon == null)
                {
                    ocon = new ThreadLocalObjectContainer();
                    list[_SlotId] = ocon;
                    if (_InitFunc != null)
                    {
                        ocon.Target = _InitFunc();
                    }
                }
                ocon.Target = value;
            }
        }
    }
    public class ThreadLocalObj<T> : ThreadLocalObj
    {
        public ThreadLocalObj() { }
        public ThreadLocalObj(Func<T> initFunc)
        {
            if (initFunc != null)
            {
                _InitFunc = () => initFunc();
            }
        }

        public new T Value
        {
            get
            {
                var rv = base.Value;
                if (rv is T)
                {
                    return (T)rv;
                }
                return default(T);
            }
            set
            {
                base.Value = value;
            }
        }
    }
}