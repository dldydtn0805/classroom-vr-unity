using System;

namespace AgoraVR.Common.Time
{

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}
}
