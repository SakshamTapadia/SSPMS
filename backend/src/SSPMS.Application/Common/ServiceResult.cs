namespace SSPMS.Application.Common;

public class ServiceResult
{
    public bool Succeeded { get; private set; }
    public string? Error { get; private set; }

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Failure(string error) => new() { Succeeded = false, Error = error };
}

public class ServiceResult<T>
{
    public bool Succeeded { get; private set; }
    public string? Error { get; private set; }
    public T? Data { get; private set; }

    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static ServiceResult<T> Failure(string error) => new() { Succeeded = false, Error = error };
}
