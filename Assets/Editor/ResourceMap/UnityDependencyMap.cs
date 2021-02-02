using UnityEngine;
using UnityEditor;
using System.IO;

namespace Assets.Editor.ResourceMap
{
    public class UnityDependencyMap : IDependencyMap
    {
        UnityDependencyInfo UnityDependencyInfo;

        public string Name
        {
            get
            {
                return "Unity";
            }
        }

        public int Priority
        {
            get
            {
                return 100;
            }
        }

        public IDependencyInfo DependencyInfo
        {
            get
            {
                if(UnityDependencyInfo == null)
                {
                    var info = ScriptableObject.CreateInstance<UnityDependencyInfo>();
                    var script = MonoScript.FromScriptableObject(info);
                    string path = AssetDatabase.GetAssetPath(script);
                    var dir = Path.GetDirectoryName(path);
                    path = Path.Combine(dir, "Config/UnityDependencyInfo.asset");
                    if(File.Exists(path))
                    {
                        UnityDependencyInfo = AssetDatabase.LoadAssetAtPath<UnityDependencyInfo>(path);
                        Object.DestroyImmediate(info);
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(info, path);
                        UnityDependencyInfo = info;
                    }
                }
                return UnityDependencyInfo;
            }
        }

        public void InitNode(MapNode node, string path)
        {
            node.Path = path;
            node.Id = node.Path.GetHashCode();
            node.Name = Path.GetFileName(node.Path);
            node.GUIContent = new GUIContent(node.Name, AssetDatabase.GetCachedIcon(node.Path));
        }

        public void CalcNodeSize(MapNode node)
        {
            node.Rect.size = new Vector2(GUI.skin.label.CalcSize(new GUIContent(node.Name)).x + 20, 40);
        }
    }
}
