using Flecs.NET.Core;

public enum TaskStatus
{
    Running,
    Success,
    Failed
}

public partial class Command : BootstrapNode2D, ITask
{
    public readonly TaskCompletionSource _Promise = new TaskCompletionSource();

    public TaskStatus TaskStatus = TaskStatus.Running;

    public Exception? Exception = null;

    public TaskCompletionSource Promise { get => _Promise; }

    public Task Task { get => Promise.Task; }

    public bool TryNotify()
    {
        if (Exception == null)
        {
            return _Promise.TrySetResult();
        }
        else
        {
            return _Promise.TrySetException(Exception);
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

        if (command.TaskStatus == TaskStatus.Running)
        {
            command.TaskStatus = TaskStatus.Success;
            world.Get<Tasks>().Commands.Add(command);
        }
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

        if (command.TaskStatus == TaskStatus.Running)
        {
            command.Exception = exception;
            command.TaskStatus = TaskStatus.Failed;
            world.Get<Tasks>().Commands.Add(command);
        }
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