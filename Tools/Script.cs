using Flecs.NET.Core;
using Godot;

public static class ScriptUtils
{
    public static async Task Set<S, C>(this Entity entity, Script script, C component) where C : Command where S : Script
    {
        entity.Set(component);

        var observer = script.OnRemove<S>(entity, component.Promise);

        try
        {
            await component.Task;
        }
        finally
        {
            observer.Destruct();
        }
    }

    public static async Task Set<C>(this Entity entity, C component) where C : Command
    {
        entity.Set(component);

        await component.Task;
    }
}

public partial class Script : BootstrapNode2D
{
    public virtual Task Run(Entity entity)
    {
        return Task.CompletedTask;
    }

    public async Task Set<S, C>(Entity entity, C component) where C : Command where S : Script
    {
        await entity.Set<S, C>(this, component);
    }

    public Observer OnRemove<S>(Entity entity, TaskCompletionSource promise) where S : Script
    {
        var entityId = entity.Id.Value;
        var world = entity.CsWorld();

        return world.Observer()
            .Term<S>()
            .Entity(entity)
            .Event(Ecs.OnRemove)
            .Each((Entity entity, ref S candidate) =>
                promise.TrySetException(new ScriptRemovedException(entityId, GetType())));
    }
}

public class DeadEntityException : Exception
{
    public readonly ulong EntityId;

    public DeadEntityException(ulong entity) : base($"{entity} is not alive")
    {
        EntityId = entity;
    }
}

public class ScriptRemovedException : Exception
{
    public readonly ulong EntityId;

    public ScriptRemovedException(ulong entity, Type type) : base($"{type} has been removed from {entity}")
    {
        EntityId = entity;
    }
}
