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
            Cleanup(world)
        };

    public static Routine System(World world) =>
        world.Routine(
            filter: world.FilterBuilder().Term<DamageCommand>().Term<Health>().NotTrigger(),
            callback: (Entity entity, ref DamageCommand damage, ref Health health) =>
            {
                health.Value -= damage.Value;
                Console.WriteLine($"Health: {health.Value}");

                if (health.Value <= 0)
                {
                    entity.Set(new DeleteCommand());
                }

                entity.Complete(damage);
            });


    public static Routine Cleanup(World world) =>
        world.Routine(
            filter: world.FilterBuilder().Term<DamageCommand>().NotTrigger(),
            callback: (Entity entity, ref DamageCommand damage) =>
            {
                entity.Remove<DamageCommand>();
            });
}