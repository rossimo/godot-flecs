using Godot;

public partial class Trigger : BootstrapNode2D
{
    [Export]
    public Target? Target { get; set; } = new TargetSelf();
}
