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
        await entity.SetAsync(new MoveCommand
        {
            Position = Position,
            Radius = Target == Entity.Null()
                ? 0
                : 66
        }, this);

        await Task.WhenAny(
            OnSetAsync<MoveCommand>(entity),
            OnRemoveAsync<MoveCommand>(entity));

        if (Target == Entity.Null()) return;

        var distance = Target.Get<Node2D>().Position.DistanceTo(entity.Get<Node2D>().Position);

        if (distance <= 66)
        {
            entity.Set(new InteractCommand()
            {
                Target = Target
            });
        }
    }

    async Task<T> OnRemoveAsync<T>(Entity entity) =>
        await OnRemoveAsync<MoveAndInteractScript, T>(entity);

    async Task<T> OnSetAsync<T>(Entity entity) =>
        await OnSetAsync<MoveAndInteractScript, T>(entity);
}
