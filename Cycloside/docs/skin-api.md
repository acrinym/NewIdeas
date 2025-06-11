# Skinning API

Skins are Avalonia resource dictionaries stored in the `Skins/` folder with the extension `.axaml`. The name of the selected skin is saved in `settings.json` under `ActiveSkin`.

The `SkinManager.LoadCurrent()` method loads the current skin at startup. Contributors can create new skins by placing an `.axaml` style file in the folder and updating the setting via the Runtime Settings panel.

A minimal skin file looks like this:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui">
    <Style Selector="Window">
        <Setter Property="Background" Value="#222" />
        <Setter Property="Foreground" Value="White" />
    </Style>
</ResourceDictionary>
```

Styles cascade like regular Avalonia themes so widgets and plugin windows automatically adopt the new look.
