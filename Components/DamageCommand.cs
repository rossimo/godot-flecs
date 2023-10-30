using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class DamageCommand : Node
{
    [Export]
    public int Value { get; set; } = 0;
}

public class Damage
{
    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            System(world),
        };

    public static Routine System(World world) =>
        world.Routine(
            filter: world.FilterBuilder().Term<DamageCommand>().Term<Health>(),
            callback: (Entity entity, ref DamageCommand damage, ref Health health) =>
            {
                health.Value -= damage.Value;

                if (health.Value <= 0)
                {
                    entity.Set(new DeleteCommand());
                }

                entity.Complete(damage);
                entity.Cleanup();
            });
}