using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Assets.Editor.ResourceMap
{
    public class UStringList : List<string> { }

    public abstract class ResourceDependencyInfo : ScriptableObject, ISerializationCallbackReceiver, IDependencyInfo
    {
        public Dictionary<string, UStringList> Childrens = new Dictionary<string, UStringList>();
        public Dictionary<string, UStringList> Parents = new Dictionary<string, UStringList>();
        public Dictionary<string, DateTime> LastUpdateTimes = new Dictionary<string, DateTime>();

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
        protected List<long> UpdateTimes = new List<long>();

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
            LastUpdateTimes.Clear();
            Dictionary<int, string> dict = new Dictionary<int, string>();
            int pathCount = UpdateTimes.Count;
            for (int i = 0; i < pathCount; i++)
            {
                dict.Add(i, Paths[i]);
                LastUpdateTimes.Add(Paths[i], new DateTime(UpdateTimes[i]));
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
            UpdateTimes.Clear();
            Dictionary<string, int> dict = new Dictionary<string, int>(UpdateTimes.Count);
            int i = 0;
            foreach (var pair in LastUpdateTimes)
            {
                Paths.Add(pair.Key);
                UpdateTimes.Add(pair.Value.Ticks);
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
