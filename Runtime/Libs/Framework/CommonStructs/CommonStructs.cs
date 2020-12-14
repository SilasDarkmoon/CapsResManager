using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using uobj = UnityEngine.Object;
#endif

namespace Capstones.UnityEngineEx
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct BOOL
    {
        public BOOL(bool v)
        {
            val = v ? 1 : 0;
        }
        public BOOL(int v)
        {
            val = v;
        }

        public int val;
        public static implicit operator bool(BOOL v)
        {
            return v.val != 0;
        }
        public static implicit operator BOOL(bool v)
        {
            var v2 = new BOOL();
            v2.val = v ? 1 : 0;
            return v2;
        }
        public static bool operator ==(BOOL v1, BOOL v2)
        {
            return (v1.val == 0) == (v2.val == 0);
        }
        public static bool operator !=(BOOL v1, BOOL v2)
        {
            return (v1.val == 0) != (v2.val == 0);
        }
        public static bool operator ==(BOOL v1, bool v2)
        {
            return (v1.val != 0) == v2;
        }
        public static bool operator !=(BOOL v1, bool v2)
        {
            return (v1.val != 0) != v2;
        }
        public static bool operator ==(bool v1, BOOL v2)
        {
            return v1 == (v2.val != 0);
        }
        public static bool operator !=(bool v1, BOOL v2)
        {
            return v1 != (v2.val != 0);
        }

        public override bool Equals(object obj)
        {
            bool v1 = (val != 0);
            if (obj is bool)
            {
                return v1 == (bool)obj;
            }
            else if (obj is BOOL)
            {
                return v1 == (((BOOL)obj).val != 0);
            }
            return false;
        }
        public override int GetHashCode()
        {
            bool v1 = (val != 0);
            return v1.GetHashCode();
        }
        public override string ToString()
        {
            bool v1 = (val != 0);
            return v1.ToString();
        }
    }

    public struct Pack<T1, T2>
    {
        public T1 t1;
        public T2 t2;

        public Pack(T1 p1, T2 p2)
        {
            t1 = p1;
            t2 = p2;
        }
    }
    public struct Pack<T1, T2, T3>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;

        public Pack(T1 p1, T2 p2, T3 p3)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
        }
    }
    public struct Pack<T1, T2, T3, T4>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
        }
    }
    public struct Pack<T1, T2, T3, T4, T5>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6, T7>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;
        public T7 t7;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
            t7 = p7;
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;
        public T7 t7;
        public T8 t8;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
            t7 = p7;
            t8 = p8;
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;
        public T7 t7;
        public T8 t8;
        public T9 t9;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8, T9 p9)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
            t7 = p7;
            t8 = p8;
            t9 = p9;
        }
    }
    public struct Pack<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        public T1 t1;
        public T2 t2;
        public T3 t3;
        public T4 t4;
        public T5 t5;
        public T6 t6;
        public T7 t7;
        public T8 t8;
        public T9 t9;
        public T10 t10;

        public Pack(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6, T7 p7, T8 p8, T9 p9, T10 p10)
        {
            t1 = p1;
            t2 = p2;
            t3 = p3;
            t4 = p4;
            t5 = p5;
            t6 = p6;
            t7 = p7;
            t8 = p8;
            t9 = p9;
            t10 = p10;
        }
    }

    public struct ValueList<T> : IList<T>
    {
        private T t0;
        private T t1;
        private T t2;
        private T t3;
        private T t4;
        private T t5;
        private T t6;
        private T t7;
        private T t8;
        private T t9;
        private List<T> tx;

        private int _cnt;

#region static funcs for set and get
        private delegate T GetTDel(ref ValueList<T> list);
        private delegate void SetTDel(ref ValueList<T> list, T val);

        private static T GetT0(ref ValueList<T> list) { return list.t0; }
        private static T GetT1(ref ValueList<T> list) { return list.t1; }
        private static T GetT2(ref ValueList<T> list) { return list.t2; }
        private static T GetT3(ref ValueList<T> list) { return list.t3; }
        private static T GetT4(ref ValueList<T> list) { return list.t4; }
        private static T GetT5(ref ValueList<T> list) { return list.t5; }
        private static T GetT6(ref ValueList<T> list) { return list.t6; }
        private static T GetT7(ref ValueList<T> list) { return list.t7; }
        private static T GetT8(ref ValueList<T> list) { return list.t8; }
        private static T GetT9(ref ValueList<T> list) { return list.t9; }

        private static void SetT0(ref ValueList<T> list, T val) { list.t0 = val; }
        private static void SetT1(ref ValueList<T> list, T val) { list.t1 = val; }
        private static void SetT2(ref ValueList<T> list, T val) { list.t2 = val; }
        private static void SetT3(ref ValueList<T> list, T val) { list.t3 = val; }
        private static void SetT4(ref ValueList<T> list, T val) { list.t4 = val; }
        private static void SetT5(ref ValueList<T> list, T val) { list.t5 = val; }
        private static void SetT6(ref ValueList<T> list, T val) { list.t6 = val; }
        private static void SetT7(ref ValueList<T> list, T val) { list.t7 = val; }
        private static void SetT8(ref ValueList<T> list, T val) { list.t8 = val; }
        private static void SetT9(ref ValueList<T> list, T val) { list.t9 = val; }

        private static GetTDel[] GetTFuncs = new GetTDel[]
        {
            GetT0,
            GetT1,
            GetT2,
            GetT3,
            GetT4,
            GetT5,
            GetT6,
            GetT7,
            GetT8,
            GetT9,
        };
        private static SetTDel[] SetTFuncs = new SetTDel[]
        {
            SetT0,
            SetT1,
            SetT2,
            SetT3,
            SetT4,
            SetT5,
            SetT6,
            SetT7,
            SetT8,
            SetT9,
        };
#endregion

#region IList<T>
        public int IndexOf(T item)
        {
            for (int i = 0; i < _cnt; ++i)
            {
                if (object.Equals(this[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index >= 0 && index <= _cnt)
            {
                this.Add(default(T));
                for (int i = _cnt - 1; i > index; --i)
                {
                    this[i] = this[i - 1];
                }
                this[index] = item;
            }
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _cnt)
            {
                for (int i = index + 1; i < _cnt; ++i)
                {
                    this[i - 1] = this[i];
                }
                this[_cnt - 1] = default(T);
                --_cnt;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index >= 0 && index < _cnt)
                {
                    if (index < GetTFuncs.Length)
                    {
                        return GetTFuncs[index](ref this);
                    }
                    else
                    {
                        if (tx != null)
                        {
                            var pindex = index - GetTFuncs.Length;
                            if (pindex < tx.Count)
                            {
                                return tx[pindex];
                            }
                        }
                    }
                }
                return default(T);
            }
            set
            {
                if (index >= 0 && index < _cnt)
                {
                    if (index < SetTFuncs.Length)
                    {
                        SetTFuncs[index](ref this, value);
                    }
                    else
                    {
                        if (tx != null)
                        {
                            var pindex = index - SetTFuncs.Length;
                            if (pindex < tx.Count)
                            {
                                tx[pindex] = value;
                            }
                        }
                    }
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (_cnt < SetTFuncs.Length)
            {
                this[_cnt++] = item;
            }
            else
            {
                ++_cnt;
                if (tx == null)
                {
                    tx = new List<T>(8);
                }
                tx.Add(item);
            }
        }

        public void Clear()
        {
            _cnt = 0;
            t0 = default(T);
            t1 = default(T);
            t2 = default(T);
            t3 = default(T);
            t4 = default(T);
            t5 = default(T);
            t6 = default(T);
            t7 = default(T);
            t8 = default(T);
            t9 = default(T);
            tx = null;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex >= 0)
            {
                for (int i = 0; i < _cnt && i + arrayIndex < array.Length; ++i)
                {
                    array[arrayIndex + i] = this[i];
                }
            }
        }

        public int Count
        {
            get { return _cnt; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0 && index < _cnt)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _cnt; ++i)
            {
                yield return this[i];
            }
        }
#endregion

        public T[] ToArray()
        {
            T[] arr = new T[_cnt];
            CopyTo(arr, 0);
            return arr;
        }

        public override bool Equals(object obj)
        {
            if (obj is ValueList<T>)
            {
                ValueList<T> types2 = (ValueList<T>)obj;
                if (types2._cnt == _cnt)
                {
                    for (int i = 0; i < _cnt; ++i)
                    {
                        if (!object.Equals(this[i], types2[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        internal static bool OpEquals(ValueList<T> source, ValueList<T> other)
        {
            if (!object.ReferenceEquals(source, null))
            {
                return source.Equals(other);
            }
            else if (!object.ReferenceEquals(other, null))
            {
                return other.Equals(source);
            }
            return true;
        }
        public static bool operator ==(ValueList<T> source, ValueList<T> other)
        {
            return OpEquals(source, other);
        }
        public static bool operator !=(ValueList<T> source, ValueList<T> other)
        {
            return !OpEquals(source, other);
        }

        public override int GetHashCode()
        {
            int code = 0;
            for (int i = 0; i < Count; ++i)
            {
                code <<= 1;
                var type = this[i];
                if (type != null)
                {
                    code += type.GetHashCode();
                }
            }
            return code;
        }
    }

    public static class EnumUtils
    { // TODO: use ByRefUtils.dll to do the convert quickly
        public static T ConvertToEnum<T>(ulong val) where T : struct
        {
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && (!NET_4_6 && !NET_STANDARD_2_0 || !NET_EX_LIB_UNSAFE)
            return (T)Enum.ToObject(typeof(T), val);
#else
            Span<ulong> span = stackalloc[] { val };
            var tspan = System.Runtime.InteropServices.MemoryMarshal.Cast<ulong, T>(span);

            if (BitConverter.IsLittleEndian)
            {
                return tspan[0];
            }
            else
            {
                return tspan[8 / System.Runtime.InteropServices.Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T))) - 1];
            }
#endif
        }
        public static ulong ConvertFromEnum<T>(T val) where T : struct
        {
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && (!NET_4_6 && !NET_STANDARD_2_0 || !NET_EX_LIB_UNSAFE)
            return Convert.ToUInt64(val);
#else
            Span<ulong> span = stackalloc ulong[1];
            var tspan = System.Runtime.InteropServices.MemoryMarshal.Cast<ulong, T>(span);

            if (BitConverter.IsLittleEndian)
            {
                tspan[0] = val;
            }
            else
            {
                tspan[8 / System.Runtime.InteropServices.Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T))) - 1] = val;
            }
            return span[0];
#endif
        }
    }

    public interface IConvertibleDictionary
    {
        T Get<T>(string key);
        void Set<T>(string key, T val); 
    }

    public static class ConvertUtils
    {
        public static T As<T>(this object val)
        {
            return val is T ? (T)val : default(T);
        }

        private static HashSet<Type> NumericTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(decimal),
            typeof(double),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(float),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
        };
        private static HashSet<Type> ConvertibleTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(decimal),
            typeof(double),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(float),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),

            typeof(char),
            typeof(string),
            typeof(IntPtr),
        };

        public class TypedConverter
        {
            protected Type _ToType;
            public Type ToType
            {
                get { return _ToType; }
            }

            public Func<object, object> ConvertFunc;
            public object Convert(object obj)
            {
                var func = ConvertFunc;
                if (func != null)
                {
                    return func(obj);
                }
                return null;
            }
        }
        public class TypedConverter<T> : TypedConverter
        { // TODO: unmanaged value type converter using ByRefUtils
            public TypedConverter()
            {
                _ToType = typeof(T);
            }
            public TypedConverter(Func<object, T> convertFunc)
                : this()
            {
                ConvertFunc = convertFunc;
            }

            public new Func<object, T> ConvertFunc;
            public new T Convert(object obj)
            {
                var func = ConvertFunc;
                if (func != null)
                {
                    return func(obj);
                }
                return default(T);
            }
        }
        public static readonly Dictionary<Type, TypedConverter> _TypedConverters = new Dictionary<Type, TypedConverter>()
        {
            { typeof(bool), new TypedConverter<bool>(
                obj =>
                {
                    if (obj == null)
                    {
                        return false;
                    }
                    if (obj is bool)
                    {
                        return (bool)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        str = str.ToLower().Trim();
                        if (str == "" || str == "n" || str == "no" || str == "f" || str == "false")
                        {
                            return false;
                        }
                        return true;
                    }
                    else if (obj is IntPtr)
                    {
                        return ((IntPtr)obj) != IntPtr.Zero;
                    }
                    else if (obj is UIntPtr)
                    {
                        return ((UIntPtr)obj) != UIntPtr.Zero;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToBoolean(obj);
                        }
                        catch { }
                    }
                    return true;
                })
            },
            { typeof(string), new TypedConverter<string>(
                obj =>
                {
                    if (obj == null)
                    {
                        return null;
                    }
                    if (obj is string)
                    {
                        return (string)obj;
                    }
                    return obj.ToString();
                })
            },
            { typeof(byte), new TypedConverter<byte>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is byte)
                    {
                        return (byte)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        byte rv;
                        byte.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (byte)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (byte)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToByte(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(sbyte), new TypedConverter<sbyte>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is sbyte)
                    {
                        return (sbyte)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        sbyte rv;
                        sbyte.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (sbyte)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (sbyte)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToSByte(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(short), new TypedConverter<short>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is short)
                    {
                        return (short)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        short rv;
                        short.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (short)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (short)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToInt16(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(ushort), new TypedConverter<ushort>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is ushort)
                    {
                        return (ushort)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        ushort rv;
                        ushort.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (ushort)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (ushort)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToUInt16(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(int), new TypedConverter<int>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is int)
                    {
                        return (int)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        int rv;
                        int.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (int)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (int)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToInt32(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(uint), new TypedConverter<uint>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is uint)
                    {
                        return (uint)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        uint rv;
                        uint.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (uint)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (uint)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToUInt32(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(long), new TypedConverter<long>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is long)
                    {
                        return (long)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        long rv;
                        long.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (long)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (long)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToInt64(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(ulong), new TypedConverter<ulong>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is ulong)
                    {
                        return (ulong)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        ulong rv;
                        ulong.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (ulong)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (ulong)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToUInt64(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(char), new TypedConverter<char>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(char);
                    }
                    if (obj is char)
                    {
                        return (char)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        char rv;
                        char.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (char)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (char)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToChar(obj);
                        }
                        catch { }
                    }
                    return default(char);
                })
            },
            { typeof(IntPtr), new TypedConverter<IntPtr>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(IntPtr);
                    }
                    if (obj is IntPtr)
                    {
                        return (IntPtr)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        ulong rv;
                        ulong.TryParse(str, out rv);
                        return (IntPtr)rv;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (IntPtr)(ulong)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return (IntPtr)System.Convert.ToUInt64(obj);
                        }
                        catch { }
                    }
                    return default(IntPtr);
                })
            },
            { typeof(UIntPtr), new TypedConverter<UIntPtr>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(UIntPtr);
                    }
                    if (obj is UIntPtr)
                    {
                        return (UIntPtr)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        ulong rv;
                        ulong.TryParse(str, out rv);
                        return (UIntPtr)rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (UIntPtr)(ulong)(IntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return (UIntPtr)System.Convert.ToUInt64(obj);
                        }
                        catch { }
                    }
                    return default(UIntPtr);
                })
            },
            { typeof(float), new TypedConverter<float>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is float)
                    {
                        return (float)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        float rv;
                        float.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (float)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (float)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToSingle(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(double), new TypedConverter<double>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is double)
                    {
                        return (double)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        double rv;
                        double.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (double)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (double)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToDouble(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(decimal), new TypedConverter<decimal>(
                obj =>
                {
                    if (obj == null)
                    {
                        return 0;
                    }
                    if (obj is decimal)
                    {
                        return (decimal)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        decimal rv;
                        decimal.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return (decimal)(IntPtr)obj;
                    }
                    else if (obj is UIntPtr)
                    {
                        return (decimal)(UIntPtr)obj;
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToDecimal(obj);
                        }
                        catch { }
                    }
                    return 0;
                })
            },
            { typeof(TimeSpan), new TypedConverter<TimeSpan>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(TimeSpan);
                    }
                    if (obj is TimeSpan)
                    {
                        return (TimeSpan)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        TimeSpan rv;
                        TimeSpan.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return new TimeSpan((long)(IntPtr)obj);
                    }
                    else if (obj is UIntPtr)
                    {
                        return new TimeSpan((long)(UIntPtr)obj);
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return new TimeSpan(System.Convert.ToInt64(obj));
                        }
                        catch { }
                    }
                    return default(TimeSpan);
                })
            },
            { typeof(DateTime), new TypedConverter<DateTime>(
                obj =>
                {
                    if (obj == null)
                    {
                        return default(DateTime);
                    }
                    if (obj is DateTime)
                    {
                        return (DateTime)obj;
                    }
                    if (obj is string)
                    {
                        var str = (string)obj;
                        DateTime rv;
                        DateTime.TryParse(str, out rv);
                        return rv;
                    }
                    else if (obj is IntPtr)
                    {
                        return new DateTime((long)(IntPtr)obj);
                    }
                    else if (obj is UIntPtr)
                    {
                        return new DateTime((long)(UIntPtr)obj);
                    }
                    if (PlatDependant.IsObjIConvertible(obj))
                    {
                        try
                        {
                            return System.Convert.ToDateTime(obj);
                        }
                        catch { }
                    }
                    return default(DateTime);
                })
            },
        };
        public static T Convert<T>(this object obj)
        {
            TypedConverter converter;
            if (_TypedConverters.TryGetValue(typeof(T), out converter))
            {
                TypedConverter<T> tconverter = converter as TypedConverter<T>;
                if (tconverter != null)
                {
                    return tconverter.Convert(obj);
                }
            }
            if (obj == null)
                return default(T);
            if (obj is T)
                return (T)obj;
            var type = typeof(T);
            //if (typeof(T) == typeof(bool))
            //{
            //    if (obj is string)
            //    {
            //        var str = (string)obj;
            //        str = str.ToLower().Trim();
            //        if (str == "" || str == "n" || str == "no" || str == "f" || str == "false")
            //        {
            //            return (T)(object)false;
            //        }
            //        return (T)(object)true;
            //    }
            //}
            if (type.IsEnum())
            {
                if (obj is string)
                {
                    return (T)Enum.Parse(type, obj as string);
                }
                else if (NumericTypes.Contains(obj.GetType()))
                {
                    return (T)Enum.ToObject(type, (object)System.Convert.ToUInt64(obj));
                }
                else if (obj is Enum)
                {
                    return (T)System.Convert.ChangeType(System.Convert.ToUInt64(obj), type);
                }
                else
                {
                    return default(T);
                }
            }
            //else if (obj is Enum)
            //{
            //    if (type == typeof(string))
            //    {
            //        return (T)(object)obj.ToString();
            //    }
            //    else if (NumericTypes.Contains(type))
            //    {
            //        return (T)System.Convert.ChangeType(System.Convert.ToUInt64(obj), type);
            //    }
            //    else
            //    {
            //        return default(T);
            //    }
            //}
            //else if (NumericTypes.Contains(type) && NumericTypes.Contains(obj.GetType()))
            //{
            //    try
            //    {
            //        return (T)System.Convert.ChangeType(obj, type);
            //    }
            //    catch
            //    {
            //        return default(T);
            //    }
            //}
            //else if (type == typeof(IntPtr) && NumericTypes.Contains(obj.GetType()))
            //{
            //    try
            //    {
            //        long l = System.Convert.ToInt64(obj);
            //        IntPtr p = (IntPtr)l;
            //        return (T)(object)p;
            //    }
            //    catch
            //    {
            //        return default(T);
            //    }
            //}
            //else if (obj is IntPtr && NumericTypes.Contains(type))
            //{
            //    IntPtr p = (IntPtr)obj;
            //    long l = (long)p;
            //    try
            //    {
            //        return (T)System.Convert.ChangeType(l, type);
            //    }
            //    catch
            //    {
            //        return default(T);
            //    }
            //}
            //else if (ConvertibleTypes.Contains(type) && ConvertibleTypes.Contains(obj.GetType()))
            //{
            //    try
            //    {
            //        return (T)System.Convert.ChangeType(obj, type);
            //    }
            //    catch
            //    {
            //        return default(T);
            //    }
            //}
            //else if (typeof(T) == typeof(string))
            //{
            //    return (T)(object)obj.ToString();
            //}
            return default(T);
        }
    }
}
