using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class FlashCommand : Node
{
    [Export]
    public Color Color { get; set; }
}

public class Flash
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            Animate(world),
        };

    public static Routine Animate(World world) =>
        world.Routine(
            filter: world.FilterBuilder<Sprite2D, FlashCommand>(),
            callback: (Entity entity, ref Sprite2D node, ref FlashCommand flash) =>
            {
                entity.Conclude(flash);

                node.Modulate = new Color(flash.Color);
                node.CreateTween().TweenProperty(node, "modulate", new Color(1, 1, 1), 0.5f);
            });
}