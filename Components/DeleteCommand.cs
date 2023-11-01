using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
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
            filter: world.FilterBuilder().Term<DeleteCommand>().NotTrigger(),
            callback: (Entity entity) =>
            {
                entity.Destruct();
            });
}