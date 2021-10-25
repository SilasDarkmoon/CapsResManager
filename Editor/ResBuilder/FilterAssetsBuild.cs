using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Capstones.UnityEngineEx;
using Capstones.UnityEditorEx;

namespace Capstones.UnityEditorEx
{
    public static class FilterAssetsBuild
    {
        static Dictionary<string, ArrayList> _filterAssets = new Dictionary<string, ArrayList>();
        static Dictionary<string, ArrayList> _variableAssets = new Dictionary<string, ArrayList>();
        public static void Init()
        {
            _filterAssets.Clear();
            _variableAssets.Clear();
            //【--res common】
            var list = new ArrayList();
            list.Add("m--d--common-fonts");
            list.Add("m--d--common-materials");
            list.Add("m--d--common-renderpipelineassets");
            list.Add("m--d--common-shaders");
            list.Add("m--d--common-textures");
            //【--package res Manager common】
            list.Add("m--d--common-intermediatescene.s");
            //【--package mvc common】
            list.Add("m--d--common-uiscene.s");
            _filterAssets.Add("m--d--common", list);

            //【--res entry】
            list = new ArrayList();
            list.Add("m--d--entry-dependencies");
            list.Add("m--d--entry-loadingscene.s");
            _filterAssets.Add("m--d--entry", list);

            //【--res game audio】
            list = new ArrayList();
            list.Add("m--d--game-audio");
            _filterAssets.Add("m--d--game-audio", list);

            //【--res game clothmaker】
            list = new ArrayList();
            list.Add("m--d--game-clothmaker");
            _filterAssets.Add("m--d--game-clothmaker", list);

            //【--res game config】
            list = new ArrayList();
            list.Add("m--d--game-config");
            _filterAssets.Add("m--d--game-config", list);

            //【--res game matchscenes】
            list = new ArrayList();
            list.Add("m--d--game-matchscenes");
            _filterAssets.Add("m--d--game-matchscenes", list);

            //【--res game models actor】
            list = new ArrayList();
            list.Add("m--d--game-models-actor-animationclip");
            _filterAssets.Add("m--d--game-models-actor-animationclip", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-camera");
            _filterAssets.Add("m--d--game-models-actor-camera", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-common");
            _filterAssets.Add("m--d--game-models-actor-common", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-costar");
            _filterAssets.Add("m--d--game-models-actor-costar", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-animator");
            _filterAssets.Add("m--d--game-models-actor-master-animator", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-animopt-cardopt");
            list.Add("m--d--game-models-actor-master-animopt-commonopt");
            list.Add("m--d--game-models-actor-master-animopt-createcharacteropt");
            list.Add("m--d--game-models-actor-master-animopt-g2p1opt");
            list.Add("m--d--game-models-actor-master-animopt-g2p2opt");
            //list.Add("m--d--game-models-actor-master-animopt-g2p3opt");
            //list.Add("m--d--game-models-actor-master-animopt-g2p4opt");
            //list.Add("m--d--game-models-actor-master-animopt-g2p5opt");
            list.Add("m--d--game-models-actor-master-animopt-guildjoinopt");
            list.Add("m--d--game-models-actor-master-animopt-newsconferenceopt");
            _filterAssets.Add("m--d--game-models-actor-master-animopt", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-avatarmask");
            _filterAssets.Add("m--d--game-models-actor-master-avatarmask", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-boy");
            _filterAssets.Add("m--d--game-models-actor-master-boy", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-materials");
            _filterAssets.Add("m--d--game-models-actor-master-materials", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-model");
            _filterAssets.Add("m--d--game-models-actor-master-model", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-n1");
            _filterAssets.Add("m--d--game-models-actor-master-n1", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-prefabs");
            _filterAssets.Add("m--d--game-models-actor-master-prefabs", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-master-simpleanim");
            _filterAssets.Add("m--d--game-models-actor-master-simpleanim", list);

            //【--res game models secretary m2】
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-character");
            _filterAssets.Add("m--d--game-models-actor-secretary-character", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-m2-animator");
            _filterAssets.Add("m--d--game-models-actor-secretary-m2-animator", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-m2-animopt-commonopt");
            list.Add("m--d--game-models-actor-secretary-m2-animopt-g2p1opt");
            list.Add("m--d--game-models-actor-secretary-m2-animopt-g2p2opt");
            //list.Add("m--d--game-models-actor-secretary-m2-animopt-g2p3opt");
            //list.Add("m--d--game-models-actor-secretary-m2-animopt-g2p4opt");
            //list.Add("m--d--game-models-actor-secretary-m2-animopt-g2p5opt");
            list.Add("m--d--game-models-actor-secretary-m2-animopt-guideopt");
            list.Add("m--d--game-models-actor-secretary-m2-animopt-homepageopt");
            list.Add("m--d--game-models-actor-secretary-m2-animopt-interactionopt");
            list.Add("m--d--game-models-actor-secretary-m2-animopt-teaopt");
            _filterAssets.Add("m--d--game-models-actor-secretary-m2-animopt", list);

            //【--res game models secretary m2】
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-m2-m2");
            _filterAssets.Add("m--d--game-models-actor-secretary-m2-m2", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-m2-prefabs");
            _filterAssets.Add("m--d--game-models-actor-secretary-m2-prefabs", list);

            //【--res game models secretary m4】
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-m4-animopt-interactionopt");
            list.Add("m--d--game-models-actor-secretary-m4-animopt-shouyejibanopt");
            list.Add("m--d--game-models-actor-secretary-m4-animopt-teaopt");
            _filterAssets.Add("m--d--game-models-actor-secretary-m4-animopt", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-m4-m4");
            _filterAssets.Add("m--d--game-models-actor-secretary-m4-m4", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-m4-prefabs");
            _filterAssets.Add("m--d--game-models-actor-secretary-m4-prefabs", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-secretary02-prefabs");
            _filterAssets.Add("m--d--game-models-actor-secretary-secretary02-prefabs", list);
            list = new ArrayList();
            list.Add("m--d--game-models-actor-secretary-secretary04-prefabs");
            _filterAssets.Add("m--d--game-models-actor-secretary-secretary04-prefabs", list);

            //【--res game model general】
            list = new ArrayList();
            list.Add("m--d--game-models-animdata");
            _filterAssets.Add("m--d--game-models-animdata", list);
            list = new ArrayList();
            list.Add("m--d--game-models-baseball");
            _filterAssets.Add("m--d--game-models-baseball", list);
            list = new ArrayList();
            list.Add("m--d--game-models-common");
            _filterAssets.Add("m--d--game-models-common", list);
            list = new ArrayList();
            list.Add("m--d--game-models-lights");
            _filterAssets.Add("m--d--game-models-lights", list);
            list = new ArrayList();
            list.Add("m--d--game-models-linerenderer");
            _filterAssets.Add("m--d--game-models-linerenderer", list);
            //list = new ArrayList();
            //list.Add("m--d--game-models-map");
            //_filterAssets.Add("m--d--game-models-map", list);
            list = new ArrayList();
            list.Add("m--d--game-models-player3dshow");
            _filterAssets.Add("m--d--game-models-player3dshow", list);
            list = new ArrayList();
            list.Add("m--d--game-models-players");
            _filterAssets.Add("m--d--game-models-players", list);
            list = new ArrayList();
            list.Add("m--d--game-models-qte");
            _filterAssets.Add("m--d--game-models-qte", list);
            list = new ArrayList();
            list.Add("m--d--game-models-shader");
            _filterAssets.Add("m--d--game-models-shader", list);
            list = new ArrayList();
            list.Add("m--d--game-models-uimodelview");
            _filterAssets.Add("m--d--game-models-uimodelview", list);
            //【--res game model scenes】
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-effects");
            _filterAssets.Add("m--d--game-models-scenes-effects", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-flag");
            _filterAssets.Add("m--d--game-models-scenes-flag", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-flares");
            _filterAssets.Add("m--d--game-models-scenes-flares", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-grass");
            _filterAssets.Add("m--d--game-models-scenes-grass", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-hks_stadium"); //* could be optimizable
            _filterAssets.Add("m--d--game-models-scenes-hks_stadium", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-mud");
            _filterAssets.Add("m--d--game-models-scenes-mud", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-news");
            _filterAssets.Add("m--d--game-models-scenes-news", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-playback");
            _filterAssets.Add("m--d--game-models-scenes-playback", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-postprocess");
            _filterAssets.Add("m--d--game-models-scenes-postprocess", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-precipiceeffect");
            _filterAssets.Add("m--d--game-models-scenes-precipiceeffect", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-skybox");
            _filterAssets.Add("m--d--game-models-scenes-skybox", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-spectators");
            _filterAssets.Add("m--d--game-models-scenes-spectators", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-techniquematch");
            _filterAssets.Add("m--d--game-models-scenes-techniquematch", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-training");
            _filterAssets.Add("m--d--game-models-scenes-training", list);
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-water");
            _filterAssets.Add("m--d--game-models-scenes-water", list);
            //【--res game model scenes story】
            list = new ArrayList();
            list.Add("m--d--game-models-scenes-story-common");
            list.Add("m--d--game-models-scenes-story-monologue");
            //list.Add("m--d--game-models-scenes-story-plotscene-beach");
            list.Add("m--d--game-models-scenes-story-plotscene-cafe");
            //list.Add("m--d--game-models-scenes-story-plotscene-hall");
            list.Add("m--d--game-models-scenes-story-plotscene-home");
            list.Add("m--d--game-models-scenes-story-plotscene-locker_room");
            list.Add("m--d--game-models-scenes-story-plotscene-park");
            list.Add("m--d--game-models-scenes-story-plotscene-training");
            list.Add("m--d--game-models-scenes-story-sceneprops");
            list.Add("m--d--game-models-scenes-story-themescene-animator");
            list.Add("m--d--game-models-scenes-story-themescene-beach");
            list.Add("m--d--game-models-scenes-story-themescene-cafe");
            list.Add("m--d--game-models-scenes-story-themescene-common");
            list.Add("m--d--game-models-scenes-story-themescene-home");
            list.Add("m--d--game-models-scenes-story-themescene-locker_room1");
            list.Add("m--d--game-models-scenes-story-themescene-park");
            list.Add("m--d--game-models-scenes-story-themescene-training");
            _filterAssets.Add("m--d--game-models-scenes-story", list);

            //【--res game ui】
            list = new ArrayList();
            list.Add("m--d--game-ui-common");
            _filterAssets.Add("m--d--game-ui-common", list);
            list = new ArrayList();
            list.Add("m--d--game-ui-coregame");
            _filterAssets.Add("m--d--game-ui-coregame", list);
            list = new ArrayList();
            list.Add("m--d--game-ui-scene");
            _filterAssets.Add("m--d--game-ui-scene", list);

            ////【--res dist】
            //list = new ArrayList();
            //list.Add("m--d-bodysizedev-body");
            //_filterAssets.Add("m--d-bodysizedev-body", list);


            //-----------------------------------------------
            //【--res dist variableAssets】
            list = new ArrayList();
            list.Add("m--d-{1}-entry");
            _variableAssets.Add("m--d-{1}-entry", list);
            list = new ArrayList();
            list.Add("m--d-{1}-game");
            _variableAssets.Add("m--d-{1}-game", list);
        }

        public static bool IsContainsAsset(string path)
        {
            foreach (KeyValuePair<string, ArrayList> asset in _filterAssets)
            {
                var key = asset.Key;

                if (path.IndexOf(key) > 0)
                {
                    var list = asset.Value;
                    for (int i = 0; i < list.Count; i++)
                    {
                        string value = list[i].ToString();
                        if (path.IndexOf(value) > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool IsValidAssets(string path, List<string> blackList)
        {
            var isValid = true;
            for (int i = 0; i < blackList.Count; ++i)
            {
                var blackItem = blackList[i];
                if (path.StartsWith(blackItem, StringComparison.InvariantCultureIgnoreCase))
                {
                    isValid = false;
                    break;
                }
            }
            return isValid;
        }

        [MenuItem("Res/Filter Split Apk", priority = 205050)]
        public static void FindFilterAssets()
        {
            List<string> blackList = CapsObbMaker.GetDistributeAssetsList("obb-except.txt");
            var filterfile = ResManager.EditorResLoader.CheckDistributePathSafe("~Config~/", "filter-asset.txt");
            if (PlatDependant.IsFileExist(filterfile))
            {
                System.IO.File.WriteAllText(@filterfile, string.Empty);
            }
            Init();

            var flag = "";
            var dflags = ResManager.GetValidDistributeFlags();
            for (int i = 0; i < dflags.Length; ++i)
            {
                if (filterfile.IndexOf(dflags[i]) > 0)
                {
                    flag = dflags[i].ToLower();
                    break;
                }
            }

            foreach (KeyValuePair<string, ArrayList> asset in _variableAssets)
            {
                var key = asset.Key;
                var list = asset.Value;
                key = string.Format(key, "{1}", flag);
                ArrayList arrayList = new ArrayList();
                for (int i = 0; i < list.Count; i++)
                {
                    string value = list[i].ToString();
                    value = string.Format(value, "{1}", flag);
                    arrayList.Add(value);
                }
                _filterAssets.Add(key, arrayList);
            }

            FileStream stream = new FileStream(filterfile, FileMode.OpenOrCreate);
            string[] arrStrResPath = Directory.GetFiles(Application.dataPath + "/StreamingAssets/res", "*b", SearchOption.AllDirectories);
            foreach (string strResPath in arrStrResPath)
            {
                string strTempPath = strResPath.Replace(@"\", "/");
                strTempPath = "res/" + strTempPath.Substring(strTempPath.IndexOf("res/") + 4);
                string splitTempPath = strTempPath + "\r\n";

                var isValid = IsValidAssets(strTempPath, blackList);

                if (isValid)
                {
                    var isContain = IsContainsAsset(strTempPath);
                    if (isContain)
                    {
                        byte[] data = Encoding.UTF8.GetBytes(splitTempPath);
                        stream.Write(data, 0, data.Length);
                    }
                }
            }
            string[] arrStrSptPath = Directory.GetFiles(Application.dataPath + "/StreamingAssets/spt", "*.lua", SearchOption.AllDirectories);
            foreach (string strSptPath in arrStrSptPath)
            {
                string strTempPath = strSptPath.Replace(@"\", "/");
                strTempPath = "spt/" + strTempPath.Substring(strTempPath.IndexOf("spt/") + 4);
                string splitTempPath = strTempPath + "\r\n";

                var isValid = IsValidAssets(strTempPath, blackList); ;
            
                if (isValid)
                {
                    byte[] data = Encoding.UTF8.GetBytes(splitTempPath);
                    stream.Write(data, 0, data.Length);
                }
            }

            stream.Flush();
            stream.Close();
            Debug.Log("RemainAssets Done!");
            List<string> filterList = CapsObbMaker.GetDistributeAssetsList("filter-asset.txt");
            var built = CapsObbMaker.MakeObbDelayedInFolder("Assets/StreamingAssets", "EditorOutput/Build/Latest/obb/", null, blackList, filterList, true);
            using (var sw = PlatDependant.OpenWriteText("Assets/StreamingAssets/hasobb.flag.txt"))
            {
                foreach (var key in built)
                {
                    sw.WriteLine(key);
                }
            }
            Debug.Log("MakeObb Done!");
        }
    }
}


