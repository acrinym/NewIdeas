using Avalonia.Controls;
using Cycloside.Scene;

namespace Cycloside.Effects
{
    internal static class EffectTargetHelper
    {
        public static Window? GetWindow(ISceneTarget target) =>
            (target as WindowSceneAdapter)?.Window;
    }
}
