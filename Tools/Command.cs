using Godot;
using Flecs.NET.Core;

public partial class Command : BootstrapNode2D, ITask
{
    public readonly TaskCompletionSource _Promise = new TaskCompletionSource();

    public TaskCompletionSource Promise { get => _Promise; }

    public Task Task { get => Promise.Task; }

    public override void Invoke(Entity entity)
    {
        base.Invoke(entity);

        if (!Promise.Task.IsCompleted)
        {
            Promise.SetResult();
        }
    }

    public  void SetException(Exception exception)
    {
        Promise.SetException(exception);
    }
}

public class ComponentRemovedException : Exception
{

}