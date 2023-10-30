using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/command.png")]
public partial class TimerCommand : Node
{
    [Export]
    public ulong Millis { get; set; }

    [Export]
    public bool Repeat { get; set; }

    public ulong Ticks { get; set; } = 0;
}

public class Timer
{
    public static IEnumerable<Observer> Observers(World world) =>
        new[] {
            Setup(world)
        };

    public static IEnumerable<Routine> Systems(World world) =>
        new[] {
            System(world),
        };

    public static Observer Setup(World world)
    {
        return world.Observer(
            filter: world.FilterBuilder<TimerCommand>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (ref TimerCommand timer) =>
            {
                timer.Ticks = Physics.MillisToTicks(timer.Millis);
            });
    }

    public static Routine System(World world) =>
        world.Routine(
        filter: world.FilterBuilder<TimerCommand>(),
        callback: (Entity entity, ref TimerCommand timer) =>
        {
            timer.Ticks--;

            if (timer.Ticks <= 0)
            {
                entity.Trigger<TimerTrigger>();

                if (timer.Repeat)
                {
                    timer.Ticks = Physics.MillisToTicks(timer.Millis);
                }
                else
                {
                    entity.Complete(timer);
                }
            }
        });
}