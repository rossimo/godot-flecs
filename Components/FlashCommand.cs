using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/command.png"), Component]
public partial class FlashCommand : Node
{
    [Export]
    public Color Color { get; set; }
}

public class Flash
{
    public static Routine System(World world) =>
        world.Routine(callback: (Entity entity, ref Sprite2D node, ref FlashCommand flash) =>
            {
                entity.Remove<FlashCommand>();

                node.Modulate = new Color(flash.Color);
                node.CreateTween().TweenProperty(node, "modulate", new Color(1, 1, 1), 1f);
            });
}