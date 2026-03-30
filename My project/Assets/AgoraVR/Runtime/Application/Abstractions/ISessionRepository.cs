using AgoraVR.Common.IDs;
using AgoraVR.Common.Results;
using AgoraVR.Domain.Session;

namespace AgoraVR.Application.Abstractions
{

public interface ISessionRepository
{
    Result Save(SessionAttempt attempt);

    Result<SessionAttempt> GetById(SessionId sessionId);
}
}
