using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/trigger.png"), Many]
public partial class InteractTrigger : Trigger
{
}

public class Interact
{
    public static IEnumerable<Observer> Observers(World world) =>
        new[] {
            Setup(world),
        };

    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            System(world),
        };

    public static int DEFAULT_RADIUS = 100;

    public static Observer Setup(World world)
    {
        var playerQuery = world.Query<Player>();

        return world.Observer()
            .Term<Button>()
            .Event(Ecs.OnSet)
            .Each((Entity entity, ref Button button) =>
            {
                button.Pressed += () =>
                {
                    entity.Trigger<ClickTrigger>();

                    foreach (var player in playerQuery.All())
                    {
                        var move = new MoveCommand()
                        {
                            Position = entity.Get<Node2D>().GlobalPosition,
                            Radius = DEFAULT_RADIUS,
                            Target = entity,
                        };

                        move.AddChild(new InteractCommand()
                        {
                            Target = entity
                        });

                        player.Set(move);
                    }
                };
            });
    }

    public static Routine System(World world) =>
        world.Routine<InteractCommand>()
        .Each((Entity entity, ref InteractCommand interact) =>
        {
            if (interact.Target.IsAlive())
            {
                var position = entity.Get<Node2D>().Position;
                var destination = interact.Target.Get<Node2D>().Position;

                if (position.DistanceTo(destination) <= DEFAULT_RADIUS)
                {
                    interact.Target.Trigger<InteractTrigger>(entity);
                    interact.Complete(entity);
                    entity.Remove<InteractCommand>();
                }
            }
        });
}