namespace AgoraVR.Common.Logging
{

public interface IAppLogger
{
    void LogInfo(string message);

    void LogWarning(string message);

    void LogError(string message);
}
}
