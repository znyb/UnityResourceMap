using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

namespace Assets.Editor.ResourceMap
{
    public class MapNode
    {
        public string Path;
        public int Id;
        public string Name;
        public GUIContent GUIContent;

        public Rect Rect;

        public List<MapNode> Childrens = new List<MapNode>();
        public List<MapNode> Parents = new List<MapNode>();
    }

    public class ResourceMapWindow : EditorWindow
    {

        [MenuItem("Window/ResourceMap")]
        static void OpenResourceMapWindow()
        {
            EditorWindow.GetWindow<ResourceMapWindow>();
        }

        MapNode currentNode;
        Dictionary<int, MapNode> NodeDict = new Dictionary<int, MapNode>();

        IDependencyMap Map;

        string currentPath;

        bool recursive = true;
        bool draggable = false;
        bool miniMap = true;
        bool locked = false;

        bool mapCreated = false;
        float mapWidth;
        float mapHeight;

        Vector2 scrollPos;
        int selectMap;
        string[] mapNames;

        Texture2D currentDotImage;
        Texture2D childrenDotImage;
        Texture2D parentDotImage;

        Texture2D childrenArrow;
        Texture2D parentArrow;
        Material s_HandleWireMaterial2D;

        List<IDependencyMap> allMaps;
        public List<IDependencyMap> AllMaps
        {
            get
            {
                if (allMaps == null)
                {
                    allMaps = new List<IDependencyMap>();
                    var type = typeof(IDependencyMap);
                    foreach (var t in type.Assembly.GetTypes())
                    {
                        if (!t.IsInterface && type.IsAssignableFrom(t))
                        {
                            allMaps.Add(Activator.CreateInstance(t) as IDependencyMap);
                        }
                    }
                    allMaps.Sort((map1, map2) => map1.Priority.CompareTo(map2.Priority));
                }
                return allMaps;
            }
        }

        public void OnEnable()
        {
            parentDotImage = EditorGUIUtility.Load("redLight") as Texture2D;
            currentDotImage = EditorGUIUtility.Load("greenLight") as Texture2D;
            childrenDotImage = EditorGUIUtility.Load("orangeLight") as Texture2D;

            parentArrow = EditorGUIUtility.Load("vcs_change") as Texture2D;
            childrenArrow = EditorGUIUtility.Load("vcs_incoming") as Texture2D;

            titleContent = new GUIContent("ResourceMap", EditorGUIUtility.Load("BlendTree Icon") as Texture2D);


            s_HandleWireMaterial2D = (Material)EditorGUIUtility.LoadRequired("SceneView/2DHandleLines.mat");
            //Debug.LogError(currentImage);
            
            mapNames = AllMaps.Select(map=>map.Name).ToArray();
            if (selectMap >= AllMaps.Count)
                selectMap = 0;
            Map = AllMaps[selectMap];

            if (string.IsNullOrEmpty(currentPath))
                OnSelectionChange();
            else
                Refresh();
        }

        public void OnGUI()
        {
            if (currentNode == null)
                return;

            DrawGrid();

            DrawToolBar();

            CreateMap();

            DrawMap();

            Event e = Event.current;
            if (e.type == EventType.MouseDrag && (e.button == 1 || e.button == 2))
            {
                scrollPos -= e.delta;
                scrollPos.x = Mathf.Max(scrollPos.x, 0);
                scrollPos.y = Mathf.Max(scrollPos.y, 0);
                Repaint();
            }
        }

        void DrawToolBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int lastSelect = selectMap;
                selectMap = EditorGUILayout.Popup(selectMap, mapNames, EditorStyles.toolbarPopup, GUILayout.Width(100));
                if(lastSelect != selectMap)
                {
                    Map = AllMaps[selectMap];
                    CreateNode();
                }

                if (GUILayout.Button("Load All", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    Map.DependencyInfo.Load(); 
                    CreateNode();
                }

                GUILayout.Space(4);

                bool r = recursive;
                recursive = GUILayout.Toggle(recursive, "Recursive", EditorStyles.toolbarButton, GUILayout.Width(100));
                if (r != recursive)
                {
                    CreateNode();
                }

                GUILayout.Space(4);

                draggable = GUILayout.Toggle(draggable, "Draggable", EditorStyles.toolbarButton, GUILayout.Width(100));

                GUILayout.Space(4);

                miniMap = GUILayout.Toggle(miniMap, "MiniMap", EditorStyles.toolbarButton, GUILayout.Width(100));

                GUILayout.Space(4);

                locked = GUILayout.Toggle(locked, "Lock", EditorStyles.toolbarButton, GUILayout.Width(100));

                GUILayout.FlexibleSpace();
            }
        }

