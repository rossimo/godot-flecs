using Godot;
using Flecs.NET.Core;
using System.ComponentModel.Design;

public partial class Command : BootstrapNode2D
{
    public readonly TaskCompletionSource Promise = new TaskCompletionSource();

    public Task Task
    {
        get
        {
            return Promise.Task;
        }
    }

    public override void Complete(Entity entity)
    {
        base.Complete(entity);

        if (!Promise.Task.IsCompleted)
        {
            Promise.SetResult();
        }
    }
}

public class ComponentRemovedException : Exception
{

}