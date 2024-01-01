using Godot;
using Flecs.NET.Core;

public static class Async
{
    public static async Task OnSetAsync<C>(this Entity entity, TaskCompletionSource? promise = null)
    {
        promise ??= new TaskCompletionSource();

        var world = entity.CsWorld();

        var observer = world.Observer()
            .Term<C>()
            .Entity(entity)
            .Event(Ecs.OnSet)
            .Each((Entity possible, ref C component) =>
            {
                if (!promise.Task.IsCompleted)
                {
                    promise.SetResult();
                }
            }
        );

        try
        {
            await promise.Task;
        }
        finally
        {
            observer.Destruct();
        };
    }

    public static async Task OnRemoveAsync<C>(this Entity entity, TaskCompletionSource? promise = null)
    {
        promise ??= new TaskCompletionSource();

        var world = entity.CsWorld();

        var observer = world.Observer()
            .Term<C>()
            .Entity(entity)
            .Event(Ecs.OnRemove)
            .Each((ref C component) =>
            {
                if (!promise.Task.IsCompleted)
                {
                    promise.SetResult();
                }
            }
        );

        try
        {
            await promise.Task;
        }
        finally
        {
            observer.Destruct();
        };
    }

    public static async Task SetAsync<C>(this Entity entity, C component, TaskCompletionSource? promise = null)
    {
        await ImmediateAsync(entity);

        if (promise?.Task.IsCompleted == true)
        {
            return;
        }

        entity.AssertAlive();

        entity.Set(component);
    }

    public static async Task RemoveAsync<C>(this Entity entity, TaskCompletionSource? promise = null)
    {
        await ImmediateAsync(entity);

        if (promise?.Task.IsCompleted == true)
        {
            return;
        }

        entity.AssertAlive();

        entity.Remove<C>();
    }

    public static async Task Command<C>(this Entity entity, C component) where C : Command
    {
        await SetAsync(entity, component, component.Promise);

        await component.Task;
    }

    public static Task ImmediateAsync(this Entity entity)
    {
        return entity.CsWorld()
            .Get<TaskCompletionSource>()
            .Task;
    }

    public static async Task OnChangeAsync<C>(Entity entity, C component, TaskCompletionSource? promise = null)
    {
        promise ??= new TaskCompletionSource();

        await SetAsync(entity, component, promise);

        await OnChangeAsync<C>(entity, promise);
    }

    public static Task OnChangeAsync<C>(Entity entity, TaskCompletionSource? promise = null)
    {
        promise ??= new TaskCompletionSource();

        var set = OnSetAsync<C>(entity, promise);
        var remove = OnRemoveAsync<C>(entity, promise);

        return Task.WhenAny(set, remove);
    }
}

public partial class Script : BootstrapNode2D
{
    public virtual Task Run(Entity entity)
    {
        return Task.CompletedTask;
    }

    private static async Task ObserveScript<S, C>(Entity entity, Task task, TaskCompletionSource promise) where S : Script
    {
        var world = entity.CsWorld();

        var observer = world.Observer()
            .Term<S>()
            .Entity(entity)
            .Event(Ecs.OnRemove).Event(Ecs.OnSet)
            .Each((Entity entity) =>
            {
                if (!promise.Task.IsCompleted)
                {
                    promise.SetException(new ScriptRemovedException());
                }
            }
        );

        try
        {
            await await Task.WhenAny(task, promise.Task);
        }
        finally
        {
            observer.Destruct();
        }
    }

    public Task SetAsync<S, C>(Entity entity, C component, TaskCompletionSource? promise = null) where S : Script
    {
        return ObserveScript<S, C>(entity, entity.SetAsync(component), promise ?? new TaskCompletionSource());
    }

    public Task RemoveAsync<S, C>(Entity entity, TaskCompletionSource? promise = null) where S : Script
    {
        return ObserveScript<S, C>(entity, entity.RemoveAsync<C>(), promise ?? new TaskCompletionSource());
    }

    public Task OnSetAsync<S, C>(Entity entity, TaskCompletionSource? promise = null) where S : Script
    {
        return ObserveScript<S, C>(entity, entity.OnSetAsync<C>(), promise ?? new TaskCompletionSource());
    }

    public Task OnRemoveAsync<S, C>(Entity entity, TaskCompletionSource? promise = null) where S : Script
    {
        return ObserveScript<S, C>(entity, entity.OnRemoveAsync<C>(), promise ?? new TaskCompletionSource());
    }

    public async Task OnChangeAsync<S, C>(Entity entity, C component, TaskCompletionSource? promise = null) where S : Script
    {
        await entity.SetAsync(component);

        await OnChangeAsync<S, C>(entity, promise);
    }

    public Task OnChangeAsync<S, C>(Entity entity, TaskCompletionSource? promise = null) where S : Script
    {
        promise ??= new TaskCompletionSource();

        var set = OnSetAsync<S, C>(entity, promise);
        var remove = OnRemoveAsync<S, C>(entity, promise);

        return Task.WhenAny(set, remove);
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
    public ScriptRemovedException() : base($"Script has been removed")
    {
    }
}
