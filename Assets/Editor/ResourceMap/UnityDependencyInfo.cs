using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.ResourceMap
{
    [PreferBinarySerialization]
    public class UnityDependencyInfo : ResourceDependencyInfo
    {
        public static UnityDependencyInfo _inst;
        public static UnityDependencyInfo inst
        {
            get
            {
                if (_inst == null)
                {
                    var info = CreateInstance<UnityDependencyInfo>();
                    var script = MonoScript.FromScriptableObject(info);
                    string path = AssetDatabase.GetAssetPath(script);
                    var dir = Path.GetDirectoryName(path);
                    path = Path.Combine(dir, "Config/UnityDependencyInfo.asset");
                    if (File.Exists(path))
                    {
                        _inst = AssetDatabase.LoadAssetAtPath<UnityDependencyInfo>(path);
                        DestroyImmediate(info);
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(info, path);
                        _inst = info;
                    }
                }
                return _inst;
            }
        }

        public override List<string> GetChildren(string path)
        {
            if (Childrens.ContainsKey(path))
                return Childrens[path];
            return AssetDatabase.GetDependencies(path, false).ToList();
        }

        public override List<string> GetParent(string path)
        {
            if (Parents.ContainsKey(path))
                return Parents[path];
            return new List<string>();
        }

        public override void Clear()
        {
            Childrens.Clear();
            Parents.Clear();
            DependencyHashDict.Clear();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }


        public override void Load()
        {
            EditorUtility.DisplayCancelableProgressBar("Hold on", "", 0);
            AssetDatabase.SaveAssets();
            string[] allAsset = AssetDatabase.GetAllAssetPaths();
            HashSet<string> removedPaths = new HashSet<string>(DependencyHashDict.Keys);
            removedPaths.ExceptWith(allAsset);
            Debug.Log(removedPaths.Count);
            foreach (var path in removedPaths)
            {
                if (Childrens.ContainsKey(path))
                {
                    foreach (var p in Childrens[path])
                    {
                        if (Parents.ContainsKey(p))
                            Parents[p].Remove(path);
                    }
                    Childrens.Remove(path);
                }
                if (Parents.ContainsKey(path))
                {
                    foreach (var p in Parents[path])
                    {
                        if (Childrens.ContainsKey(p))
                            Childrens[p].Remove(path);

                        DependencyHashDict.Remove(p);
                    }
                    Parents.Remove(path);
                }
                DependencyHashDict.Remove(path);
            }
            int count = allAsset.Length;
            for (int i = 0; i < count; i++)
            {
                if (i % 100 == 0)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Hold on", "GetDependencies " + i + "/" + count, (float)i / count))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                }
                string p = allAsset[i];
                var hash = AssetDatabase.GetAssetDependencyHash(p);
                if (!DependencyHashDict.ContainsKey(p) || DependencyHashDict[p] != hash)
                {
                    if(Childrens.ContainsKey(p))
                    {
                        foreach(var ps in Childrens[p])
                        {
                            if (Parents.ContainsKey(ps))
                                Parents[ps].Remove(p);
                        }
                    }
                    var dp = AssetDatabase.GetDependencies(p, false);
                    Childrens[p] = new UStringList();
                    Childrens[p].AddRange(dp);
                    Childrens[p].Remove(p);
                    foreach (var d in dp)
                    {
                        if (d == p)
                            continue;
                        if (!Parents.ContainsKey(d))
                            Parents.Add(d, new UStringList());
                        Parents[d].Remove(p);
                        Parents[d].Add(p);
                    }
                    DependencyHashDict[p] = hash;
                }
            }
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets(); 
            EditorUtility.ClearProgressBar();
        }
    }
}
