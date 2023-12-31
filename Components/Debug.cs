using Godot;

[GlobalClass, Icon("res://resources/tools/component.png")]
public partial class Debug : Node
{
	[Export]
	public float Value { get; set; }
}
