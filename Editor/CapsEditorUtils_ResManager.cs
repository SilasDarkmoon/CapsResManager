using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Capstones.UnityEditorEx
{
    public static partial class CapsEditorUtils
    {
        public static void HideFile(string path)
        {
#if UNITY_EDITOR_OSX
            var si = new System.Diagnostics.ProcessStartInfo("chflags", "hidden \"" + path + "\"");
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#else
            if (System.IO.Directory.Exists(path))
            {
                var di = new System.IO.DirectoryInfo(path);
                di.Attributes |= System.IO.FileAttributes.Hidden;
            }
            else
            {
                var fi = new System.IO.FileInfo(path);
                if (fi.Exists)
                {
                    fi.Attributes |= System.IO.FileAttributes.Hidden;
                }
            }
#endif
        }
        public static void UnhideFile(string path)
        {
#if UNITY_EDITOR_OSX
            var si = new System.Diagnostics.ProcessStartInfo("chflags", "nohidden \"" + path + "\"");
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#else
            if (System.IO.Directory.Exists(path))
            {
                var di = new System.IO.DirectoryInfo(path);
                di.Attributes &= ~System.IO.FileAttributes.Hidden;
            }
            else
            {
                var fi = new System.IO.FileInfo(path);
                if (fi.Exists)
                {
                    fi.Attributes &= ~System.IO.FileAttributes.Hidden;
                }
            }
#endif
        }
        public static bool IsFileHidden(string path)
        {
#if UNITY_EDITOR_OSX
            var si = new System.Diagnostics.ProcessStartInfo("ls", "-lOd \"" + path + "\"");
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
            var output = p.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(output))
            {
                return false;
            }
            output = output.Trim();
            if (output.EndsWith(path, System.StringComparison.InvariantCultureIgnoreCase))
            {
                output = output.Substring(0, output.Length - path.Length).Trim();
            }
            var idsplit = output.IndexOfAny(new[] { '/', '\\' });
            if (idsplit >= 0)
            {
                output = output.Substring(0, idsplit);
            }
            return output.Contains("hidden");
#else
            if (System.IO.Directory.Exists(path))
            {
                var di = new System.IO.DirectoryInfo(path);
                return (di.Attributes & System.IO.FileAttributes.Hidden) != 0;
            }
            else
            {
                var fi = new System.IO.FileInfo(path);
                if (fi.Exists)
                {
                    return (fi.Attributes & System.IO.FileAttributes.Hidden) != 0;
                }
            }
            return false;
#endif
        }

        public static void DeleteDirLink(string path)
        {
#if UNITY_EDITOR_WIN
            var si = new System.Diagnostics.ProcessStartInfo("cmd", "/C \"rmdir \"" + path.Replace('/', '\\') + "\"\"");
            si.CreateNoWindow = true;
            si.UseShellExecute = false;
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#else
            var si = new System.Diagnostics.ProcessStartInfo("rm", "\"" + path + "\"");
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#endif
        }
        public static void MakeDirLink(string link, string target)
        {
#if UNITY_EDITOR_WIN
            var si = new System.Diagnostics.ProcessStartInfo("cmd", "/C \"mklink /D \"" + link.Replace('/', '\\') + "\"" + " \"" + target.Replace('/', '\\') + "\"\"");
            si.CreateNoWindow = true;
            si.UseShellExecute = false;
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                si = new System.Diagnostics.ProcessStartInfo("cmd", "/C \"mklink /J \"" + link.Replace('/', '\\') + "\"" + " \"" + (target.StartsWith(".") ? System.IO.Path.GetDirectoryName(link.Replace('/', '\\').TrimEnd('\\')) + "\\" : "") + target.Replace('/', '\\') + "\"\"");
                si.CreateNoWindow = true;
                si.UseShellExecute = false;
                p = System.Diagnostics.Process.Start(si);
                p.WaitForExit();
            }
#else
            var si = new System.Diagnostics.ProcessStartInfo("ln", "-s \"" + target + "\"" + " \"" + link + "\"");
            var p = System.Diagnostics.Process.Start(si);
            p.WaitForExit();
#endif
        }
        public static bool IsDirLink(string path)
        {
#if UNITY_EDITOR_WIN
            var di = new System.IO.DirectoryInfo(path);
            return di.Exists && (di.Attributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
#else
            if (System.IO.Directory.Exists(path) || System.IO.File.Exists(path))
            {
                var di = new System.IO.DirectoryInfo(path);
                return (di.Attributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
            }
            return false;
#endif
        }

        public static int __LINE__
        {
            get
            {
                return new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileLineNumber();
            }
        }
        public static string __FILE__
        {
            get
            {
                return new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();
            }
        }
        public static string __ASSET__
        {
            get
            {
                var file = new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();

                return GetAssetNameFromPath(file) ?? file;
            }
        }
        public static string __MOD__
        {
            get
            {
                var file = new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();

                //return CapsModEditor.GetAssetModName(GetAssetNameFromPath(file));

                var package = CapsModEditor.GetPackageNameFromPath(file);
                if (string.IsNullOrEmpty(package))
                {
                    var rootdir = System.Environment.CurrentDirectory;
                    if (file.StartsWith(rootdir))
                    {
                        file = file.Substring(rootdir.Length).TrimStart('/', '\\');
                    }
                    file = file.Replace('\\', '/');
                    //var iassets = file.IndexOf("Assets/");
                    //if (iassets > 0)
                    //{
                    //    file = file.Substring(iassets);
                    //}
                    return CapsModEditor.GetAssetModName(file);
                }
                else
                {
                    return CapsModEditor.GetPackageModName(package);
                }
            }
        }

        public static string GetAssetNameFromPath(string path)
        {
            var file = path;
            var package = CapsModEditor.GetPackageNameFromPath(file);
            if (string.IsNullOrEmpty(package))
            {
                var rootdir = System.Environment.CurrentDirectory;
                if (file.StartsWith(rootdir, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    file = file.Substring(rootdir.Length).TrimStart('/', '\\');
                }
                else
                {
                    return null;
                }
                //var iassets = file.IndexOf("Assets/");
                //if (iassets > 0)
                //{
                //    file = file.Substring(iassets);
                //}
                file = file.Replace('\\', '/');
                return file;
            }
            else
            {
                var rootdir = CapsModEditor.GetPackageRoot(package);
                file = file.Substring(rootdir.Length).TrimStart('/', '\\');
                file = file.Replace('\\', '/');
                file = "Packages/" + package + "/" + file;
                return file;
            }
        }

        public static bool ExecuteProcess(System.Diagnostics.ProcessStartInfo si)
        {
            si.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.CreateNoWindow = true;

            var process = new System.Diagnostics.Process();
            process.StartInfo = si;

            process.OutputDataReceived += (s, e) => WriteProcessOutput(s as System.Diagnostics.Process, e.Data, false);

            process.ErrorDataReceived += (s, e) => WriteProcessOutput(s as System.Diagnostics.Process, e.Data, true);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Debug.LogErrorFormat("Error when execute process {0} {1}", si.FileName, si.Arguments);
                return false;
            }
            else
            {
                return true;
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
                    Debug.LogFormat("[{0}] {1}", processName, data);
                }
                else
                {
                    Debug.LogErrorFormat("[{0} Error] {1}", processName, data);
                }
            }
        }

        public static string GetStreamMD5(System.IO.Stream stream)
        {
            try
            {
                byte[] hash = null;
                if (stream != null)
                {
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        hash = md5.ComputeHash(stream);
                    }
                }
                if (hash == null || hash.Length <= 0) return "";
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < hash.Length; ++i)
                {
                    sb.Append(hash[i].ToString("X2"));
                }
                return sb.ToString();
            }
            catch { }
            return "";
        }
        public static string GetFileMD5(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    using (var stream = System.IO.File.OpenRead(path))
                    {
                        return GetStreamMD5(stream);
                    }
                }
            }
            catch { }
            return "";
        }
        public static long GetFileLength(string path)
        {
            try
            {
                var f = new System.IO.FileInfo(path);
                return f.Length;
            }
            catch { }
            return 0;
        }
    }
}