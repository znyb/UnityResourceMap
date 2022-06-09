using UnityEngine;
using UnityEditor;
using System.IO;

namespace Assets.Editor.ResourceMap
{
    public class UnityDependencyMap : IDependencyMap
    {

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
                return UnityDependencyInfo.inst;
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
