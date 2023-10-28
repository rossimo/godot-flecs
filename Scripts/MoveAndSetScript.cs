using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public partial class MoveAndSetScript : Script
{
    [Export]
    public Vector2 Position;

    [Export]
    public Node? Component;

    public Entity Target = Entity.Null();

    public override async Task Run(Entity entity)
    {
        var range = Target.IsAlive() ? 100 : 0;

        await entity.OnChangeAsync(this, new MoveCommand
        {
            Target = Target,
            Position = Position,
            Range = range
        });

        if (!Target.IsAlive() ||
            !entity.Has<Node2D>() ||
            !Target.Has<Node2D>() ||
            Component is null)
        {
            return;
        }

        var position = entity.Get<Node2D>().Position;
        var destination = Target.Get<Node2D>().Position;
        if (position.DistanceTo(destination) > range)
        {
            return;
        }

        await entity.ReflectionSetAsync(this, Component);
    }
}

public static class MoveAndInteractScriptUtils
{
    public static async Task OnChangeAsync<C, T>(this Entity entity, MoveAndSetScript script, T component) =>
        await entity.OnChangeAsync<MoveAndSetScript, T>(script, component);
}
