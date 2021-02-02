
namespace Assets.Editor.ResourceMap
{
    public interface IDependencyMap
    {
        string Name { get; }
        int Priority { get; }
        IDependencyInfo DependencyInfo { get; }
        void InitNode(MapNode node, string path);
        void CalcNodeSize(MapNode node);
    }
}
