using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class MoveAndInteractScript : Script
{
    public Entity Target = Entity.Null();

    [Export]
    public Vector2 Position;

    public override async Task Run(Entity entity)
    {
        var range = Target.IsAlive() ? 66 : 0;

        await entity.WatchAsync(this, new MoveCommand
        {
            Position = Position,
            Range = range
        });

        if (!Target.IsAlive()) return;

        var position = entity.Get<Node2D>().Position;
        var destination = Target.Get<Node2D>().Position;

        if (position.DistanceTo(destination) > range) return;

        await entity.SetAsync(this, new InteractCommand()
        {
            Target = Target
        });
    }
}
