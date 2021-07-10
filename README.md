# ExtraNodesCS
Godot addon with additional nodes (C# Version)

## Hint:
Currently there is only the the `ThrowCast2D` node, a 3D version is currently under development / testing.

More nodes will come as I need them for my own projects.

I only add nodes that are of excellent code quality, that are tested well. But I'm only a human; even I could do errors. If you find one, please add an issue to the
[Github Issue Page](https://github.com/AFE-GmdG/ExtraNodesCS/issues).

Greetings, Andreas

## Usage:
You can omit the folder `Demo` if you want to use this plugin.

## Known Issues:
### Memoryleak in ThrowCast2D node:
The culprit seems to be the `Physics2DServer.SpaceGetDirectState(...)` method, used by the collision calculation in ThrowCast2D but I currently don't know how to solve.


## Version
### 1.0.0
- First iteration
- Adding ThrowCast2D
- Adding DemoScene
