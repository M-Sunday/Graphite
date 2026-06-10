# Graphite

A Unity project — terminal emulator, development tools, and the Graphite dialog system for the Unity Editor.

## Project Structure

```
Assets/
├── Animations/      # Animation controllers and clips
├── Editor/          # Editor tools
├── Fonts/           # Custom fonts
├── Graphite/        # Graphite dialog system
│   ├── Dialog/      # Dialog graph assets and character database
│   ├── Prefabs/     # Option panel prefabs
│   ├── Runtime/     # Runtime dialog scripts
│   │   └── Data/    # Dialog data types (DialogEntry, DialogOption, DialogSO)
│   └── Scripts/     # Graph editor source
│       ├── Dialog/  # Shared dialog types (DialogGraphContainer)
│       └── Editor/  # Graph editor nodes, views, utilities
│           └── Dialog/
│               └── Resources/  # Icons and stylesheets
├── Scenes/          # Game and UI scenes
│   ├── dialog ui.unity
│   ├── LOADING.unity
│   └── SampleScene.unity
└── TextMesh Pro/    # TextMesh Pro assets
Packages/            # Unity package dependencies
ProjectSettings/     # Unity project configuration
```

## Requirements

- Unity 2022.3 or newer
- .NET Standard 2.1

## Getting Started

1. Open the project in Unity Hub
2. Navigate to `Assets/Scenes/SampleScene` to begin
3. Open the Terminal window via `Window > General > Terminal`
4. Open the Dialog Graph editor via `Dialog > Dialogue Window`

## License

See [LICENSE](./LICENSE) for details.
