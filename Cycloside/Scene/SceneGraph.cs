using System.Collections.Generic;
using System.Linq;

namespace Cycloside.Scene
{
    /// <summary>
    /// Root scene graph. Manages hierarchy and render order.
    /// </summary>
    public class SceneGraph
    {
        public static SceneGraph Instance { get; } = new SceneGraph();

        private readonly SceneNode _root = new SceneNode { Layer = SceneNode.LayerDesktop };

        public SceneNode Root => _root;

        public void AddChild(SceneNode node) => _root.AddChild(node);

        public void RemoveChild(SceneNode node) => _root.RemoveChild(node);

        public IEnumerable<SceneNode> GetRenderOrder()
        {
            return Flatten(_root).OrderBy(n => n.Layer).ThenBy(n => n.ZIndex);
        }

        private static IEnumerable<SceneNode> Flatten(SceneNode node)
        {
            yield return node;
            foreach (var child in node.Children)
            {
                foreach (var n in Flatten(child))
                    yield return n;
            }
        }
    }
}
