using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/component.png")]
public partial class WorldNode : Node2D
{
    public World World;

    public override void _EnterTree()
    {
        base._EnterTree();

        World = World.Create();

        World.Set(new Tasks());

        Interop.Observers(World);

        World.Set(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        World.Add<Destructing>();

        World.Dispose();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        World.Progress();

        World.Get<Tasks>().Yield();
    }
}

public class Tasks
{
    private List<Command> _Tasks = new List<Command>();

    public void Add(Command task)
    {
        _Tasks.Add(task);
    }

    public void Yield()
    {
        foreach (var command in _Tasks)
        {
            command.Yield();
        }

        _Tasks.Clear();
    }
}