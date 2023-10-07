using Godot;

[GlobalClass]
public partial class TargetSelf : Target
{
    [Export]
    public float Value { get; set; }
}