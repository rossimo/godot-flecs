using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png"), Component]
public partial class DeleteCommand : Node
{
}

public class Delete
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            System(world),
        };

    public static Routine System(World world) =>
        world.Routine(
            filter: world.FilterBuilder().Term<DeleteCommand>().Term<Sprite2D>(),
            callback: (Entity entity) =>
            {
                entity.Destruct();
            });
}