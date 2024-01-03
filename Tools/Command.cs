using Flecs.NET.Core;

public partial class Command : BootstrapNode2D, IAsync
{
    public Task Task { get => Promise.Task; }

    private TaskCompletionSource Promise = new TaskCompletionSource();

    private bool Complete;

    private Exception? Exception = null;

    public void SetResult(World world)
    {
        if (!Complete)
        {
            Complete = true;
            world.Get<Asyncs>().Add(this);
        }
    }

    public void SetException(World world, Exception exception)
    {
        if (!Complete)
        {
            Complete = true;
            Exception = exception;
            world.Get<Asyncs>().Add(this);
        }
    }

    public void Async()
    {
        if (Exception == null)
        {
            Promise.SetResult();
        }
        else
        {
            Promise.SetException(Exception);
        }
    }
}
