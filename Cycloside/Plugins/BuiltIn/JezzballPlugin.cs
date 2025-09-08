using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// The Jezzball plugin, now refactored to host a MonoGame instance.
    /// The actual game logic is now in the JezzballGame class.
    /// </summary>
    public class JezzballPlugin : IPlugin
    {
        private Window? _window;
        private JezzballGame? _game;
        private System.Threading.CancellationTokenSource? _cancellationTokenSource;

        public string Name => "Jezzball";
        public string Description => "A fresh Jezzball clone based on the Canvas project";
        public Version Version => new(2, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            // Create the game instance
            _game = new JezzballGame();
            _cancellationTokenSource = new System.Threading.CancellationTokenSource();

            _window = new Window
            {
                Title = "Jezzball",
                Width = 800,
                Height = 630,
                CanResize = true,
                MinWidth = 640,
                MinHeight = 480,
                Content = new TextBlock 
                { 
                    Text = "Jezzball Game Loading...", 
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };

            _window.KeyDown += OnWindowKeyDown;
            _window.Show();
            
            // FIXED: Run the game in a separate thread with cancellation support
            System.Threading.Tasks.Task.Run(() => 
            {
                try
                {
                    _game.Run();
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping the game
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Jezzball game error: {ex.Message}");
                }
            }, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            // FIXED: Improved disposal performance with timeout and better cleanup
            try
            {
                // Cancel the game thread first
                _cancellationTokenSource?.Cancel();
                
                // Give the game a short time to exit gracefully
                if (_game != null)
                {
                    _game.Exit();
                    
                    // Wait briefly for graceful shutdown, then force dispose
                    var disposeTask = Task.Run(() =>
                    {
                        try
                        {
                            _game.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error disposing JezzballGame: {ex.Message}");
                        }
                    });
                    
                    // Don't wait too long - dispose in background if needed
                    if (!disposeTask.Wait(1000)) // 1 second timeout
                    {
                        System.Diagnostics.Debug.WriteLine("JezzballGame disposal timed out, continuing cleanup");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during Jezzball cleanup: {ex.Message}");
            }
            finally
            {
                // Always clean up UI and references
                if (_window != null)
                {
                    try
                    {
                        _window.KeyDown -= OnWindowKeyDown;
                        _window.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error closing Jezzball window: {ex.Message}");
                    }
                }
                
                // Dispose cancellation token
                try
                {
                    _cancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing cancellation token: {ex.Message}");
                }
                
                // Clear references
                _window = null;
                _game = null;
                _cancellationTokenSource = null;
            }
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.R:
                    _game?.RestartGame();
                    e.Handled = true;
                    break;
                case Key.Space:
                    _game?.TogglePause();
                    e.Handled = true;
                    break;
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        private void ShowHelp()
        {
            var helpWindow = new Window
            {
                Title = "Jezzball Help",
                Width = 500,
                Height = 400,
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = @"Jezzball Help

Goal: Build walls to capture at least 75% of the area while avoiding balls.

Controls:
- Left Click: Place a wall
- Right Click: Change wall direction (Horizontal/Vertical)
- R: Restart game
- Space: Pause/Unpause
- F1: Show this help

Rules:
- Click to create wall segments that grow in both directions
- If a ball hits a wall while it's growing, you lose a life
- Enclosed areas without balls count toward your score
- Clear 75% of the area to advance to the next level
- Each level adds more balls
- Game ends when you run out of lives

Tips:
- Plan your walls carefully
- Watch ball trajectories
- Use the grid to help with placement
- Don't place walls too close to balls",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(20)
                    }
                }
            };
            helpWindow.Show();
        }
    }
}