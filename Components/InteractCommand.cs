using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png"), Component]
public partial class InteractCommand : Node
{
    public Entity Target = Entity.Null();
}