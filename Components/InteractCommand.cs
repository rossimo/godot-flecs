using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class InteractCommand : Command
{
    public Entity Target = Entity.Null();
}