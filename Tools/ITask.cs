public interface ITask
{

    public TaskCompletionSource Promise { get; }
    public Task Task { get; }
    public void SetException(Exception exception);
}

public interface ITask<T>
{
    public TaskCompletionSource<T> Promise { get; }
    public Task<T> Task { get; }
    public void SetException(Exception exception);
}