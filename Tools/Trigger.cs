using Godot;

[Many]
public partial class Trigger : Node
{
    [Export]
    public Target? Target { get; set; }
}
