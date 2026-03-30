using AgoraVR.Application.Abstractions;
using AgoraVR.Common.IDs;
using AgoraVR.Common.Logging;
using AgoraVR.Common.Results;
using AgoraVR.Common.Time;
using AgoraVR.Domain.Session;

namespace AgoraVR.Application.Retry
{

public sealed class RetrySessionUseCase
{
    private readonly IAppLogger _logger;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITimeProvider _timeProvider;

    public RetrySessionUseCase(
        ISessionRepository sessionRepository,
        ITimeProvider timeProvider,
        IAppLogger logger)
    {
        _sessionRepository = sessionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Result<SessionAttempt> Execute(SessionId currentSessionId)
    {
        Result<SessionAttempt> sessionResult = _sessionRepository.GetById(currentSessionId);
        if (sessionResult.IsFailure)
        {
            return sessionResult;
        }

        SessionAttempt retryAttempt = sessionResult.Value.CreateRetry(SessionId.CreateNew(), _timeProvider.UtcNow);
        Result saveResult = _sessionRepository.Save(retryAttempt);
        if (saveResult.IsFailure)
        {
            return Result<SessionAttempt>.Failure(saveResult.ErrorCode, saveResult.ErrorMessage);
        }

        _logger.LogInfo(
            $"Created retry session {retryAttempt.SessionId} from session {currentSessionId}.");
        return Result<SessionAttempt>.Success(retryAttempt);
    }
}
}
