namespace Launcher.Core.Services;

public enum FetchStatus
{
    Success,
    Failure
}

public class FetchResult<T>
{
    public FetchStatus Status { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static FetchResult<T> SuccessResult(T data) => new FetchResult<T> { Status = FetchStatus.Success, Data = data };
    public static FetchResult<T> FailureResult(string message) => new FetchResult<T> { Status = FetchStatus.Failure, ErrorMessage = message };
}
