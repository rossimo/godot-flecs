using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class DamageCommand : BootstrapNode2D
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
        world.Routine()
            .Term<DamageCommand>().Term<Health>()
            .Each((Entity entity, ref DamageCommand damage, ref Health health) =>
            {
                health.Value -= damage.Value;

                if (entity.Has<Debug>())
                {
                    GD.Print($"Health: {health.Value}");
                }

                entity.Set(new FlashCommand()
                {
                    Color = new Color(1, 0, 0)
                });

                if (health.Value <= 0)
                {
                    entity.Set(new DeleteCommand());
                }

                damage.Complete(entity);
                entity.Remove<DamageCommand>();
            });


    public static Routine Cleanup(World world) =>
        world.Routine()
            .Term<DamageCommand>()
            .Each((Entity entity, ref DamageCommand damage) =>
            {
                entity.Remove<DamageCommand>();
            });
}