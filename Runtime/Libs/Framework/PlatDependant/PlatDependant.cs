namespace Capstones.UnityEngineEx
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;

#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && !NET_4_6 && !NET_STANDARD_2_0
    using Unity.IO.Compression;
#else
    using System.IO.Compression;
#endif

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class PlatDependant
    {
        #region Logger
        public static bool LogEnabled = true;
        public static bool LogInfoEnabled = true;
        public static bool LogWarningEnabled = true;
        public static bool LogErrorEnabled = true;
        public static bool _LogToConsoleEnabled = true;
        public static bool LogToConsoleEnabled
        {
            get { return _LogToConsoleEnabled; }
            set { _LogToConsoleEnabled = value; }
        }
        public static volatile bool LogToFileEnabled = true;
        [ThreadStatic] public static bool LogCSharpStackTraceEnabled = true;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        [ThreadStatic] public static bool DisableLogTemp;
#endif
        public static string LogFilePath;
        public static event Action<string> OnExLogger;
        public static Func<string, string> OnExStackTrace;

        private static class Logger
        {
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && !NET_4_6 && !NET_STANDARD_2_0
            private static Unity.Collections.Concurrent.ConcurrentQueue<StringBuilder> LogQueue = new Unity.Collections.Concurrent.ConcurrentQueue<StringBuilder>();
            private static Unity.Collections.Concurrent.ConcurrentQueue<StringBuilder> LogPool = new Unity.Collections.Concurrent.ConcurrentQueue<StringBuilder>();
#else
            private static System.Collections.Concurrent.ConcurrentQueue<StringBuilder> LogQueue = new System.Collections.Concurrent.ConcurrentQueue<StringBuilder>();
            private static System.Collections.Concurrent.ConcurrentQueue<StringBuilder> LogPool = new System.Collections.Concurrent.ConcurrentQueue<StringBuilder>();
#endif
            private static System.Threading.AutoResetEvent LogNotify = new System.Threading.AutoResetEvent(false);
            private static System.Threading.AutoResetEvent LogFileDoneNotify = new System.Threading.AutoResetEvent(true);

            private static string GetStackTrace()
            {
                var stack = Environment.StackTrace;
                var ex = OnExStackTrace;
                if (ex != null)
                {
                    var exstack = ex(stack);
                    if (!string.IsNullOrEmpty(exstack))
                    {
                        if (string.IsNullOrEmpty(stack))
                        {
                            stack = exstack;
                        }
                        else
                        {
                            stack = stack + "\n" + exstack;
                        }
                    }
                }
                return stack;
            }
            [ThreadStatic] private static bool _ForbidStackTrace;

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            public struct LogMessage
            {
                public DateTime Time;
                public string Message;
                public string StackTrace;
                public UnityEngine.LogType LogType;
            }
            public static readonly LogMessage[] LogMessages = new LogMessage[32];
            private static int _LogMessageIndex = 0;
            public static int LogMessageIndex { get { return _LogMessageIndex; } }
            public static string[] CopyLogMessages()
            {
                string[] rv = new string[LogMessages.Length];
                var start = _LogMessageIndex;
                for (int i = 0; i < LogMessages.Length; ++i)
                {
                    var index = (start + i) % LogMessages.Length;
                    var log = LogMessages[index];
                    var message = string.Format("{0:HH\\:mm\\:ss.ff} ", log.Time);
                    message += log.LogType;
                    message += Environment.NewLine;
                    message += log.Message;
                    message += Environment.NewLine;
                    message += log.StackTrace;
                    rv[i] = message;
                }
                return rv;
            }
#endif

            static Logger()
            {
#if DISABLE_LOG_ALL
                LogEnabled = false;
#endif
#if DISABLE_LOG_INFO
                LogInfoEnabled = false;
#endif
#if DISABLE_LOG_WARN
                LogWarningEnabled = false;
#endif
#if DISABLE_LOG_ERROR
                LogErrorEnabled = false;
#endif
#if DISABLE_LOG_CONSOLE
                LogToConsoleEnabled = false;
#endif
#if DISABLE_LOG_STACKTRACE
                LogCSharpStackTraceEnabled = false;
#endif
#if !(DEVELOPMENT_BUILD || UNITY_EDITOR || ALWAYS_SHOW_LOG || DEBUG)
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                LogToConsoleEnabled = false;
#endif
#endif

                string logdir = Capstones.UnityEngineEx.ThreadSafeValues.IsolatedPath;
#if UNITY_IOS && !UNITY_EDITOR && (LOG_TO_DOCUMENT_FOLDER || DEVELOPMENT_BUILD || ALWAYS_SHOW_LOG || DEBUG)
                logdir = Capstones.UnityEngineEx.ThreadSafeValues.AppPersistentDataPath;
#endif
                var file = logdir + "/log/cs/log" + DateTime.Now.ToString("MMdd") + ".txt";
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                if (!IsFileExist(file))
                {
                    foreach (var ofile in GetAllFiles(logdir + "/log/cs/"))
                    {
                        DeleteFile(ofile);
                    }
                }
#endif
                LogFilePath = file;

                RunBackgroundLongTime(prog =>
                {
#if UNITY_EDITOR
                    try
                    {
#endif
                        while (LogNotify.WaitOne())
                        {
                            WaitForLogFileDone();
                            try
                            {
                                using (var sw = OpenAppendText(LogFilePath))
                                {
                                    if (sw != null)
                                    {
                                        StringBuilder sb;
                                        while (LogQueue.TryDequeue(out sb))
                                        {
                                            var str = sb.ToString();
                                            ReturnStringBuilder(sb);

                                            try
                                            {
                                                sw.WriteLine(str);
                                            }
                                            catch { }
                                            var exlogger = OnExLogger;
                                            if (exlogger != null)
                                            {
                                                exlogger(str);
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                            SetLogFileDone();
                        }
#if UNITY_EDITOR
                    }
                    catch (System.Threading.ThreadAbortException) { }
#endif
                });
            }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            public static void OnUnityLogReceived(string condition, string stackTrace, UnityEngine.LogType type)
            {
                if (LogEnabled && !DisableLogTemp)
                {
                    if (type == UnityEngine.LogType.Log && LogInfoEnabled || type == UnityEngine.LogType.Warning && LogWarningEnabled || type != UnityEngine.LogType.Log && type != UnityEngine.LogType.Warning && LogErrorEnabled)
                    {
                        if (LogCSharpStackTraceEnabled && !_ForbidStackTrace)
                        {
                            string exstack = null;
                            if (ThreadSafeValues.IsMainThread || !string.IsNullOrEmpty(stackTrace))
                            {
                                var ex = OnExStackTrace;
                                if (ex != null)
                                {
                                    exstack = ex(stackTrace);
                                }
                            }
                            else
                            {
                                exstack = GetStackTrace();
                            }
                            if (!string.IsNullOrEmpty(exstack))
                            {
                                DisableLogTemp = true;
                                if (type == UnityEngine.LogType.Log)
                                {
                                    UnityEngine.Debug.Log(condition + "\nex stack trace:\n" + exstack);
                                }
                                else if (type == UnityEngine.LogType.Warning)
                                {
                                    UnityEngine.Debug.LogWarning(condition + "\nex stack trace:\n" + exstack);
                                }
                                else
                                {
                                    UnityEngine.Debug.LogError(condition + "\nex stack trace:\n" + exstack);
                                }
                                DisableLogTemp = false;

                                if (string.IsNullOrEmpty(stackTrace))
                                {
                                    stackTrace = exstack;
                                }
                                else
                                {
                                    stackTrace = stackTrace + "\n" + exstack;
                                }
                            }
                        }

                        var index = (System.Threading.Interlocked.Increment(ref _LogMessageIndex) - 1) % LogMessages.Length;
                        var message = new LogMessage() { Message = condition, StackTrace = stackTrace, LogType = type, Time = DateTime.Now };
                        LogMessages[index] = message;

                        if (LogToFileEnabled)
                        {
                            var sb = GetStringBuilder();
                            sb.AppendFormat("{0:HH\\:mm\\:ss.ff}", message.Time);
                            switch (type)
                            {
                                case UnityEngine.LogType.Log:
                                    sb.AppendLine(" I");
                                    break;
                                case UnityEngine.LogType.Warning:
                                    sb.AppendLine(" W");
                                    break;
                                default:
                                    sb.AppendLine(" E");
                                    break;
                            }
                            sb.AppendLine(condition);
                            sb.AppendLine(stackTrace);
                            EnqueueLog(sb);
                        }
                    }
                }
            }
#endif

            public static string SendLogBegin()
            {
                LogToFileEnabled = false;
                WaitForLogFileDone();
                return LogFilePath;
            }
            public static void SendLogEnd()
            {
                SetLogFileDone();
                LogToFileEnabled = true;
            }

            public static void WaitForLogFileDone()
            {
                LogFileDoneNotify.WaitOne();
            }
            public static void SetLogFileDone()
            {
                LogFileDoneNotify.Set();
            }

            public static void EnqueueLog(StringBuilder sb)
            {
                if (sb != null)
                {
                    LogQueue.Enqueue(sb);
                    LogNotify.Set();
                }
            }
            public static StringBuilder GetStringBuilder()
            {
                StringBuilder rv = null;
                if (LogPool.TryDequeue(out rv))
                {
                    rv.Remove(0, rv.Length);
                    return rv;
                }
                rv = new StringBuilder();
                return rv;
            }
            public static void ReturnStringBuilder(StringBuilder sb)
            {
                if (sb != null)
                {
                    LogPool.Enqueue(sb);
                }
            }

            public static void LogInfo(object obj)
            {
                if (!LogEnabled) return;
                if (!LogInfoEnabled) return;

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                if (LogToConsoleEnabled)
                {
                    UnityEngine.Debug.Log(obj);
                }
                else if (LogToFileEnabled)
                {
                    var time = DateTime.Now;
                    var msg = obj == null ? "nullptr" : obj.ToString();
                    var index = (System.Threading.Interlocked.Increment(ref _LogMessageIndex) - 1) % LogMessages.Length;
                    var message = new LogMessage() { Message = msg, StackTrace = "(omitted)", LogType = UnityEngine.LogType.Log, Time = time };
                    if (LogCSharpStackTraceEnabled)
                    {
                        message.StackTrace = GetStackTrace();
                    }
                    LogMessages[index] = message;

                    var sb = GetStringBuilder();
                    sb.AppendFormat("{0:HH\\:mm\\:ss.ff}", time);
                    sb.AppendLine(" I");
                    sb.AppendLine(msg);
                    if (LogCSharpStackTraceEnabled)
                    {
                        sb.AppendLine(message.StackTrace);
                    }
                    EnqueueLog(sb);
                }
#else
                if (LogToConsoleEnabled || LogToFileEnabled)
                {
                    var time = DateTime.Now;
                    var msg = obj == null ? "nullptr" : obj.ToString();
                    var sb = GetStringBuilder();
                    sb.AppendFormat("{0:HH\\:mm\\:ss.ff}", time);
                    sb.AppendLine(" I");
                    sb.AppendLine(msg);
                    if (LogCSharpStackTraceEnabled)
                    {
                        sb.AppendLine(GetStackTrace());
                    }
                    if (LogToConsoleEnabled)
                    {
                        Console.WriteLine(sb);
                    }
                    if (LogToFileEnabled)
                    {
                        EnqueueLog(sb);
                    }
                    else
                    {
                        ReturnStringBuilder(sb);
                    }
                }
#endif
            }

            public static void LogError(object obj)
            {
                if (!LogEnabled) return;
                if (!LogErrorEnabled) return;

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                if (LogToConsoleEnabled)
                {
                    UnityEngine.Debug.LogError(obj);
                }
                else if (LogToFileEnabled)
                {
                    var time = DateTime.Now;
                    var msg = obj == null ? "nullptr" : obj.ToString();
                    var index = (System.Threading.Interlocked.Increment(ref _LogMessageIndex) - 1) % LogMessages.Length;
                    var message = new LogMessage() { Message = msg, StackTrace = "(omitted)", LogType = UnityEngine.LogType.Error, Time = time };
                    if (LogCSharpStackTraceEnabled)
                    {
                        message.StackTrace = GetStackTrace();
                    }
                    LogMessages[index] = message;

                    var sb = GetStringBuilder();
                    sb.AppendFormat("{0:HH\\:mm\\:ss.ff}", time);
                    sb.AppendLine(" E");
                    sb.AppendLine(msg);
                    if (LogCSharpStackTraceEnabled)
                    {
                        sb.AppendLine(message.StackTrace);
                    }
                    EnqueueLog(sb);
                }
#else
                if (LogToConsoleEnabled || LogToFileEnabled)
                {
                    var time = DateTime.Now;
                    var msg = obj == null ? "nullptr" : obj.ToString();
                    var sb = GetStringBuilder();
                    sb.AppendFormat("{0:HH\\:mm\\:ss.ff}", time);
                    sb.AppendLine(" E");
                    sb.AppendLine(msg);
                    if (LogCSharpStackTraceEnabled)
                    {
                        sb.AppendLine(GetStackTrace());
                    }
                    if (LogToConsoleEnabled)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(sb);
                        Console.ResetColor();
                    }
                    if (LogToFileEnabled)
                    {
                        EnqueueLog(sb);
                    }
                    else
                    {
                        ReturnStringBuilder(sb);
                    }
                }
#endif
            }

            public static void LogWarning(object obj)
            {
                if (!LogEnabled) return;
                if (!LogWarningEnabled) return;

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                if (LogToConsoleEnabled)
                {
                    UnityEngine.Debug.LogWarning(obj);
                }
                else if (LogToFileEnabled)
                {
                    var time = DateTime.Now;
                    var msg = obj == null ? "nullptr" : obj.ToString();
                    var index = (System.Threading.Interlocked.Increment(ref _LogMessageIndex) - 1) % LogMessages.Length;
                    var message = new LogMessage() { Message = msg, StackTrace = "(omitted)", LogType = UnityEngine.LogType.Warning, Time = time };
                    if (LogCSharpStackTraceEnabled)
                    {
                        message.StackTrace = GetStackTrace();
                    }
                    LogMessages[index] = message;

                    var sb = GetStringBuilder();
                    sb.AppendFormat("{0:HH\\:mm\\:ss.ff}", time);
                    sb.AppendLine(" W");
                    sb.AppendLine(msg);
                    if (LogCSharpStackTraceEnabled)
                    {
                        sb.AppendLine(message.StackTrace);
                    }
                    EnqueueLog(sb);
                }
#else
                if (LogToConsoleEnabled || LogToFileEnabled)
                {
                    var time = DateTime.Now;
                    var msg = obj == null ? "nullptr" : obj.ToString();
                    var sb = GetStringBuilder();
                    sb.AppendFormat("{0:HH\\:mm\\:ss.ff}", time);
                    sb.AppendLine(" W");
                    sb.AppendLine(msg);
                    if (LogCSharpStackTraceEnabled)
                    {
                        sb.AppendLine(GetStackTrace());
                    }
                    if (LogToConsoleEnabled)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(sb);
                        Console.ResetColor();
                    }
                    if (LogToFileEnabled)
                    {
                        EnqueueLog(sb);
                    }
                    else
                    {
                        ReturnStringBuilder(sb);
                    }
                }
#endif
            }
        }
        #endregion

        public static event Action PreQuitting = () => { };
        public static event Action Quitting = () => { };
        private static void FastQuit()
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        private static bool _FastQuitEnabled = false;
        public static void EnableFastQuit()
        {
            _FastQuitEnabled = true;
        }
        public static void DisableFastQuit()
        {
            _FastQuitEnabled = false;
        }

        static PlatDependant()
        {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            UnityEngine.Application.logMessageReceivedThreaded += (condition, stackTrace, type) =>
            {
                Logger.OnUnityLogReceived(condition, stackTrace, type);
            };
#else
            LogCSharpStackTraceEnabled = false;
#endif

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            UnityEngine.Application.quitting += () =>
            {
                PreQuitting();
                Quitting();
                if (_FastQuitEnabled)
                {
                    FastQuit();
                }
            };
#else
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                PreQuitting();
                Quitting();
                if (_FastQuitEnabled)
                {
                    FastQuit();
                }
            };
#endif
        }

        public static string SendLogBegin()
        {
            return Logger.SendLogBegin();
        }
        public static void SendLogEnd()
        {
            Logger.SendLogEnd();
        }
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static string[] CopyRecentLogMessages()
        {
            return Logger.CopyLogMessages();
        }
        public static void RePrintRecentLogs()
        {
            DisableLogTemp = true;
            var logs = CopyRecentLogMessages();
            UnityEngine.Debug.LogError("=================== ReShow Log Start ====================");
            for (int i = 0; i < logs.Length; ++i)
            {
                UnityEngine.Debug.LogError(logs[i]);
            }
            UnityEngine.Debug.LogError("=================== ReShow Log Done ====================");
            DisableLogTemp = false;
        }
#endif

        public static void LogInfo(this object obj)
        {
            Logger.LogInfo(obj);
        }

        public static void LogError(this object obj)
        {
            Logger.LogError(obj);
        }

        public static void LogWarning(this object obj)
        {
            Logger.LogWarning(obj);
        }

        public static bool IsValueType(this Type type)
        {
#if NETFX_CORE
            return type.GetTypeInfo().IsValueType;
#else
            return type.IsValueType;
#endif
        }

        public static bool IsEnum(this Type type)
        {
#if NETFX_CORE
            return type.GetTypeInfo().IsEnum;
#else
            return type.IsEnum;
#endif
        }

        public static bool IsGenericType(this Type type)
        {
#if NETFX_CORE
            return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }

        public static bool ContainsGenericParameters(this Type type)
        {
#if NETFX_CORE
            return type.GetTypeInfo().ContainsGenericParameters;
#else
            return type.ContainsGenericParameters;
#endif
        }

        public static TypeCode GetTypeCode(this Type type)
        {
#if NETFX_CORE
            if (type.IsEnum())
            {
                return TypeCode.Int32;
            }
            return WinRTLegacy.TypeExtensions.GetTypeCode(type);
#else
            return Type.GetTypeCode(type);
#endif
        }

        public static bool IsObjIConvertible(this object obj)
        {
            return obj is IConvertible;
        }

        public static bool IsTypeIConvertible(this Type type)
        {
            return typeof(IConvertible).IsAssignableFrom(type);
        }

        public static Type[] GetAllNestedTypes(this Type type)
        {
#if NETFX_CORE
            List<Type> types = new List<Type>();
            while (type != null)
            {
                try
                {
                    types.AddRange(type.GetTypeInfo().DeclaredNestedTypes.Select(ti => ti.AsType()));
                }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                try
                {
                    type = type.GetTypeInfo().BaseType;
                }
                catch (Exception e)
                {
                    LogInfo(e);
                    type = null;
                }
            }
            return types.ToArray();
#else
            HashSet<Type> types = new HashSet<Type>();
            while (type != null)
            {
                try
                {
                    types.UnionWith(type.GetNestedTypes());
                }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                try
                {
                    type = type.BaseType;
                }
                catch (Exception e)
                {
                    LogInfo(e);
                    type = null;
                }
            }
            return types.ToArray();
#endif
        }

        public static MethodInfo GetDelegateMethod(this Delegate del)
        {
#if NETFX_CORE
            return del.GetMethodInfo();
#else
            return del.Method;
#endif
        }

        private readonly static char[] _FolderSeparators = new[] { '\\', '/' };
        public static string GetRelativePath(string relativeTo, string path)
        {
            var fullrel = System.IO.Path.GetFullPath(relativeTo);
            var fullpath = System.IO.Path.GetFullPath(path);
            var partsrel = fullrel.Split(_FolderSeparators, StringSplitOptions.RemoveEmptyEntries);
            var partspath = fullpath.Split(_FolderSeparators, StringSplitOptions.RemoveEmptyEntries);

            int diffindex = 0;
            for (; diffindex < partsrel.Length && diffindex < partspath.Length; ++diffindex)
            {
                var partrel = partsrel[diffindex];
                var partpath = partspath[diffindex];
                if (!string.Equals(partrel, partpath, StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }
            }
            if (diffindex == 0)
            {
                // totally different.
                return fullpath;
            }
            else
            {
                System.Text.StringBuilder sb = new StringBuilder();
                if (diffindex == partsrel.Length)
                {
                    // fully based on relativeTo
                    sb.Append(".");
                }
                else
                {
                    for (int i = diffindex; i < partsrel.Length; ++i)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append("/");
                        }
                        sb.Append("..");
                    }
                }

                if (diffindex == partspath.Length)
                {
                    // path is a part of relativeTo
                }
                else
                {
                    for (int i = diffindex; i < partspath.Length; ++i)
                    {
                        sb.Append("/");
                        sb.Append(partspath[i]);
                    }
                }
                return sb.ToString();
            }
        }

        public static byte[] ReadAllBytes(this string path)
        {
            using (var src = OpenRead(path))
            {
                if (src != null)
                {
                    int len = -1;
                    try
                    {
                        len = (int)src.Length;
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                    }
                    if (len >= 0)
                    {
                        var result = new byte[len];
                        src.Read(result, 0, len);
                        return result;
                    }
                    else
                    {
                        List<byte> result = new List<byte>();
                        byte[] buffer = CopyStreamBuffer;
                        int readcnt = 0;
                        while ((readcnt = src.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            result.AddRange(new ArraySegment<byte>(buffer, 0, readcnt));
                        }
                        return result.ToArray();
                    }
                }
            }
            return null;
        }
        public static void WriteBytes(this string path, byte[] data, int offset, int count)
        {
            //if (data != null && offset >= 0 && count >= 0 && offset + count <= data.Length)
            {
                using (var stream = OpenWrite(path))
                {
                    if (stream != null)
                    {
                        stream.Write(data, offset, count);
                    }
                }
            }
        }
        public static void WriteAllBytes(this string path, byte[] data)
        {
            WriteBytes(path, data, 0, data.Length);
        }
        public static void AppendBytes(this string path, byte[] data, int offset, int count)
        {
            //if (data != null && offset >= 0 && count >= 0 && offset + count <= data.Length)
            {
                using (var stream = OpenAppend(path))
                {
                    if (stream != null)
                    {
                        stream.Write(data, offset, count);
                    }
                }
            }
        }
        public static void AppendAllBytes(this string path, byte[] data)
        {
            AppendBytes(path, data, 0, data.Length);
        }
        public static string ReadAllText(this string path)
        {
            using (var sr = OpenReadText(path))
            {
                if (sr != null)
                {
                    return sr.ReadToEnd();
                }
            }
            return null;
        }
        public static void WriteAllText(this string path, string text)
        {
            using (var sw = OpenWriteText(path))
            {
                if (sw != null)
                {
                    sw.Write(text);
                }
            }
        }
        public static void AppendAllText(this string path, string text)
        {
            using (var sw = OpenAppendText(path))
            {
                if (sw != null)
                {
                    sw.Write(text);
                }
            }
        }
        public static string[] ReadAllLines(this string path)
        {
            using (var sr = OpenReadText(path))
            {
                if (sr != null)
                {
                    List<string> lines = new List<string>();
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    // Trim tailing empty lines.
                    while (lines.Count > 0 && lines[lines.Count - 1] == "")
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }
                    return lines.ToArray();
                }
            }
            return null;
        }
        public static void WriteAllLines(this string path, IList<string> lines)
        {
            using (var sw = OpenWriteText(path))
            {
                if (sw != null)
                {
                    if (lines != null)
                    {
                        for (int i = 0; i < lines.Count; ++i)
                        {
                            sw.WriteLine(lines[i]);
                        }
                    }
                }
            }
        }
        public static void WriteAllLines(this string path, params string[] lines)
        {
            WriteAllLines(path, (IList<string>)lines);
        }
        public static void AppendAllLines(this string path, IList<string> lines)
        {
            using (var sw = OpenAppendText(path))
            {
                if (sw != null)
                {
                    if (lines != null)
                    {
                        for (int i = 0; i < lines.Count; ++i)
                        {
                            sw.WriteLine(lines[i]);
                        }
                    }
                }
            }
        }
        public static void AppendAllLines(this string path, params string[] lines)
        {
            AppendAllLines(path, (IList<string>)lines);
        }

        public static System.IO.StreamReader OpenReadText(this string path)
        {
            try
            {
                var stream = OpenRead(path);
                if (stream != null)
                {
                    return new System.IO.StreamReader(stream);
                }
            }
            catch (Exception e)
            {
                LogInfo(e);
            }
            return null;
        }

        public static System.IO.Stream OpenRead(this string path)
        {
            for (int i = 0; i <= 3; ++i)
            { // retry 3 times
                try
                {
                    return System.IO.File.OpenRead(path);
                }
                catch (ArgumentException) { break; }
                catch (DirectoryNotFoundException) { break; }
                catch (FileNotFoundException) { break; }
                catch (NotSupportedException) { break; }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                Sleep(1);
            }
            return null;
        }

        public static System.IO.StreamWriter OpenWriteText(this string path)
        {
            try
            {
                var stream = OpenWrite(path);
                if (stream != null)
                {
                    return new System.IO.StreamWriter(stream);
                }
            }
            catch (Exception e)
            {
                LogInfo(e);
            }
            return null;
        }

        public static System.IO.Stream OpenWrite(this string path)
        {
            var stream = OpenAppend(path);
            stream.SetLength(0);
            return stream;
        }

        public static System.IO.StreamWriter OpenAppendText(this string path)
        {
            try
            {
                var stream = OpenAppend(path);
                if (stream != null)
                {
                    return new System.IO.StreamWriter(stream);
                }
            }
            catch (Exception e)
            {
                LogInfo(e);
            }
            return null;
        }

        public static System.IO.Stream OpenAppend(this string path)
        {
            CreateFolder(System.IO.Path.GetDirectoryName(path));
            for (int i = 0; i <= 3; ++i)
            { // retry 3 times
                try
                {
                    var stream = System.IO.File.OpenWrite(path);
                    if (stream != null)
                    {
                        if (stream.CanSeek)
                        {
                            stream.Seek(0, System.IO.SeekOrigin.End);
                        }
                        else
                        {
                            LogInfo(path + " cannot append.");
                        }
                        return stream;
                    }
                }
                catch (ArgumentException) { break; }
                catch (NotSupportedException) { break; }
                catch (DirectoryNotFoundException) { break; }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                Sleep(1);
            }
            return null;
        }

        public static bool IsFileExist(this string path)
        {
            try
            {
                return System.IO.File.Exists(path);
            }
            catch (Exception e)
            {
                LogInfo(e);
                return false;
            }
        }
        public static bool IsFolderExist(this string path)
        {
            try
            {
                return System.IO.Directory.Exists(path);
            }
            catch (Exception e)
            {
                LogInfo(e);
                return false;
            }
        }

        public static void DeleteFile(this string path)
        {
            for (int i = 0; i <= 3; ++i)
            { // retry 3 times
                try
                {
                    System.IO.File.Delete(path);
                    return;
                }
                catch (ArgumentException) { break; }
                catch (NotSupportedException) { break; }
                catch (DirectoryNotFoundException) { break; }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                Sleep(1);
            }
        }

        public static string[] GetAllFiles(this string dir)
        {
            try
            {
                List<string> files = new List<string>();
                try
                {
                    var subfiles = System.IO.Directory.GetFiles(dir);
                    if (subfiles != null)
                    {
                        for (int i = 0; i < subfiles.Length; ++i)
                        {
                            var file = subfiles[i].Replace('\\', '/');
                            files.Add(file);
                        }
                    }
                }
                catch (System.IO.DirectoryNotFoundException) { }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                try
                {
                    var subs = System.IO.Directory.GetDirectories(dir);
                    if (subs != null)
                    {
                        for (int i = 0; i < subs.Length; ++i)
                        {
                            try
                            {
                                files.AddRange(GetAllFiles(subs[i]));
                            }
                            catch (Exception e)
                            {
                                LogInfo(e);
                            }
                        }
                    }
                }
                catch (System.IO.DirectoryNotFoundException) { }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                return files.ToArray();
            }
            catch (System.IO.DirectoryNotFoundException) { }
            catch (Exception e)
            {
                LogInfo(e);
            }
            return new string[0];
        }

        public static string[] GetSubFiles(this string dir)
        {
            try
            {
                List<string> files = new List<string>();
                try
                {
                    var subfiles = System.IO.Directory.GetFiles(dir);
                    if (subfiles != null)
                    {
                        for (int i = 0; i < subfiles.Length; ++i)
                        {
                            var file = subfiles[i].Replace('\\', '/');
                            files.Add(file);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                return files.ToArray();
            }
            catch (System.IO.DirectoryNotFoundException) { }
            catch (Exception e)
            {
                LogInfo(e);
            }
            return new string[0];
        }

        public static string[] GetAllFolders(this string dir)
        {
            try
            {
                List<string> files = new List<string>();
                try
                {
                    var subs = System.IO.Directory.GetDirectories(dir);
                    if (subs != null)
                    {
                        for (int i = 0; i < subs.Length; ++i)
                        {
                            try
                            {
                                var sub = subs[i].Replace('\\', '/');
                                files.Add(sub);
                                files.AddRange(GetAllFolders(sub));
                            }
                            catch (Exception e)
                            {
                                LogInfo(e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                return files.ToArray();
            }
            catch (System.IO.DirectoryNotFoundException) { }
            catch (Exception e)
            {
                LogInfo(e);
            }
            return new string[0];
        }

        public static string[] GetSubFolders(this string dir)
        {
            try
            {
                List<string> files = new List<string>();
                try
                {
                    var subs = System.IO.Directory.GetDirectories(dir);
                    if (subs != null)
                    {
                        for (int i = 0; i < subs.Length; ++i)
                        {
                            try
                            {
                                var sub = subs[i].Replace('\\', '/');
                                files.Add(sub);
                            }
                            catch (Exception e)
                            {
                                LogInfo(e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                return files.ToArray();
            }
            catch (System.IO.DirectoryNotFoundException) { }
            catch (Exception e)
            {
                LogInfo(e);
            }
            return new string[0];
        }

        public static void CreateFolder(this string path)
        {
            for (int i = 0; i <= 3; ++i)
            { // retry 3 times
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                    return;
                }
                catch (ArgumentException) { break; }
                catch (NotSupportedException) { break; }
                catch (Exception e)
                {
                    LogInfo(e);
                }
                Sleep(1);
            }
        }

        [ThreadStatic] private static byte[] _CopyStreamBuffer;
        public static byte[] CopyStreamBuffer
        {
            get
            {
                if (_CopyStreamBuffer == null)
                {
                    _CopyStreamBuffer = new byte[64 * 1024];
                }
                return _CopyStreamBuffer;
            }
        }
        public static void CopyTo(this System.IO.Stream src, System.IO.Stream dst)
        {
            byte[] buffer = CopyStreamBuffer;
            int readcnt = 0;
            while ((readcnt = src.Read(buffer, 0, buffer.Length)) != 0)
            {
                dst.Write(buffer, 0, readcnt);
                dst.Flush();
            }
        }

        public static bool IsFileSameName(this string src, string dst)
        {
            try
            {
                if (src == dst)
                {
                    return true;
                }
                if (string.IsNullOrEmpty(src))
                {
                    if (string.IsNullOrEmpty(dst))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (string.IsNullOrEmpty(dst))
                {
                    return false;
                }

                if (System.IO.Path.GetFullPath(src) == System.IO.Path.GetFullPath(dst))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                LogInfo(e);
            }
            return false;
        }

        public static void CopyFile(this string src, string dst)
        {
            if (IsFileSameName(src, dst))
            {
                return;
            }

            if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(dst))
            {
                try
                {
                    CreateFolder(System.IO.Path.GetDirectoryName(dst));
                    System.IO.File.Copy(src, dst, true);
                }
                catch (Exception e)
                {
                    LogInfo(e);
                }
            }
        }
        public static void MoveFile(this string src, string dst)
        {
            if (IsFileSameName(src, dst))
            {
                LogInfo("Move same file: " + (src ?? "") + " -> " + (dst ?? ""));
                return;
            }

            if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(dst))
            {
                CreateFolder(System.IO.Path.GetDirectoryName(dst));
                // try to lock src and delete dst.
                { 
                    System.IO.Stream srcfile = null;
                    try
                    {
                        srcfile = OpenRead(src);
                        DeleteFile(dst);
                    }
                    catch (Exception e)
                    {
                        LogInfo(e);
                    }
                    finally
                    {
                        if (srcfile != null)
                        {
                            srcfile.Dispose();
                        }
                    }
                }
                try
                {
                    System.IO.File.Move(src, dst);
                }
                catch (Exception e)
                {
                    LogError(e);
                    throw;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(src))
                {
                    LogInfo("MoveFile, src is empty");
                }
                if (string.IsNullOrEmpty(dst))
                {
                    LogInfo("MoveFile, dst is empty");
                }
            }
        }

        public static TaskProgress RunBackground(Action<TaskProgress> work)
        {
            var prog = new TaskProgress();
#if NETFX_CORE
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    work(prog);
                }
                catch (Exception e)
                {
                    LogError(e);
                    prog.Error = e.Message;
                }
                finally
                {
                    prog.Done = true;
                }
            });
#else
            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    work(prog);
                }
#if UNITY_EDITOR
                catch (System.Threading.ThreadAbortException) { }
#endif
                catch (Exception e)
                {
                    LogError(e);
                    prog.Error = e.Message;
                }
                finally
                {
                    prog.Done = true;
                }
            });
#endif
            return prog;
        }
#if NETFX_CORE
        public static TaskProgress RunBackground(Func<TaskProgress, System.Threading.Tasks.Task> work)
        {
            var prog = new TaskProgress();
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await work(prog);
                }
                catch (Exception e)
                {
                    LogError(e);
                    prog.Error = e.Message;
                }
                finally
                {
                    prog.Done = true;
                }
            });
            return prog;
        }
#endif

#if NETFX_CORE
        public static TaskProgress RunBackgroundLongTime(Action<TaskProgress> work)
        {
            var prog = new TaskProgress();
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    work(prog);
                }
                catch (Exception e)
                {
                    LogError(e);
                    prog.Error = e.Message;
                }
                finally
                {
                    prog.Done = true;
                }
            });
            prog.Task = task;
            return prog;
        }
#else
        public static TaskProgress RunBackgroundLongTime(Action<TaskProgress> work)
        {
            var prog = new TaskProgress();
            var thread = new System.Threading.Thread(state =>
            {
                var progress = state as TaskProgress;
                try
                {
                    work(progress);
                }
#if UNITY_EDITOR
                catch (System.Threading.ThreadAbortException) { }
#endif
                catch (Exception e)
                {
                    LogError(e);
                    progress.Error = e.Message;
                }
                finally
                {
                    progress.Done = true;
                }
            });
            thread.IsBackground = true;
            thread.Start(prog);
            prog.Task = thread;
            return prog;
        }
#endif

        public static void Sleep(int milliseconds)
        {
#if NETFX_CORE
            System.Threading.Tasks.Task.Delay(milliseconds).Wait();
#else
            System.Threading.Thread.Sleep(milliseconds);
#endif
        }

        public static TaskProgress UnzipAsync(string zip, string destdir)
        {
            var prog = new TaskProgress();
            try
            {
                if (IsFileExist(zip))
                {
                    var taskcnt = Environment.ProcessorCount;
                    int entryIndex = 0;
                    int finishCnt = 0;
                    Action<TaskProgress> UnzipWork = p =>
                    {
                        try
                        {
                            using (var stream = PlatDependant.OpenRead(zip))
                            {
                                using (var zipa = new ZipArchive(stream, ZipArchiveMode.Read))
                                {
                                    var entries = zipa.Entries;
                                    if (entries != null)
                                    {
                                        var index = System.Threading.Interlocked.Increment(ref entryIndex) - 1;
                                        if (index == 0)
                                        {
                                            prog.Total = entries.Count;
                                        }
                                        while (index < entries.Count)
                                        {
                                            System.Threading.Interlocked.Increment(ref prog.Length);

                                            try
                                            {
                                                var entry = entries[index];
                                                var name = entry.FullName;
                                                var dest = System.IO.Path.Combine(destdir, name);
                                                using (var srcs = entry.Open())
                                                {
                                                    if (!dest.EndsWith("/") && !dest.EndsWith("\\"))
                                                    {
                                                        using (var dsts = PlatDependant.OpenWrite(dest))
                                                        {
                                                            srcs.CopyTo(dsts);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                LogError(e);
                                                prog.Error = e.Message;
                                            }

                                            index = System.Threading.Interlocked.Increment(ref entryIndex) - 1;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(e);
                            prog.Error = e.Message;
                        }
                        finally
                        {
                            if (System.Threading.Interlocked.Increment(ref finishCnt) >= taskcnt)
                            {
                                prog.Done = true;
                            }
                        }
                    };
                    for (int i = 0; i < taskcnt; ++i)
                    {
                        RunBackground(UnzipWork);
                    }
                    return prog;
                }
            }
            catch (Exception e)
            {
                LogError(e);
                prog.Error = e.Message;
            }
            prog.Done = true;
            return prog;
        }

#if UNITY_EDITOR
        public static bool ExecuteProcess(System.Diagnostics.ProcessStartInfo si)
        {
            bool safeWaitMode = true;
#if UNITY_EDITOR_WIN
            safeWaitMode = false;
#endif
            // TODO: on Apple M1, we must use safeWaitMode. we should test non-safeMode on Mac-on-Intel and Linux and add "#if" here. NOTICE: use SystemInfo.processorType to get cpu model name.

            si.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.CreateNoWindow = true;

            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = si;

                process.OutputDataReceived += (s, e) => WriteProcessOutput(s as System.Diagnostics.Process, e.Data, false);

                process.ErrorDataReceived += (s, e) => WriteProcessOutput(s as System.Diagnostics.Process, e.Data, true);

                System.Threading.ManualResetEventSlim waitHandleForProcess = null;
                if (safeWaitMode)
                {
                    waitHandleForProcess = new System.Threading.ManualResetEventSlim();
                    process.Exited += (s, e) => waitHandleForProcess.Set();
                }

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (waitHandleForProcess)
                {
                    while (!process.HasExited)
                    {
                        if (safeWaitMode)
                        {
                            waitHandleForProcess.Wait(1000);
                        }
                        else
                        {
                            process.WaitForExit(1000);
                        }
                    }
                }

                if (process.ExitCode != 0)
                {
                    LogError(string.Format("Error when execute process {0} {1}", si.FileName, si.Arguments));
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        private static void WriteProcessOutput(System.Diagnostics.Process p, string data, bool isError)
        {
            if (!string.IsNullOrEmpty(data))
            {
                string processName = System.IO.Path.GetFileName(p.StartInfo.FileName);
#if UNITY_EDITOR_OSX
                if (processName == "wine" || processName == "mono")
                {
                    processName = System.IO.Path.GetFileName(p.StartInfo.Arguments.Split(' ').FirstOrDefault());
                }
#endif
                if (!isError)
                {
                    LogInfo(string.Format("[{0}] {1}", processName, data));
                }
                else
                {
                    LogError(string.Format("[{0} Error] {1}", processName, data));
                }
            }
        }
#endif
    }

    public class TaskProgress
    {
        public long Length = 0;
        public long Total = 0;
        public volatile bool Done = false;
        public string Error = null;
        public object Task = null;

        public Action OnCancel = null;
        public void Cancel()
        {
            if (OnCancel != null)
            {
                OnCancel();
            }
        }
    }
}
