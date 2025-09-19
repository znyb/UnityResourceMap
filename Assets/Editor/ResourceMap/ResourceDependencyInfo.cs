using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace Assets.Editor.ResourceMap
{
    public class UStringList : List<string> { }

    public abstract class ResourceDependencyInfo : ScriptableObject, ISerializationCallbackReceiver, IDependencyInfo
    {
        public Dictionary<string, UStringList> Childrens = new Dictionary<string, UStringList>();
        public Dictionary<string, UStringList> Parents = new Dictionary<string, UStringList>();
        public Dictionary<string, Hash128> DependencyHashDict = new Dictionary<string, Hash128>();

        [SerializeField]
        protected List<int> ChildrenPaths = new List<int>();
        [SerializeField]
        protected List<int> ChildrenDepends = new List<int>();
        [SerializeField]
        protected List<int> ParentDepends = new List<int>();
        [SerializeField]
        protected List<int> ParentPaths = new List<int>();
        [SerializeField]
        protected List<string> Paths = new List<string>();
        [SerializeField]
        protected List<string> DependencyHashs = new List<string>();

        public abstract List<string> GetChildren(string path);

        public abstract List<string> GetParent(string path);

        public abstract void Clear();

        public abstract void Load();

        public virtual void OnAfterDeserialize()
        {
            //Debug.LogError("OnAfterDeserialize Parents.Count:" + ParentPaths.Count);
            //Debug.LogError("OnAfterDeserialize Childrens.Count:" + ChildrenPaths.Count);
            Childrens.Clear();
            Parents.Clear();
            DependencyHashDict.Clear();
            Dictionary<int, string> dict = new Dictionary<int, string>();
            int pathCount = DependencyHashs.Count;
            for (int i = 0; i < pathCount; i++)
            {
                dict.Add(i, Paths[i]);
                DependencyHashDict.Add(Paths[i], Hash128.Parse(DependencyHashs[i]));
            }
            int childrenCount = ChildrenPaths.Count;
            int index = 0;
            for (int i = 0; i < childrenCount; i++)
            {
                UStringList list = new UStringList();
                for (; ChildrenDepends[index] >= 0; index++)
                    list.Add(dict[ChildrenDepends[index]]);
                index++;
                Childrens.Add(dict[ChildrenPaths[i]], list);
            }
            Debug.Assert(index == ChildrenDepends.Count);
            int parnetCount = ParentPaths.Count;
            index = 0;
            for (int i = 0; i < parnetCount; i++)
            {
                UStringList list = new UStringList();
                for (; ParentDepends[index] >= 0; index++)
                    list.Add(dict[ParentDepends[index]]);
                index++;
                Parents.Add(dict[ParentPaths[i]], list);
            }
            Debug.Assert(index == ParentDepends.Count);
        }

        public virtual void OnBeforeSerialize()
        {
            //Debug.LogError("OnBeforeSerialize Parents.Count:" + Parents.Count); 
            //Debug.LogError("OnBeforeSerialize Childrens.Count:" + Childrens.Count); 
            ChildrenPaths.Clear();
            ChildrenDepends.Clear();
            ParentPaths.Clear();
            ParentDepends.Clear();
            Paths.Clear();
            DependencyHashs.Clear();
            Dictionary<string, int> dict = new Dictionary<string, int>(DependencyHashs.Count);
            int i = 0;
            foreach (var pair in DependencyHashDict)
            {
                Paths.Add(pair.Key);
                DependencyHashs.Add(pair.Value.ToString());
                dict.Add(pair.Key, i);
                i++;
            }
            foreach (var pair in Childrens)
            {
                ChildrenPaths.Add(dict[pair.Key]);
                ChildrenDepends.AddRange(pair.Value.Select(p=>dict[p]));
                ChildrenDepends.Add(-1);
            }
            foreach (var pair in Parents)
            {
                ParentPaths.Add(dict[pair.Key]);
                ParentDepends.AddRange(pair.Value.Select(p=>dict[p]));
                ParentDepends.Add(-1);
            }
        }
    }
}
