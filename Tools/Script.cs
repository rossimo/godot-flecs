using Godot;
using Flecs.NET.Core;
using Flecs.NET.Bindings;

public interface Script
{
    Task Run(Entity entity);
}

public static class ScriptUtils
{
    public static Task<T> OnSetAsync<T>(this Entity entity)
    {
        entity.AssertAlive();

        var world = entity.CsWorld();

        var promise = new TaskCompletionSource<T>();

        var observer = world.Observer(
            filter: world.FilterBuilder().Term<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnSet),
            callback: (Entity possible, ref T component) =>
            {
                if (possible.Id.Value == entity.Id.Value && !promise.Task.IsCompleted)
                {
                    promise.SetResult(component);
                }
            }
        );

        return promise.Task.ContinueWith((task) =>
        {
            observer.Destruct();
            return task.Result;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public static Task<T> OnRemoveAsync<T>(this Entity entity)
    {
        entity.AssertAlive();

        var world = entity.CsWorld();

        var promise = new TaskCompletionSource<T>();

        var observer = world.Observer(
            filter: world.FilterBuilder().Term<T>(),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (Entity possible, ref T component) =>
            {
                if (possible.Id.Value == entity.Id.Value && !promise.Task.IsCompleted)
                {
                    promise.SetResult(component);
                }
            }
        );

        return promise.Task.ContinueWith((task) =>
        {
            observer.Destruct();
            return task.Result;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public static void SetAsync<T>(this Entity entity, T component)
    {
        entity.AssertAlive();

        entity.Set(component);
    }

    public static void AssertAlive(this Entity entity)
    {
        if (!entity.IsAlive())
        {
            throw new EntityDeadException(entity);
        }
    }
}

public class EntityDeadException : Exception
{
    public EntityDeadException(Entity entity) : base($"Entity is not alive")
    {
    }
}