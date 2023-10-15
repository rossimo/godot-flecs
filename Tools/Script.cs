using Godot;
using Flecs.NET.Core;
using Flecs.NET.Bindings;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public interface Script
{
    Task Run(Entity entity);

    void OnRemove(Entity entity);
}

public static class ScriptUtils
{
    public static Task<T> OnSetAsync<S, T>(this Entity entity, Script script) where S : Script
    {
        entity.AssertAlive();

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

        var scriptObserver = entity.ScriptObserver<S, T>(promise);

        return promise.Task.ContinueWith((task) =>
        {
            componentObserver.Destruct();
            scriptObserver.Destruct();
            return task.Result;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public static Task<T> OnRemoveAsync<S, T>(this Entity entity) where S : Script
    {
        entity.AssertAlive();

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

        var scriptObserver = entity.ScriptObserver<S, T>(promise);

        return promise.Task.ContinueWith((task) =>
        {
            componentObserver.Destruct();
            scriptObserver.Destruct();
            return task.Result;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    static Observer ScriptObserver<S, T>(this Entity entity, TaskCompletionSource<T> promise) where S : Script
    {
        var world = entity.CsWorld();

        return world.Observer(
            filter: world.FilterBuilder().Term<S>().Entity(entity),
            observer: world.ObserverBuilder().Event(Ecs.OnRemove),
            callback: (ref S script) =>
            {
                if (!promise.Task.IsCompleted)
                {
                    if (entity.IsValid() && entity.IsAlive())
                    {
                        script.OnRemove(entity);
                    }
                    promise.SetException(new ScriptRemovedException(entity));
                }
            }
        );
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