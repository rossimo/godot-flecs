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

        World.Set(new TaskCompletionSource());

        Interop.Observers(World);

        World.Set(this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        World.Dispose();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        World.Progress();

        World.Get<TaskCompletionSource>().SetResult();
        World.Set(new TaskCompletionSource());
    }
}