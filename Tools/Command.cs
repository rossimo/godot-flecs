using Flecs.NET.Core;

public partial class Command : BootstrapNode2D, ITask
{
    public TaskCompletionSource Promise { get => _Promise; }

    public Task Task { get => Promise.Task; }

    private TaskCompletionSource _Promise = new TaskCompletionSource();

    private bool Complete;

    private Exception? Exception = null;

    public void SetResult(World world)
    {
        if (!Complete)
        {
            Complete = true;
            world.Get<Tasks>().Add(this);
        }
    }

    public void SetException(World world, Exception exception)
    {
        if (!Complete)
        {
            Complete = true;
            Exception = exception;
            world.Get<Tasks>().Add(this);
        }
    }

    public void Yield()
    {
        if (Exception == null)
        {
            _Promise.SetResult();
        }
        else
        {
            _Promise.SetException(Exception);
        }
    }
}

public static class CommandUtils
{
    public static void Success<C>(this Entity entity, bool remove = true) where C : Command
    {
        if (entity.Has<C>())
        {
            entity.Success(entity.Get<C>(), remove);
        }
    }

    public static void Success<C>(this Entity entity, C command, bool remove = true) where C : Command
    {
        var world = entity.CsWorld();

        if (remove)
        {
            entity.Remove<C>();
        }

        command.Invoke(entity);
        command.SetResult(world);
    }

    public static void Failure<C>(this Entity entity, Exception exception, bool remove = true) where C : Command
    {
        if (entity.Has<C>())
        {
            entity.Failure(entity.Get<C>(), exception, remove);
        }
    }

    public static void Failure<C>(this Entity entity, C command, Exception exception, bool remove = true) where C : Command
    {
        var world = entity.CsWorld();

        if (remove)
        {
            entity.Remove<C>();
        }

        command.SetException(world, exception);
    }
}

public class ComponentRemovedException<C> : Exception
{
    public readonly Entity Entity;

    public ComponentRemovedException(Entity entity) : base($"{typeof(C)} has been removed from {entity}")
    {
        Entity = entity;
    }
}