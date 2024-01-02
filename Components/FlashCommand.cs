using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class FlashCommand : Command
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
        world.Routine<Entity2D, FlashCommand>()
            .Each((Entity entity, ref Entity2D entity2d, ref FlashCommand flash) =>
            {
                entity.Remove<FlashCommand>();
                flash.Invoke(entity);

                entity2d.Modulate = new Color(flash.Color);

                var original = entity.Has<ColorizeCommand>()
                    ? entity.Get<ColorizeCommand>().Color
                    : new Color(1, 1, 1);

                entity2d.CreateTween().TweenProperty(entity2d, "modulate", original, 0.5f);
            });
}