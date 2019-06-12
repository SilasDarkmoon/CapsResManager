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
            }

            private static void OnPackagesChanged()
            {
                CapsDistributeEditor.CheckDefaultSelectedDistributeFlags();
                CapsModEditor.CheckModsAndMakeLink();
            }
            private static void OnDistributeFlagsChanged()
            {
                CapsModEditor.CheckModsVisibility();
            }
        }
#pragma warning disable 0414
        private static CapsEditorInitializer_ResManager i_CapsEditorInitializer_ResManager = new CapsEditorInitializer_ResManager();
#pragma warning restore

        [MenuItem("Res/Check Build", priority = 200105)]
        public static void CheckBuildCommand()
        {
            CapsResBuilderChecker.CheckRes("EditorOutput/Intermediate/ResBuildCheckResult.txt");
        }

        [MenuItem("Res/Build Res (No Update)", priority = 200110)]
        public static void BuildResCommand()
        {
            CapsResBuilder.BuildingParams = CapsResBuilder.ResBuilderParams.Create();
            CapsResBuilder.BuildingParams.makezip = false;
            var work = CapsResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            CapsResBuilder.BuildingParams = null;
        }
    }
}