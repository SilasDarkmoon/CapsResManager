using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Capstones.UnityEditorEx
{
    [InitializeOnLoad]
    public static partial class CapsEditorInitializer
    {
        private class CapsEditorInitializer_ResManager
        {
            public CapsEditorInitializer_ResManager()
            {
                CapsPackageEditor.OnPackagesChanged += () =>
                {
                    CapsDistributeEditor.CheckDefaultSelectedDistributeFlags();
                    CapsModEditor.CheckModsAndMakeLink();
                };
                CapsDistributeEditor.OnDistributeFlagsChanged += CapsModEditor.CheckModsVisibility;
            }
        }
#pragma warning disable 0414
        private static CapsEditorInitializer_ResManager i_CapsEditorInitializer_ResManager = new CapsEditorInitializer_ResManager();
#pragma warning restore

        [MenuItem("Test/Build Res")]
        public static void BuildResCommand()
        {
            var work = CapsResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
        }
        [MenuItem("Test/Build Scripts")]
        public static void BuildSptCommand()
        {
            //var work = CapsSptBuilder.BuildSptAsync(null, null, new[] { new CapsSptBuilder.SptBuilderEx_RawCopy() });
            //while (work.MoveNext()) ;
        }
        [MenuItem("Test/Check Build")]
        public static void CheckBuildCommand()
        {
            CapsResBuilderChecker.CheckRes("EditorOutput/Intermediate/ResBuildCheckResult.txt");
        }

        [MenuItem("Test/Test2")]
        public static void Test2Command()
        {
            Debug.Log(UnityEngineEx.IsolatedPrefs.GetString("pref1"));
            UnityEngineEx.IsolatedPrefs.SetString("pref1", "val1");
            UnityEngineEx.IsolatedPrefs.Save();
        }

        [MenuItem("Test/Test3")]
        public static void Test3Command()
        {
            //Debug.Log(UnityEngineEx.IsolatedPrefs.IsolatedID);
            //CapsPackageEditor.Test();

            //Debug.Log(CapsEditorUtils.__FILE__);
            //Debug.Log(CapsEditorUtils.__ASSET__);
            //Debug.Log(CapsEditorUtils.__LINE__);
            //Debug.Log(CapsEditorUtils.__MOD__);

            //foreach (var asset in AssetDatabase.GetAllAssetPaths())
            //{
            //    if (asset.StartsWith("Packages/"))
            //    {
            //        Debug.Log(asset);
            //    }
            //}

            Debug.Log(AssetDatabase.GUIDToAssetPath(AssetDatabase.AssetPathToGUID("packages/cn.capstones.resmanager/CapsRes/dist/testdist/new Material.mat")));
        }
    }
}