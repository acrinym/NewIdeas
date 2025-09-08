using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Host class for MonoGame content that can be embedded in Avalonia UI
    /// </summary>
    public class MonoGameHost : IDisposable
    {
        private GraphicsDevice? _graphicsDevice;
        private SpriteBatch? _spriteBatch;
        private bool _disposed = false;

        public GraphicsDevice? GraphicsDevice => _graphicsDevice;
        public SpriteBatch? SpriteBatch => _spriteBatch;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public MonoGameHost(int width, int height)
        {
            Width = width;
            Height = height;
            InitializeGraphics();
        }

        private void InitializeGraphics()
        {
            // Create graphics device for rendering
            var graphicsAdapter = GraphicsAdapter.DefaultAdapter;
            var presentationParameters = new PresentationParameters
            {
                BackBufferWidth = Width,
                BackBufferHeight = Height,
                BackBufferFormat = SurfaceFormat.Color,
                DepthStencilFormat = DepthFormat.Depth24Stencil8,
                DeviceWindowHandle = IntPtr.Zero, // Will be set by host
                IsFullScreen = false,
                MultiSampleCount = 0,
                PresentationInterval = PresentInterval.One,
                RenderTargetUsage = RenderTargetUsage.DiscardContents
            };

            _graphicsDevice = new GraphicsDevice(graphicsAdapter, GraphicsProfile.Reach, presentationParameters);
            _spriteBatch = new SpriteBatch(_graphicsDevice);
        }

        public void BeginDraw()
        {
            _graphicsDevice?.Clear(Color.CornflowerBlue);
        }

        public void EndDraw()
        {
            _graphicsDevice?.Present();
        }

        public void Resize(int width, int height)
        {
            if (Width != width || Height != height)
            {
                Width = width;
                Height = height;
                
                // Recreate graphics device with new dimensions
                _graphicsDevice?.Dispose();
                _spriteBatch?.Dispose();
                InitializeGraphics();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _spriteBatch?.Dispose();
                    _graphicsDevice?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
