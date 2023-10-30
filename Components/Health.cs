using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/component.png")]
public partial class Health : Node
{
    [Export]
    public int Value { get; set; } = 0;
}