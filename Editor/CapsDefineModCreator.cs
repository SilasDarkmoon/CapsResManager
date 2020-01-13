﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    public class CapsDefineModCreator : EditorWindow
    {
        [MenuItem("Mods/Make Mod for Precompiler Define Symbol", priority = 200010)]
        static void Init()
        {
            var win = GetWindow<CapsDefineModCreator>();
            win.titleContent = new GUIContent("Type Symbol");
        }

        protected string _Symbol = "DEBUG_";
        void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Which precompiler symbol would you like to define?");
            _Symbol = GUILayout.TextField(_Symbol);
            if (GUILayout.Button("OK"))
            {
                if (string.IsNullOrEmpty(_Symbol))
                {
                    EditorUtility.DisplayDialog("Error", "Empty Symbol!", "OK");
                }
                else if (_Symbol.EndsWith("_"))
                {
                    EditorUtility.DisplayDialog("Error", "Symbol should not end with _", "OK");
                }
                else
                {
                    if (System.IO.Directory.Exists("Assets/Mods/" + _Symbol))
                    {
                        EditorUtility.DisplayDialog("Warning", "It seems that the mod has been already created.", "OK");
                    }
                    else
                    {
                        var descdir = "Assets/Mods/" + _Symbol + "/Resources";
                        System.IO.Directory.CreateDirectory(descdir);
                        AssetDatabase.ImportAsset(descdir);
                        var desc = ScriptableObject.CreateInstance<CapsModDesc>();
                        desc.Mod = _Symbol;
                        AssetDatabase.CreateAsset(desc, "Assets/Mods/" + _Symbol + "/Resources/resdesc.asset");
                        var sympath = "Assets/Mods/" + _Symbol + "/Link/mcs.rsp";
                        using (var sw = PlatDependant.OpenWriteText(sympath))
                        {
                            sw.Write("-define:");
                            sw.WriteLine(_Symbol);
                        }
                        CapsModEditor.CheckModsVisibility();
                    }
                    Close();
                }
            }
            GUILayout.EndVertical();
        }
    }
}
