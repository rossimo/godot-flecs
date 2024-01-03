using Flecs.NET.Core;
using Godot;

public static class ScriptUtils
{
    public static async Task Set<C>(this Entity entity, Script script, C component) where C : IAsync
    {
        entity.Set(component);

        var observer = script.OnRemove(entity, component);

        try
        {
            await component.Task;
        }
        finally
        {
            observer.Destruct();
        }
    }

    public static async Task Set<C>(this Entity entity, C component) where C : IAsync
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

    public async Task Set<C>(Entity entity, C component) where C : IAsync
    {
        await entity.Set<C>(this, component);
    }

    public Observer OnRemove<C>(Entity entity, C component) where C : IAsync
    {
        var world = entity.CsWorld();
        var entityId = entity.Id.Value;
        var scriptComponent = this.GetComponent(world);

        return world.Observer()
            .Term(scriptComponent)
            .Entity(entity)
            .Event(Ecs.OnRemove)
            .Each((Entity entity) =>
                component.SetException(world, new ScriptRemovedException(entityId, GetType())));
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
