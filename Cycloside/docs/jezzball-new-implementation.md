# New Jezzball Plugin Implementation

## Overview

This is a completely fresh implementation of the Jezzball plugin based on the Canvas project found in the docs folder. The original plugin has been disabled and replaced with this new implementation.

## Key Features

### Game Mechanics
- **Wall Building**: Click to place walls that grow in both directions
- **Ball Physics**: Realistic ball bouncing with proper collision detection
- **Grid System**: 20x20 pixel grid for precise wall placement
- **Area Capture**: Flood fill algorithm to determine captured areas
- **Scoring**: Points based on captured area percentage
- **Level Progression**: Each level adds more balls (up to 8)

### Controls
- **Left Click**: Place a wall
- **Right Click**: Change wall direction (Horizontal/Vertical)
- **R Key**: Restart game
- **Space**: Pause/Unpause
- **F1**: Show help

### Game Rules
1. Goal: Capture at least 75% of the area while avoiding balls
2. If a ball hits a growing wall, you lose a life
3. Enclosed areas without balls count toward your score
4. Clear 75% of the area to advance to the next level
5. Each level adds more balls
6. Game ends when you run out of lives (3 lives total)

### Technical Implementation

#### Core Classes
- **JezzballPlugin**: Main plugin entry point
- **JezzballControl**: UI control handling rendering and input
- **JezzballGameState**: Game logic and state management
- **Ball**: Ball physics and rendering
- **Wall**: Wall growth and collision detection
- **GridCell**: Grid system for area calculation

#### Key Algorithms
- **Flood Fill**: Used to determine captured areas
- **Line-Circle Intersection**: For ball-wall collision detection
- **Grid-based Area Calculation**: Efficient area scoring system

#### Sound System
- Kept from original implementation
- Supports multiple sound events (Click, WallBuild, WallHit, etc.)
- Uses AudioService for playback

## Differences from Original

### What's New
- Completely rewritten from scratch
- Based on Canvas project implementation
- Simplified and more focused codebase
- Better separation of concerns
- More accurate game mechanics

### What's Kept
- Sound system architecture
- Basic plugin structure
- Window management
- Keyboard shortcuts

### What's Removed
- Complex theming system
- Power-ups (for now)
- Advanced ball types
- Particle effects
- Complex menu system

## Future Enhancements

1. **Power-ups**: Add back ice walls, extra lives, freeze, double score
2. **Theming**: Implement theme system for different visual styles
3. **Particles**: Add visual effects for collisions and captures
4. **High Scores**: Persistent high score system
5. **Settings**: Configurable game options
6. **Advanced Balls**: Different ball types with unique behaviors

## Files Modified

- `Cycloside/Plugins/BuiltIn/JezzballPlugin.cs` - Completely new implementation
- `Cycloside/Plugins/BuiltIn/JezzballSound.cs` - Sound system (kept from original)
- `Cycloside/Plugins/BuiltIn/JezzballPlugin.cs.disabled` - Original plugin (disabled)
- `Cycloside/Plugins/BuiltIn/JezzballSound.cs.disabled` - Original sound file (disabled)

## Building and Running

The plugin is designed to work with the existing Cycloside framework. To build:

```bash
cd Cycloside
dotnet build
dotnet run
```

The plugin will be automatically loaded and available in the plugin menu.

## Game Flow

1. **Start**: Game begins with 3 lives, level 1, 2 balls
2. **Play**: Click to place walls, avoid balls hitting growing walls
3. **Capture**: Walls grow until they hit other walls or boundaries
4. **Score**: Enclosed areas without balls are captured
5. **Advance**: Reach 75% area captured to advance to next level
6. **Continue**: Each level adds more balls, increasing difficulty
7. **Game Over**: Lose all lives or complete all levels

## Technical Notes

- Uses Avalonia UI framework for rendering
- 60 FPS game loop with delta time
- Efficient collision detection algorithms
- Grid-based area calculation for performance
- Proper resource disposal and cleanup