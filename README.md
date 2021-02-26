# UnityResourceMap
展示Unity资源的依赖关系图

## 使用
点击菜单Window/ResourceMap打开资源索引界面，在Project中选中想要查看索引关系的资源，将在界面中展示选中资源引用了哪些资源和哪些资源引用了选中的资源

如果是第一次打开界面或是项目中的资源依赖发生变更时，点击 Load All 更新项目中所有资源的索引信息（如果项目中资源过多，第一次可能比较慢）

#### 开关
Recursive

  如果关闭Recursive，将只展示当前选中资源直接引用的资源，如果开启，将递归展示所有间接引用的资源

Draggable

  是否能拖动各个资源节点

MiniMap

  开关小地图，小地图中绿色点为当前选中节点，黄色为选中资源引用的资源节点，红色为引用了选中资源的资源节点

Lock

  开启Lock后，将锁住当前索引图，不再随选中的资源改变而变化



![Screenshot](https://github.com/znyb/znyb.github.io/blob/master/Image/ResourceMap.png)
