using Godot;
using Flecs.NET.Core;

[GlobalClass, Icon("res://resources/tools/script.png"), Component]
public abstract partial class Script : Node
{
    public abstract Task Run(Entity entity);

    public Task<T> OnSetAsync<S, T>(Entity entity) where S : Script
    {
        AssertAlive(entity);

        var world = entity.CsWorld();

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

        return promise.Task.ContinueWith((task) =>
        {
            componentObserver.Destruct();
            scriptObserver.Destruct();

            task.Exception?.Handle(ex => throw ex);

            return task.Result;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public Task<T> OnRemoveAsync<S, T>(Entity entity) where S : Script
    {
        AssertAlive(entity);

        var world = entity.CsWorld();

        var promise = new TaskCompletionSource<T>();

        var componentObserver = world.Observer(
            filter: world.FilterBuilder().Term<T>().Entity(entity),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (Entity possible, ref T component) =>
            {
                if (!promise.Task.IsCompleted)
                {
                    promise.SetResult(component);
                }
            }
        );

        var scriptObserver = ScriptObserver<S, T>(entity, promise);

        return promise.Task.ContinueWith((task) =>
        {
            componentObserver.Destruct();
            scriptObserver.Destruct();

            task.Exception?.Handle(ex => throw ex);

            return task.Result;
        }, TaskScheduler.FromCurrentSynchronizationContext());
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

    public void SetAsync<T>(Entity entity, T component)
    {
        AssertAlive(entity);

        entity.Set(component);
    }

    public void RemoveAsync<T>(Entity entity)
    {
        AssertAlive(entity);

        entity.Remove<T>();
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