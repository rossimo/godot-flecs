using Godot;
using Flecs.NET.Core;

public class Timer
{
    public static IEnumerable<Observer> Observers(World world) =>
        new[] {
            Setup(world)
        };

    public static IEnumerable<Routine> Systems(World world) =>
        new Routine[] {

        };

    public static Observer Setup(World world)
    {
        return world.Observer()
            .Term<Godot.Timer>()
            .Event(Ecs.OnSet)
            .Each((Entity entity, ref Godot.Timer timer) =>
            {
                timer.ProcessCallback = Godot.Timer.TimerProcessCallback.Physics;
                timer.Start();

                var timerVar = timer;
                timer.Timeout += () =>
                {
                    world.DeferSuspend();
                    try
                    {
                        foreach (var child in timerVar.GetChildren())
                        {
                            entity.SetNode(child.Duplicate());
                        }

                        if (timerVar.OneShot)
                        {
                            timerVar.QueueFree();
                        }
                    }
                    finally
                    {
                        world.DeferResume();
                    }
                };
            });
    }
}