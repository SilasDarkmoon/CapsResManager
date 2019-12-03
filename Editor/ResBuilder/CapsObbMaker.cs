using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
using CompressionLevel = Unity.IO.Compression.CompressionLevel;
#else
using System.IO.Compression;
using CompressionLevel = System.IO.Compression.CompressionLevel;
#endif

namespace Capstones.UnityEditorEx
{
    public static class CapsObbMaker
    {
        public static void MakeObb(string dest, params string[] subzips)
        {
            if (!string.IsNullOrEmpty(dest) && subzips != null && subzips.Length > 0)
            {
                HashSet<string> reskeys = new HashSet<string>();
                HashSet<string> sptkeys = new HashSet<string>();
                using (var sdest = PlatDependant.OpenWrite(dest))
                {
                    using (var zdest = new ZipArchive(sdest, ZipArchiveMode.Create))
                    {
                        for (int i = 0; i < subzips.Length; ++i)
                        {
                            try
                            {
                                var sfile = subzips[i];
                                if (PlatDependant.IsFileExist(sfile))
                                {
                                    var key = System.IO.Path.GetFileNameWithoutExtension(sfile).ToLower();
                                    bool isres = false;
                                    bool isspt = false;
                                    HashSet<string> entrynames = new HashSet<string>();
                                    using (var ssrc = PlatDependant.OpenRead(sfile))
                                    {
                                        using (var zsrc = new ZipArchive(ssrc, ZipArchiveMode.Read))
                                        {
                                            foreach (var sentry in zsrc.Entries)
                                            {
                                                var fullname = sentry.FullName;
                                                if (fullname.StartsWith("res/"))
                                                {
                                                    isres = true;
                                                }
                                                else if (fullname.StartsWith("spt/"))
                                                {
                                                    isspt = true;
                                                }
                                                if (entrynames.Add(fullname))
                                                {
                                                    var dentry = zdest.CreateEntry(fullname, isres ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
                                                    using (var ses = sentry.Open())
                                                    {
                                                        using (var des = dentry.Open())
                                                        {
                                                            ses.CopyTo(des);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (isres)
                                    {
                                        reskeys.Add(key);
                                    }
                                    if (isspt)
                                    {
                                        sptkeys.Add(key);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }

                        if (reskeys.Count > 0)
                        {
                            var resindex = zdest.CreateEntry("res/index.txt", CompressionLevel.Optimal);
                            using (var sindex = resindex.Open())
                            {
                                using (var swindex = new System.IO.StreamWriter(sindex, System.Text.Encoding.UTF8))
                                {
                                    foreach (var key in reskeys)
                                    {
                                        swindex.WriteLine(key);
                                    }
                                }
                            }
                        }
                        if (sptkeys.Count > 0)
                        {
                            var sptindex = zdest.CreateEntry("spt/index.txt", CompressionLevel.Optimal);
                            using (var sindex = sptindex.Open())
                            {
                                using (var swindex = new System.IO.StreamWriter(sindex, System.Text.Encoding.UTF8))
                                {
                                    foreach (var key in sptkeys)
                                    {
                                        swindex.WriteLine(key);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
