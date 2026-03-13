using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace Cycloside.Scene
{
    /// <summary>
    /// Scene graph node. Implements ISceneTarget for effect compatibility.
    /// </summary>
    public class SceneNode : ISceneTarget
    {
        public const int LayerDesktop = 0;
        public const int LayerPlugin = 100;
        public const int LayerDialog = 200;
        public const int LayerOverlay = 300;

        private PixelPoint _position;
        private double _opacity = 1.0;
        private bool _isVisible = true;
        private int _zIndex;
        private int _layer = LayerPlugin;
        private SceneNode? _parent;
        private readonly List<SceneNode> _children = new();

        public SceneNode? Parent => _parent;
        public IReadOnlyList<SceneNode> Children => _children;
        public int ZIndex { get => _zIndex; set => _zIndex = value; }
        public int Layer { get => _layer; set => _layer = value; }
        public IRenderTarget? RenderTarget { get; set; }

        public PixelRect Bounds
        {
            get
            {
                var size = new PixelSize(100, 100);
                return new PixelRect(_position, size);
            }
        }

        public PixelPoint Position { get => _position; set => _position = value; }
        public double Opacity { get => _opacity; set => _opacity = value; }
        public bool IsVisible => _isVisible;
        public Avalonia.Threading.IDispatcher? Dispatcher => Avalonia.Threading.Dispatcher.UIThread;

        public void AddChild(SceneNode node)
        {
            if (node._parent != null)
                node._parent.RemoveChild(node);
            node._parent = this;
            _children.Add(node);
        }

        public void RemoveChild(SceneNode node)
        {
            if (node._parent == this)
            {
                _children.Remove(node);
                node._parent = null;
            }
        }

        public SceneNode? FindNode(Func<SceneNode, bool> predicate)
        {
            if (predicate(this)) return this;
            foreach (var child in _children)
            {
                var found = child.FindNode(predicate);
                if (found != null) return found;
            }
            return null;
        }

        public void BringToFront() => _zIndex = int.MaxValue;
        public void SendToBack() => _zIndex = int.MinValue;
    }
}