        Vector2 miniMapSize = new Vector2(300, 300);
        float miniMapBorder = 20;
        void DrawMiniMap()
        {
            if (!miniMap)
                return;
            
            GUI.Window(1, new Rect(scrollPos.x + position.width - miniMapSize.x - 10, scrollPos.y + 10, miniMapSize.x, miniMapSize.y), DrawMiniMap, "MiniMap");
            GUI.BringWindowToFront(1);
        }

        void DrawMiniMap(int id)
        {
            var miniSize = miniMapSize - Vector2.one * miniMapBorder * 2;
            var mapSize = new Vector2(mapWidth > position.width ? mapWidth : position.width, mapHeight > position.height ? mapHeight : position.height);
            var region = new Rect(scrollPos.x / mapSize.x * miniSize.x + miniMapBorder,
                    scrollPos.y / mapSize.y * miniSize.y + miniMapBorder,
                    position.width / mapSize.x * miniSize.x,
                    position.height / mapSize.y * miniSize.y);
            GUI.Box(region, "");

            float size = nodeHeight / mapSize.y * miniSize.y;
            size = Mathf.Clamp(size, 3, 15);
            //Debug.Log(size);
            foreach (var pair in NodeDict)
            {
                var rect = new Rect(pair.Value.Rect.x * miniSize.x / mapSize.x + miniMapBorder,
                    pair.Value.Rect.y * miniSize.y / mapSize.y + miniMapBorder,
                    size, size);
                if (pair.Value == currentNode)
                    GUI.DrawTexture(rect, currentDotImage);
                else if (pair.Value.Rect.x < currentNode.Rect.x)
                    GUI.DrawTexture(rect, parentDotImage);
                else
                    GUI.DrawTexture(rect, childrenDotImage);

            }

            Event e = Event.current;
            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseUp) && e.button == 0)
            {
                scrollPos.x = Mathf.Min((e.mousePosition.x - miniMapBorder) * mapSize.x / miniSize.x - position.width / 2, mapSize.x - position.width);
                scrollPos.y = Mathf.Min((e.mousePosition.y - miniMapBorder) * mapSize.y / miniSize.y - position.height / 2, mapSize.y - position.height);
                //Debug.Log(e.mousePosition);
                e.Use();
                Repaint();
            }
        }

        void DrawMap()
        {
            var rect = new Rect(0, 0, mapWidth, mapHeight);
            var view = new Rect(0, 20, position.width, position.height - 20);
            scrollPos = GUI.BeginScrollView(view, scrollPos, rect, GUIStyle.none, GUIStyle.none);

            view.position = scrollPos;
            parentLines.Clear();
            childrenLines.Clear();
            DrawParentLine(currentNode, view);
            DrawChildrenLine(currentNode, view);

            BeginWindows();
            foreach (var pair in NodeDict)
            {
                var node = pair.Value;
                if (view.Overlaps(node.Rect))
                    node.Rect = GUI.Window(node.Id, node.Rect, DrawNode, node.GUIContent);
            }


            DrawMiniMap();
            EndWindows();

            GUI.EndScrollView(true);
        }

        List<int> parentLines = new List<int>();
        List<int> childrenLines = new List<int>();
        Vector2 arrowSize = new Vector2(15, 20);

        void DrawParentLine(MapNode node, Rect view)
        {
            if (parentLines.Contains(node.Id))
                return;

            bool visbale = view.Overlaps(node.Rect);
            parentLines.Add(node.Id);
            foreach (var n in node.Parents)
            {
                bool draw = visbale || view.Overlaps(n.Rect);
                if (draw)
                    Handles.DrawAAPolyLine(node.Rect.center, n.Rect.center);
                DrawParentLine(n, view);
                if (draw)
                {
                    var rot = Quaternion.FromToRotation(Vector2.up, n.Rect.center - node.Rect.center);
                    var arrowPos = (node.Rect.center + n.Rect.center) / 2 + (node.Rect.center - n.Rect.center) / 5 - (Vector2)(rot * (arrowSize / 2));
                    var mat = GUI.matrix;
                    //Debug.Log(Quaternion.FromToRotation(Vector2.up, n.Rect.center - node.Rect.center).eulerAngles);
                    GUIUtility.RotateAroundPivot(rot.eulerAngles.z, arrowPos);
                    GUI.DrawTexture(new Rect(arrowPos, arrowSize), parentArrow, ScaleMode.StretchToFill);
                    GUI.matrix = mat;
                }
            }
        }

        void DrawChildrenLine(MapNode node, Rect view)
        {
            if (childrenLines.Contains(node.Id))
                return;

            bool visbale = view.Overlaps(node.Rect);
            childrenLines.Add(node.Id);
            foreach (var n in node.Childrens)
            {
                bool draw = visbale || view.Overlaps(n.Rect);
                if (draw)
                    Handles.DrawAAPolyLine(node.Rect.center, n.Rect.center);
                DrawChildrenLine(n, view);

                if (draw)
                {
                    var rot = Quaternion.FromToRotation(Vector2.up, node.Rect.center - n.Rect.center);
                    var arrowPos = (node.Rect.center + n.Rect.center) / 2 + (n.Rect.center - node.Rect.center) / 8 - (Vector2)(rot * (arrowSize / 2));
                    var mat = GUI.matrix;
                    //Debug.Log(Quaternion.FromToRotation(Vector2.up, n.Rect.center - node.Rect.center).eulerAngles);
                    GUIUtility.RotateAroundPivot(rot.eulerAngles.z, arrowPos);
                    GUI.DrawTexture(new Rect(arrowPos, arrowSize), childrenArrow, ScaleMode.StretchToFill);
                    GUI.matrix = mat;
                }
            }
        }

        void DrawNode(int id)
        {
            var node = NodeDict[id];
            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(node.Path));
            }
            if (draggable)
                GUI.DragWindow();
        }



        void OnSelectionChange()
        {
            if (Selection.activeObject == null || !AssetDatabase.IsMainAsset(Selection.activeObject))
            {
                //Repaint();
                return;
            }

            if (locked && currentNode != null)
                return;

            currentPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            Refresh();
        }

        void Refresh()
        {
            if (string.IsNullOrEmpty(currentPath))
                return;

            currentNode = new MapNode();
            Map.InitNode(currentNode, currentPath);

            CreateNode();
        }

        void CreateNode()
        {
            if (currentNode == null)
                return;

            mapCreated = false;
            currentNode.Childrens.Clear();
            currentNode.Parents.Clear();
            currentNode.Rect = new Rect();
            NodeDict.Clear();
            NodeDict.Add(currentNode.Id, currentNode);

            CreateChildrenNode(currentNode);
            CreateParentNode(currentNode);

            Repaint();
        }

        void CreateChildrenNode(MapNode node)
        {
            foreach (var p in Map.DependencyInfo.GetChildren(node.Path))
            {
                int id = p.GetHashCode();
                if (NodeDict.ContainsKey(id))
                {
                    node.Childrens.Add(NodeDict[id]);
                }
                else
                {
                    var n = new MapNode();
                    Map.InitNode(n, p);
                    NodeDict.Add(id, n);
                    node.Childrens.Add(NodeDict[id]);
                    if (recursive)
                        CreateChildrenNode(n);
                }
            }
        }

        void CreateParentNode(MapNode node)
        {
            foreach (var p in Map.DependencyInfo.GetParent(node.Path))
            {
                int id = p.GetHashCode();
                if (NodeDict.ContainsKey(id))
                {
                    node.Parents.Add(NodeDict[id]);
                }
                else
                {
                    var n = new MapNode();
                    Map.InitNode(n, p);
                    NodeDict.Add(id, n);
                    node.Parents.Add(NodeDict[id]);
                    if (recursive)
                        CreateParentNode(n);
                }
            }
        }

        Vector2 nodeSize = new Vector2(300, 50);
        int nodeHeight = 40;
        List<int> parentNodePos = new List<int>();
        List<int> childrenNodePos = new List<int>();

        void CreateMap()
        {
            if (mapCreated)
                return;

            parentNodePos.Clear();
            childrenNodePos.Clear();
            int parentDepth = 0;
            float parentY = nodeSize.y;
            var parentPos = CalcParentRect(currentNode, ref parentY, 0, ref parentDepth);
            int childrenDepth = parentDepth + 1;
            float childrenY = nodeSize.y;
            var childrenPos = parentPos;
            if (currentNode.Childrens.Count > 0)
            {
                parentNodePos.Remove(currentNode.Id);
                childrenPos = CalcChildrenRect(currentNode, ref childrenY, childrenDepth, ref childrenDepth);
            }

            float offsetY = Mathf.Max(0, childrenPos.y - parentPos.y);
            Vector2 parentOffset = new Vector2(parentDepth * nodeSize.x + nodeSize.x, offsetY);
            foreach (var id in parentNodePos)
            {
                NodeDict[id].Rect.position += parentOffset;
            }

            mapWidth = childrenDepth * nodeSize.x + 2 * nodeSize.x;
            mapHeight = Mathf.Max(parentY + offsetY, childrenY) + 2 * nodeSize.y;
            Vector2 offset = Vector2.zero;
            if (mapWidth < position.width)
            {
                offset.x = (position.width - mapWidth) / 2;
                mapWidth = position.width;
            }
            if (mapHeight < position.height)
            {
                offset.y = (position.height - mapHeight) / 2;
                mapHeight = position.height;
            }

            if (offset != Vector2.zero)
            {
                foreach (var pair in NodeDict)
                {
                    pair.Value.Rect.position += offset;
                }
            }

            //Debug.Log(mapWidth);
            //Debug.Log(mapHeight);
            scrollPos = currentNode.Rect.center - position.size / 2;
            mapCreated = true;
        }

        Vector2 CalcParentRect(MapNode node, ref float y, int depth, ref int parentDepth)
        {
            float x = depth * -nodeSize.x;
            parentNodePos.Add(node.Id);

            if (depth > parentDepth)
                parentDepth = depth;

            Map.CalcNodeSize(node);
            if (node.Parents.Count == 0)
            {
                var pos = new Vector2(x, y);
                node.Rect.position = pos;
                y += nodeSize.y;
                return pos;
            }
            else
            {
                float yy = 0f;
                int count = 0;
                foreach (var p in node.Parents)
                {
                    if (!parentNodePos.Contains(p.Id))
                    {
                        yy += CalcParentRect(p, ref y, depth + 1, ref parentDepth).y;
                        count++;
                    }
                }
                if (count == 0)
                {
                    yy = y;
                    count = 1;
                    y += nodeSize.y;
                }
                var pos = new Vector2(x, yy / count);
                node.Rect.position = pos;
                return pos;
            }
        }

        Vector2 CalcChildrenRect(MapNode node, ref float y, int depth, ref int childrenDepth)
        {
            float x = depth * nodeSize.x;
            childrenNodePos.Add(node.Id);

            if (depth > childrenDepth)
                childrenDepth = depth;

            Map.CalcNodeSize(node);
            if (node.Childrens.Count == 0)
            {
                var pos = new Vector2(x, y);
                if (!parentNodePos.Contains(node.Id))
                {
                    node.Rect.position = pos;
                    y += nodeSize.y;
                }
                return pos;
            }
            else
            {
                float yy = 0f;
                int count = 0;
                foreach (var p in node.Childrens)
                {
                    if (!childrenNodePos.Contains(p.Id))
                    {
                        yy += CalcChildrenRect(p, ref y, depth + 1, ref childrenDepth).y;
                        count++;
                    }
                }
                if (count == 0)
                {
                    yy = y;
                    count = 1;
                    y += nodeSize.y;
                }
                var pos = new Vector2(x, yy / count);
                if (!parentNodePos.Contains(node.Id))
                    node.Rect.position = pos;
                return pos;
            }
        }

        private void DrawGrid()
        {
            if (Event.current.type == EventType.Repaint)
            {
                s_HandleWireMaterial2D.SetPass(0);
                GL.PushMatrix();
                GL.Begin(GL.LINES);
                DrawGridLines(12f, new Color(0f, 0f, 0f, 0.18f));
                DrawGridLines(120f, new Color(0f, 0f, 0f, 0.28f));
                GL.End();
                GL.PopMatrix();
            }
        }

        private void DrawGridLines(float gridSize, Color gridColor)
        {
            GL.Color(gridColor);
            for (float num = 0; num < position.width; num += gridSize)
            {
                DrawLine(new Vector2(num, 0), new Vector2(num, position.height));
            }
            GL.Color(gridColor);
            for (float num2 = 0; num2 < position.height; num2 += gridSize)
            {
                DrawLine(new Vector2(0, num2), new Vector2(position.width, num2));
            }
        }

        private void DrawLine(Vector2 p1, Vector2 p2)
        {
            GL.Vertex(p1);
            GL.Vertex(p2);
        }
    }
}
