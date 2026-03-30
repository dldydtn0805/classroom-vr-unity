using AgoraVR.Application.Abstractions;
using AgoraVR.Common.IDs;
using AgoraVR.Common.Logging;
using AgoraVR.Common.Results;
using AgoraVR.Common.Time;
using AgoraVR.Domain.Session;

namespace AgoraVR.Application.SessionFlow
{

public sealed class AdvanceSessionStageUseCase
{
    private readonly IAppLogger _logger;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITimeProvider _timeProvider;

    public AdvanceSessionStageUseCase(
        ISessionRepository sessionRepository,
        ITimeProvider timeProvider,
        IAppLogger logger)
    {
        _sessionRepository = sessionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Result<SessionAttempt> Execute(SessionId sessionId, SessionStage nextStage)
    {
        Result<SessionAttempt> sessionResult = _sessionRepository.GetById(sessionId);
        if (sessionResult.IsFailure)
        {
            return sessionResult;
        }

        SessionAttempt attempt = sessionResult.Value;
        Result transitionResult = attempt.AdvanceTo(nextStage, _timeProvider.UtcNow);
        if (transitionResult.IsFailure)
        {
            return Result<SessionAttempt>.Failure(transitionResult.ErrorCode, transitionResult.ErrorMessage);
        }

        Result saveResult = _sessionRepository.Save(attempt);
        if (saveResult.IsFailure)
        {
            return Result<SessionAttempt>.Failure(saveResult.ErrorCode, saveResult.ErrorMessage);
        }

        _logger.LogInfo($"Advanced session {sessionId} to {nextStage}.");
        return Result<SessionAttempt>.Success(attempt);
    }
}
}
