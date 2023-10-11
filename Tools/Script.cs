using Godot;
using Flecs.NET.Core;

public interface Script
{
    Task Run(Entity entity);
}

public static class ScriptUtils
{
    public static Task<T> OnSetAsync<T>(this Entity entity)
    {
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
}
