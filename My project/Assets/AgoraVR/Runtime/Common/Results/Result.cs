namespace AgoraVR.Common.Results
{

public readonly struct Result
{
    private Result(bool isSuccess, ErrorCode errorCode, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ErrorCode ErrorCode { get; }

    public string ErrorMessage { get; }

    public static Result Success()
    {
        return new Result(true, ErrorCode.None, string.Empty);
    }

    public static Result Failure(ErrorCode errorCode, string errorMessage)
    {
        return new Result(false, errorCode, errorMessage);
    }
}
}
