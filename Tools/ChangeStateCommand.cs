using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class ChangeStateCommand : Command
{
    [Export]
    public string State { get; set; } = "";
}