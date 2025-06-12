# Custom Cursor Example

Cycloside lets each window or control specify a cursor. You can store the desired cursor type in `settings.json` under `ComponentCursors` and apply it via `CursorManager.ApplyFromSettings`.

```json
{
  "ComponentCursors": {
    "Plugins": "Cross",
    "Main": "Hand"
  }
}
```

Within code you can also set the cursor directly:

```csharp
using Avalonia.Input;
using Cycloside;

var win = new Window();
win.Cursor = new Cursor(StandardCursorType.Hand);
```

To load from settings for a plugin window:

```csharp
var win = new Window();
ThemeManager.ApplyFromSettings(win, "Plugins");
CursorManager.ApplyFromSettings(win, "Plugins");
win.Show();
```

Any `StandardCursorType` value is valid. Themes and skins may also set the `Cursor` property in their style definitions.
