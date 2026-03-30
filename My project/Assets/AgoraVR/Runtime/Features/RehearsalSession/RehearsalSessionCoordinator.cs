using AgoraVR.Application.Retry;
using AgoraVR.Application.SessionFlow;
using AgoraVR.Common.IDs;
using AgoraVR.Common.Logging;
using AgoraVR.Common.Results;
using AgoraVR.Domain.Session;

namespace AgoraVR.Features.RehearsalSession
{

public sealed class RehearsalSessionCoordinator
{
    private readonly AdvanceSessionStageUseCase _advanceSessionStageUseCase;
    private readonly IAppLogger _logger;
    private readonly RetrySessionUseCase _retrySessionUseCase;
    private readonly StartSessionUseCase _startSessionUseCase;

    public RehearsalSessionCoordinator(
        StartSessionUseCase startSessionUseCase,
        AdvanceSessionStageUseCase advanceSessionStageUseCase,
        RetrySessionUseCase retrySessionUseCase,
        IAppLogger logger)
    {
        _startSessionUseCase = startSessionUseCase;
        _advanceSessionStageUseCase = advanceSessionStageUseCase;
        _retrySessionUseCase = retrySessionUseCase;
        _logger = logger;
    }

    public SessionId CurrentSessionId { get; private set; }

    public Result<SessionAttempt> StartNewSession(string topicId)
    {
        Result<SessionAttempt> result = _startSessionUseCase.Execute(new StartSessionRequest(topicId));
        if (result.IsSuccess)
        {
            CurrentSessionId = result.Value.SessionId;
        }

        return result;
    }

    public Result<SessionAttempt> AdvanceTo(SessionStage nextStage)
    {
        if (CurrentSessionId.IsEmpty)
        {
            return Result<SessionAttempt>.Failure(
                ErrorCode.InvalidArgument,
                "Cannot advance a session before one has been started.");
        }

        return _advanceSessionStageUseCase.Execute(CurrentSessionId, nextStage);
    }

    public Result<SessionAttempt> RetryCurrentSession()
    {
        if (CurrentSessionId.IsEmpty)
        {
            return Result<SessionAttempt>.Failure(
                ErrorCode.InvalidArgument,
                "Cannot retry a session before one has been started.");
        }

        Result<SessionAttempt> retryResult = _retrySessionUseCase.Execute(CurrentSessionId);
        if (retryResult.IsSuccess)
        {
            CurrentSessionId = retryResult.Value.SessionId;
            _logger.LogInfo($"Switched current session to retry {CurrentSessionId}.");
        }

        return retryResult;
    }
}
}
