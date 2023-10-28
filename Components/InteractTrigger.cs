using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/trigger.png"), Component, Many]
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

    public static Observer Setup(World world)
    {
        var playerQuery = world.Query(filter: world.FilterBuilder().Term<Player>());

        return world.Observer(
            filter: world.FilterBuilder<Button>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity entity, ref Button button) =>
            {
                button.Pressed += () =>
                {
                    foreach (var player in playerQuery.All())
                    {
                        player.Set(new MoveAndInteractScript()
                        {
                            Position = entity.Get<Node2D>().GlobalPosition,
                            Target = entity
                        });
                    }
                };
            });
    }

    public static Routine System(World world) =>
        world.Routine(
        filter: world.FilterBuilder<InteractCommand>(),
        callback: (Entity entity, ref InteractCommand interact) =>
        {
            interact.Target.Trigger<InteractTrigger>(entity);
            entity.Remove<InteractCommand>();
        });
}