using Flecs.NET.Core;

public interface IAsync
{
    public Task Task { get; }

    public void Async();
    public void SetResult(World world);
    public void SetException(World world, Exception exception);
}

public static class TaskUtils
{
    public static void Success<C>(this Entity entity, bool remove = true) where C : IAsync
    {
        if (entity.Has<C>())
        {
            entity.Success(entity.Get<C>(), remove);
        }
    }

    public static void Success<C>(this Entity entity, C component, bool remove = true)
    {
        var world = entity.CsWorld();

        if (component is IAsync async)
        {
            async.SetResult(world);
        }

        if (remove)
        {
            entity.Remove<C>();
        }

        if (component is IInvoke invoke)
        {
            invoke.Invoke(entity);
        }
    }

    public static void Failure<C>(this Entity entity, Exception exception, bool remove = true)
    {
        if (entity.Has<C>())
        {
            entity.Failure(entity.Get<C>(), exception, remove);
        }
    }

    public static void Failure<C>(this Entity entity, C component, Exception exception, bool remove = true)
    {
        var world = entity.CsWorld();

        if (remove)
        {
            entity.Remove<C>();
        }

        if (component is IAsync async)
        {
            async.SetException(world, exception);
        }
    }
}

public class Asyncs
{
    private List<IAsync> Tasks = new List<IAsync>();

    public void Add(IAsync task)
    {
        Tasks.Add(task);
    }

    public void Async()
    {
        foreach (var task in Tasks)
        {
            task.Async();
        }

        Tasks.Clear();
    }
}