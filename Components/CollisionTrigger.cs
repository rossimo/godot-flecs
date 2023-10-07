using Godot;

[GlobalClass, Icon("res://resources/trigger.png"), Component, Many]
public partial class CollisionTrigger : Node
{
    [Export]
    public Target? Target { get; set; }
}
