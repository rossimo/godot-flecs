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
        var range = Target.IsAlive() ? 100 : 0;

        await entity.SetAsync(this, new MoveCommand
        {
            Target = Target,
            Position = Position,
            Range = range
        });

        await entity.OnChangeAsync<MoveCommand>(this);

        if (!Target.IsAlive()) return;

        if (!entity.Has<Node2D>() || !Target.Has<Node2D>()) return;

        var position = entity.Get<Node2D>().Position;
        var destination = Target.Get<Node2D>().Position;

        if (position.DistanceTo(destination) > range) return;
        
        await entity.SetAsync(this, new InteractCommand()
        {
            Target = Target
        });
    }
}

public static class MoveAndInteractScriptUtils
{
    public static async Task OnChangeAsync<T>(this Entity entity, MoveAndInteractScript script) =>
        await entity.OnChangeAsync<MoveAndInteractScript, T>(script);
}
