namespace AgoraVR.Common.Results
{

public readonly struct Result<T>
{
    private Result(bool isSuccess, T value, ErrorCode errorCode, string errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value { get; }

    public ErrorCode ErrorCode { get; }

    public string ErrorMessage { get; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, ErrorCode.None, string.Empty);
    }

    public static Result<T> Failure(ErrorCode errorCode, string errorMessage)
    {
        return new Result<T>(false, default, errorCode, errorMessage);
    }
}
}
