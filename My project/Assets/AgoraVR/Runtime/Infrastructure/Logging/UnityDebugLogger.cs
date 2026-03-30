using AgoraVR.Common.Logging;
using UnityEngine;

namespace AgoraVR.Infrastructure.Logging
{

public sealed class UnityDebugLogger : IAppLogger
{
    public void LogInfo(string message)
    {
        Debug.Log($"[AgoraVR] {message}");
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning($"[AgoraVR] {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"[AgoraVR] {message}");
    }
}
}
