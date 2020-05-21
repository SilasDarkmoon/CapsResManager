using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    public static class CapsDistributeEditor
    {
        internal static string GetDistributeFlagsFilePath()
        {
            //var mod = CapsEditorUtils.__MOD__;
            //string path = CapsModEditor.GetPackageOrModRoot(mod);
            //if (!string.IsNullOrEmpty(path))
            //{
            //    path += "/Resources";
            //    System.IO.Directory.CreateDirectory(path);
            //    path += "/DistributeFlags.txt";
            //}
            //else
            //{
            //    path = "Assets/Resources/DistributeFlags.txt";
            //}
            //return path;
            return "Assets/Resources/DistributeFlags.txt";
        }

        public static void CheckDefaultSelectedDistributeFlags()
        {
            var path = GetDistributeFlagsFilePath();
            if (!System.IO.File.Exists(path))
            {
                var src = CapsModEditor.FindAssetInMods("DefaultDistributeFlags.txt", true);
                if (src != null && System.IO.File.Exists(src))
                {
                    System.IO.File.Copy(src, path);
                    AssetDatabase.ImportAsset(path);
                    return;
                }
            }
        }

        public static string[] GetDistributesInMod(string mod)
        {
            List<string> dflags = new List<string>();
            HashSet<string> setflags = new HashSet<string>();

            Action<string> checkDir = pathc =>
            {
                if (System.IO.Directory.Exists(pathc))
                {
                    var dirs = System.IO.Directory.GetDirectories(pathc);
                    foreach (var dir in dirs)
                    {
                        var path = System.IO.Path.GetFileName(dir);
                        if (setflags.Add(path))
                        {
                            dflags.Add(path);
                        }
                    }
                }
            };
            Action<string> checkSub = sub =>
            {
                if (string.IsNullOrEmpty(mod))
                {
                    checkDir("Assets" + sub);
                }
                else
                {
                    var proot = CapsModEditor.GetModRootInPackage(mod);
                    if (!string.IsNullOrEmpty(proot))
                    {
                        checkDir(proot + sub);
                    }
                    checkDir("Assets/Mods/" + mod + sub);
                }
            };

            checkSub("/CapsRes/dist/");
            checkSub("/CapsSpt/dist/");
            checkSub("/Resources/dist/");

            return dflags.ToArray();
        }
        public static string[] GetAllDistributes()
        {
            List<string> dflags = new List<string>();
            HashSet<string> setflags = new HashSet<string>();

            var subflags = GetDistributesInMod("");
            for (int j = 0; j < subflags.Length; ++j)
            {
                var subflag = subflags[j];
                if (setflags.Add(subflag))
                {
                    dflags.Add(subflag);
                }
            }

            var mods = CapsModEditor.GetAllModsOrPackages();
            for (int i = 0; i < mods.Length; ++i)
            {
                subflags = GetDistributesInMod(mods[i]);
                for (int j = 0; j < subflags.Length; ++j)
                {
                    var subflag = subflags[j];
                    if (setflags.Add(subflag))
                    {
                        dflags.Add(subflag);
                    }
                }
            }

            return dflags.ToArray();
        }
        public static string[] GetOptionalDistributes()
        {
            HashSet<string> flags = new HashSet<string>(GetAllDistributes());
            flags.UnionWith(CapsModEditor.GetOptionalMods());
            var arrflags = flags.ToArray();
            Array.Sort(arrflags);
            return arrflags;
        }

        public static OrderedEvent<Action> OnDistributeFlagsChanged = (Action)(() => { });
        internal static void FireOnDistributeFlagsChanged()
        {
            OnDistributeFlagsChanged.Handler();
        }
    }

    public class DistributeSelectWindow : EditorWindow, ISerializationCallbackReceiver
    {
        [MenuItem("Res/Select Distribute Flags", priority = 200000)]
        public static void Init()
        {
            var win = GetWindow<DistributeSelectWindow>();
            win.titleContent = new GUIContent("Distribute Flags");
            win.DistributeFlags = new Dictionary<string, bool>();
            win.DistributeFlagOrder = new LinkedList<string>();
            var allflags = CapsDistributeEditor.GetOptionalDistributes();
            for (int i = 0; i < allflags.Length; ++i)
            {
                win.DistributeFlags[allflags[i]] = false;
            }
            var selflags = ResManager.PreRuntimeDFlags;
            for (int i = 0; i < selflags.Count; ++i)
            {
                win.SelectDistributeFlag(selflags[i], true);
            }
            win.SaveDistributeFlags();
        }

        public Dictionary<string, bool> DistributeFlags = new Dictionary<string, bool>();
        public LinkedList<string> DistributeFlagOrder = new LinkedList<string>();

        public Vector2 offset1, offset2;

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            if (GUILayout.Button("Reset"))
            {
                Init();
            }
            offset1 = GUILayout.BeginScrollView(offset1);
            Dictionary<string, bool> copy = new Dictionary<string, bool>(DistributeFlags);
            foreach (var kvp in copy)
            {
                SelectDistributeFlag(kvp.Key, EditorGUILayout.ToggleLeft(kvp.Key, kvp.Value, GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent(kvp.Key)).x + GUI.skin.toggle.CalcSize(GUIContent.none).x)));
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            if (GUILayout.Button("Apply"))
            {
                SaveDistributeFlags();
                Close();
            }
            offset2 = GUILayout.BeginScrollView(offset2);
            var node = DistributeFlagOrder.First;
            LinkedListNode<string> nodeup = null, nodedown = null, noderemove = null;
            while (node != null)
            {
                var curnode = node;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("X", new GUIStyle(GUI.skin.button) { stretchWidth = false, fixedWidth = 25 })) noderemove = curnode;
                if (GUILayout.Button("▲", new GUIStyle(GUI.skin.button) { stretchWidth = false, fixedWidth = 25 })) nodeup = curnode;
                if (GUILayout.Button("▼", new GUIStyle(GUI.skin.button) { stretchWidth = false, fixedWidth = 25 })) nodedown = curnode;
                EditorGUILayout.LabelField(node.Value, GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent(node.Value)).x));
                GUILayout.EndHorizontal();
                node = node.Next;
            }
            if (nodeup != null)
            {
                var nodep = nodeup.Previous;
                if (nodep != null)
                {
                    DistributeFlagOrder.Remove(nodeup);
                    DistributeFlagOrder.AddBefore(nodep, nodeup);
                }
            }
            if (nodedown != null)
            {
                var noden = nodedown.Next;
                if (noden != null)
                {
                    DistributeFlagOrder.Remove(nodedown);
                    DistributeFlagOrder.AddAfter(noden, nodedown);
                }
            }
            if (noderemove != null)
            {
                SelectDistributeFlag(noderemove.Value, false);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        public void SelectDistributeFlag(string flag, bool sel)
        {
            var old = false;
            if (DistributeFlags.TryGetValue(flag, out old))
            {
                DistributeFlags[flag] = sel;
                if (old && !sel)
                {
                    DistributeFlagOrder.Remove(flag);
                }
                else if (!old && sel)
                {
                    DistributeFlagOrder.AddLast(flag);
                }
            }
        }

        public void SaveDistributeFlags()
        {
            var old = ResManager.PreRuntimeDFlags;
            bool changed = old.Count != DistributeFlagOrder.Count;
            if (!changed)
            {
                int index = 0;
                foreach (var flag in DistributeFlagOrder)
                {
                    if (old[index++] != flag)
                    {
                        changed = true;
                        break;
                    }
                }
            }
            if (changed)
            {
                AssetDatabase.SaveAssets();
                System.Text.StringBuilder sbflags = new System.Text.StringBuilder();
                foreach (var oflag in DistributeFlagOrder)
                {
                    sbflags.Append("<");
                    sbflags.Append(oflag);
                }
                var path = CapsDistributeEditor.GetDistributeFlagsFilePath();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, sbflags.ToString());
                AssetDatabase.ImportAsset(CapsModEditor.GetAssetNameFromPath(path));

                ResManager.PreRuntimeDFlags = new List<string>(DistributeFlagOrder);
                CapsDistributeEditor.FireOnDistributeFlagsChanged();
            }
        }

        [SerializeField] protected string[] SerializeDistributeFlags;
        [SerializeField] protected bool[] SerializeDistributeFlagStates;
        [SerializeField] string[] SerializeDistributeFlagOrder;
        public void OnBeforeSerialize()
        {
            SerializeDistributeFlags = DistributeFlags.Keys.ToArray();
            SerializeDistributeFlagStates = DistributeFlags.Values.ToArray();
            SerializeDistributeFlagOrder = DistributeFlagOrder.ToArray();
        }

        public void OnAfterDeserialize()
        {
            DistributeFlags.Clear();
            DistributeFlagOrder.Clear();
            if (SerializeDistributeFlags != null && SerializeDistributeFlagStates != null)
            {
                for (int i = 0; i < SerializeDistributeFlags.Length && i < SerializeDistributeFlagStates.Length; ++i)
                {
                    DistributeFlags[SerializeDistributeFlags[i]] = SerializeDistributeFlagStates[i];
                }
            }
            if (SerializeDistributeFlagOrder != null)
            {
                for (int i = 0; i < SerializeDistributeFlagOrder.Length; ++i)
                {
                    DistributeFlagOrder.AddLast(SerializeDistributeFlagOrder[i]);
                }
            }
        }
    }
}