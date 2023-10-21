using Godot;
using Flecs.NET.Core;

public partial class Script : Node
{
    public virtual Task Run(Entity entity)
    {
        return Task.CompletedTask;
    }

    private TaskCompletionSource? defer;

    public async Task<T> OnSetAsync<S, T>(Entity entity) where S : Script
    {
        AssertAlive(entity);

        await IsImmediate(entity.CsWorld());

        var world = entity.CsWorld();

        var deferred = entity.CsWorld().IsDeferred();

        var promise = new TaskCompletionSource<T>();

        var componentObserver = world.Observer(
            filter: world.FilterBuilder().Term<T>().Entity(entity),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity possible, ref T component) =>
            {
                if (!promise.Task.IsCompleted)
                {
                    promise.SetResult(component);
                }
            }
        );

        var scriptObserver = ScriptObserver<S, T>(entity, promise);

        try
        {
            return await promise.Task;
        }
        finally
        {
            componentObserver.Destruct();
            scriptObserver.Destruct();
        };
    }

    public async Task<T> OnRemoveAsync<S, T>(Entity entity) where S : Script
    {
        AssertAlive(entity);

        await IsImmediate(entity.CsWorld());

        var world = entity.CsWorld();

        var promise = new TaskCompletionSource<T>();

        var componentObserver = world.Observer(
            filter: world.FilterBuilder().Term<T>().Entity(entity),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (ref T component) =>
            {
                if (!promise.Task.IsCompleted)
                {
                    promise.SetResult(component);
                }
            }
        );

        var scriptObserver = ScriptObserver<S, T>(entity, promise);

        try
        {
            return await promise.Task;
        }
        finally
        {
            componentObserver.Destruct();
            scriptObserver.Destruct();
        };
    }

    Observer ScriptObserver<S, T>(Entity entity, TaskCompletionSource<T> promise) where S : Script
    {
        var world = entity.CsWorld();

        return world.Observer(
            filter: world.FilterBuilder().Term<S>().Entity(entity),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (Iter it) =>
            {
                if (!promise.Task.IsCompleted)
                {
                    promise.SetException(new ScriptRemovedException(entity));
                }
            }
        );
    }

    public void Iterate()
    {
        if (defer != null)
        {
            defer.SetResult();
            defer = null;
        }
    }

    public async Task SetAsync<T>(Entity entity, T component)
    {
        await IsImmediate(entity.CsWorld());

        AssertAlive(entity);

        entity.Set(component);
    }

    public async Task RemoveAsync<T>(Entity entity)
    {
        await IsImmediate(entity.CsWorld());

        AssertAlive(entity);

        entity.Remove<T>();
    }

    async Task IsImmediate(World world)
    {
        if (world.IsDeferred())
        {
            if (defer == null)
            {
                defer = new TaskCompletionSource();
            }

            await defer.Task;
        }
    }

    void AssertAlive(Entity entity)
    {
        if (!entity.IsAlive())
        {
            throw new DeadEntityException(entity);
        }
    }
}

public class DeadEntityException : Exception
{
    public DeadEntityException(Entity entity) : base($"Entity is not alive")
    {
    }
}

public class ScriptRemovedException : Exception
{
    public ScriptRemovedException(Entity entity) : base($"Script has been removed")
    {
    }
}

public static class ScriptUtils
{
    public static Task SetAsync<T>(this Entity entity, Script script, T component)
    {
        return script.SetAsync(entity, component);
    }

    public static Task RemoveAsync<T>(this Entity entity, Script script)
    {
        return script.RemoveAsync<T>(entity);
    }

    public static Task OnSetAsync<S, T>(this Entity entity, S script) where S : Script
    {
        return script.OnSetAsync<S, T>(entity);
    }

    public static Task OnRemoveAsync<S, T>(this Entity entity, S script) where S : Script
    {
        return script.OnRemoveAsync<S, T>(entity);
    }

    public static async Task WatchAsync<S, T>(this Entity entity, S script, T component) where S : Script
    {
        await entity.SetAsync(script, component);

        var set = script.OnSetAsync<S, T>(entity);
        var remove = script.OnRemoveAsync<S, T>(entity);

        await Task.WhenAny(set, remove);
    }
}