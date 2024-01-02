public interface ITask
{

    public TaskCompletionSource Promise { get; }
    public Task Task { get; }
}