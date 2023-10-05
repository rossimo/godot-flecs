using Godot;

[GlobalClass, Component]
public partial class Speed : Node
{
    [Export]
    public float Value { get; set; }
}