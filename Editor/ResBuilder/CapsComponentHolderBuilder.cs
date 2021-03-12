﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Capstones.UnityEngineEx;

namespace Capstones.UnityEditorEx
{
    public class CapsComponentHolderBuilder : CapsResBuilder.IResBuilderEx
    {
        public void Cleanup()
        {
        }
        public bool CreateItem(CapsResManifestNode node)
        {
            return false;
        }
        public string FormatBundleName(string asset, string mod, string dist, string norm)
        {
            return null;
        }
        public void GenerateBuildWork(string bundleName, IList<string> assets, ref AssetBundleBuild abwork, CapsResBuilder.CapsResBuildWork modwork, int abindex)
        {
        }
        public void ModifyItem(CapsResManifestItem item)
        {
        }
        public void OnSuccess()
        {
        }

        public void Prepare(string output)
        {
        }

        [MenuItem("Res/Build Component Holder", priority = 202010)]
        public static void BuildComponentHolder()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);

            HashSet<Type> compTypes = new HashSet<Type>();
            compTypes.Add(typeof(Transform));
            var phpath = "Assets/Mods/" + CapsEditorUtils.__MOD__ + "/Resources/ComponentHolder.prefab";
            GameObject tgo = null;
            if (PlatDependant.IsFileExist(phpath))
            {
                var old = AssetDatabase.LoadMainAssetAtPath(phpath) as GameObject;
                if (old)
                {
                    tgo = GameObject.Instantiate(old);
                    var oldcomps = tgo.GetComponentsInChildren(typeof(Component), true);
                    for (int i = 0; i < oldcomps.Length; ++i)
                    {
                        var oldcomp = oldcomps[i];
                        if (oldcomp != null)
                        {
                            compTypes.Add(oldcomp.GetType());
                        }
                    }
                }
            }
            if (tgo == null)
            {
                tgo = new GameObject("ComponentHolder");
                tgo.SetActive(false);
            }

            var allassets = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < allassets.Length; ++i)
            {
                var asset = allassets[i];
                if (asset.EndsWith(".prefab", StringComparison.InvariantCultureIgnoreCase))
                {
                    var prefab = AssetDatabase.LoadMainAssetAtPath(asset) as GameObject;
                    if (prefab == null)
                    {
                        Debug.LogError("Cannot load " + asset);
                        continue;
                    }
                    var comps = prefab.GetComponentsInChildren(typeof(Component), true);
                    for (int j = 0; j < comps.Length; ++j)
                    {
                        var comp = comps[j];
                        if (comp == null)
                        {
                            Debug.LogError("Prefab has invalid component: " + asset);
                            continue;
                        }
                        var compt = comp.GetType();
                        if (compTypes.Add(compt))
                        {
                            // should add this comp to tgo.
                            var hgo = new GameObject(compt.Name);
                            hgo.transform.SetParent(tgo.transform, false);
                            AddComponentWithDep(hgo, compt, compTypes);
                        }
                    }
                }
                else if (asset.EndsWith(".unity", StringComparison.InvariantCultureIgnoreCase)
                    || asset.EndsWith(".u3d", StringComparison.InvariantCultureIgnoreCase))
                {
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(asset, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                    if (!scene.IsValid())
                    {
                        Debug.LogError("Cannot load " + asset);
                        continue;
                    }
                    var roots = scene.GetRootGameObjects();
                    for (int k = 0; k < roots.Length; ++k)
                    {
                        var root = roots[k];
                        var comps = root.GetComponentsInChildren(typeof(Component), true);
                        for (int j = 0; j < comps.Length; ++j)
                        {
                            var comp = comps[j];
                            if (comp == null)
                            {
                                Debug.LogError("Scene has invalid component: " + asset);
                                continue;
                            }
                            var compt = comp.GetType();
                            if (compTypes.Add(compt))
                            {
                                // should add this comp to tgo.
                                var hgo = new GameObject(compt.Name);
                                hgo.transform.SetParent(tgo.transform, false);
                                AddComponentWithDep(hgo, compt, compTypes);
                            }
                        }
                    }
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                }
            }
            PlatDependant.CreateFolder(System.IO.Path.GetDirectoryName(phpath));
            PlatDependant.DeleteFile(phpath);
            PrefabUtility.SaveAsPrefabAsset(tgo, phpath);
            GameObject.DestroyImmediate(tgo);
        }

        private static void SafeAddMissingComponent(GameObject go, Type t)
        {
            try
            {
                if (!go.GetComponent(t))
                {
                    go.AddComponent(t);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        private static void AddComponentWithDep(GameObject go, Type compt, HashSet<Type> compTypes)
        {
            var deps = compt.GetCustomAttributes(typeof(RequireComponent), true);
            if (deps != null)
            {
                for (int k = 0; k < deps.Length; ++k)
                {
                    var dep = deps[k] as RequireComponent;
                    if (dep != null)
                    {
                        var dep0 = dep.m_Type0;
                        if (dep0 != null && dep0.IsSubclassOf(typeof(Component)))
                        {
                            AddComponentWithDep(go, dep0, compTypes);
                        }
                        var dep1 = dep.m_Type1;
                        if (dep1 != null && dep1.IsSubclassOf(typeof(Component)))
                        {
                            AddComponentWithDep(go, dep1, compTypes);
                        }
                        var dep2 = dep.m_Type2;
                        if (dep2 != null && dep2.IsSubclassOf(typeof(Component)))
                        {
                            AddComponentWithDep(go, dep2, compTypes);
                        }
                    }
                }
            }
            SafeAddMissingComponent(go, compt);
            compTypes.Add(compt);
        }
    }

    [InitializeOnLoad]
    public static class CapsComponentHolderBuilderEntry
    {
        private static CapsComponentHolderBuilder _Builder = new CapsComponentHolderBuilder();
        static CapsComponentHolderBuilderEntry()
        {
            CapsResBuilder.ResBuilderEx.Add(_Builder);
        }
    }
}
