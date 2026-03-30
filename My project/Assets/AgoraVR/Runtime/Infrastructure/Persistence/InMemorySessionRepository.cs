using System.Collections.Generic;
using AgoraVR.Application.Abstractions;
using AgoraVR.Common.IDs;
using AgoraVR.Common.Results;
using AgoraVR.Domain.Session;

namespace AgoraVR.Infrastructure.Persistence
{

public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly Dictionary<string, SessionAttempt> _storage = new Dictionary<string, SessionAttempt>();

    public Result Save(SessionAttempt attempt)
    {
        if (attempt == null || attempt.SessionId.IsEmpty)
        {
            return Result.Failure(ErrorCode.InvalidArgument, "Session attempt is invalid.");
        }

        _storage[attempt.SessionId.Value] = attempt;
        return Result.Success();
    }

    public Result<SessionAttempt> GetById(SessionId sessionId)
    {
        if (sessionId.IsEmpty)
        {
            return Result<SessionAttempt>.Failure(ErrorCode.InvalidArgument, "Session id is empty.");
        }

        if (!_storage.TryGetValue(sessionId.Value, out SessionAttempt attempt))
        {
            return Result<SessionAttempt>.Failure(
                ErrorCode.NotFound,
                $"Session {sessionId} was not found.");
        }

        return Result<SessionAttempt>.Success(attempt);
    }
}
}
