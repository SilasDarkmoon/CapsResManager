using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Capstones.UnityEditorEx
{
    [InitializeOnLoad]
    public static partial class CapsEditorInitializer
    {
        public static void ShouldAlreadyInit() { }

        private class CapsEditorInitializer_ResManager
        {
            public CapsEditorInitializer_ResManager()
            {
                ResManagerEditorEntry.ShouldAlreadyInit();
                CapsModEditor.ShouldAlreadyInit();

                CapsPackageEditor.OnPackagesChanged += OnPackagesChanged;
                CapsDistributeEditor.OnDistributeFlagsChanged += OnDistributeFlagsChanged;
                //CapsDistributeEditor.OnDistributeFlagsChanged += CapsModEditor.CheckModsVisibility;
                //CapsDistributeEditor.OnDistributeFlagsChanged += UnityEngineEx.ResManager.RebuildRuntimeResCache;
            }

            private static void OnPackagesChanged()
            {
                CapsDistributeEditor.CheckDefaultSelectedDistributeFlags();
                CapsModEditor.CheckModsAndMakeLink();
                UnityEngineEx.ResManager.RebuildRuntimeResCache();
            }
            [UnityEngineEx.EventOrder(-100)]
            private static void OnDistributeFlagsChanged()
            {
                CapsModEditor.CheckModsVisibility();
                UnityEngineEx.ResManager.RebuildRuntimeResCache();
            }
        }
#pragma warning disable 0414
        private static CapsEditorInitializer_ResManager i_CapsEditorInitializer_ResManager = new CapsEditorInitializer_ResManager();
#pragma warning restore
    }
}