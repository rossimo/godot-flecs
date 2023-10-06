using Godot;

[GlobalClass, Icon("res://resources/component.png"), Component]
public partial class Speed : Node
{
    [Export]
    public float Value { get; set; }
}