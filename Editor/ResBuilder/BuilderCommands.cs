using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Capstones.UnityEditorEx
{
    public static class CapsResBuilderCommands
    {
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

        #region Test
        [MenuItem("Test/Res/Create Icon", priority = 500010)]
        public static void TestCreateIcon()
        {
            IconMaker.WriteTextToImage(Random.Range(0, 1000).ToString(), "EditorOutput/temp.png");
            System.IO.Directory.CreateDirectory("EditorOutput/testfolder");
            if (IconMaker.ChangeImageToIco("EditorOutput/temp.png", null))
            {
                IconMaker.SetFolderIcon("EditorOutput/testfolder", "EditorOutput/temp.ico");
            }
            else
            {
                IconMaker.SetFolderIcon("EditorOutput/testfolder", "EditorOutput/temp.png");
            }
        }
        #endregion
    }
}