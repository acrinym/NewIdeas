# Skinning API

Skins are Avalonia resource dictionaries stored in the `Skins/` folder with the extension `.axaml`. You can assign one or more skins to a plugin or component in `settings.json` under `ComponentSkins`.

Use `SkinManager.ApplySkinTo(element, "MySkin")` to layer a skin on top of the global theme at runtime.

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
