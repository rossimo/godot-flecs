using Godot;

[GlobalClass, Icon("res://resources/tools/component.png")]
public partial class Speed : Node
{
	[Export]
	public float Value { get; set; }
}
