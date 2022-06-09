using System.Collections.Generic;

namespace Assets.Editor.ResourceMap
{
    public interface IDependencyInfo
    {
        void Clear();
        void Load();
        List<string> GetChildren(string path);
        List<string> GetParent(string path);
    }
}
