using Godot;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class AnimationCommand : Command
{
    [Export]
    public string Animation { get; set; } = "";
}