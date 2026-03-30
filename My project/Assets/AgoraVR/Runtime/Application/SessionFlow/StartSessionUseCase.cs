using AgoraVR.Application.Abstractions;
using AgoraVR.Common.IDs;
using AgoraVR.Common.Logging;
using AgoraVR.Common.Results;
using AgoraVR.Common.Time;
using AgoraVR.Domain.Session;

namespace AgoraVR.Application.SessionFlow
{

public sealed class StartSessionUseCase
{
    private readonly IAppLogger _logger;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ITopicCatalog _topicCatalog;

    public StartSessionUseCase(
        ISessionRepository sessionRepository,
        ITopicCatalog topicCatalog,
        ITimeProvider timeProvider,
        IAppLogger logger)
    {
        _sessionRepository = sessionRepository;
        _topicCatalog = topicCatalog;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Result<SessionAttempt> Execute(StartSessionRequest request)
    {
        Result<AgoraVR.Domain.Topic.TopicDefinition> topicResult =
            string.IsNullOrWhiteSpace(request.TopicId)
                ? _topicCatalog.GetDefaultTopic()
                : _topicCatalog.GetById(request.TopicId);

        if (topicResult.IsFailure)
        {
            return Result<SessionAttempt>.Failure(topicResult.ErrorCode, topicResult.ErrorMessage);
        }

        SessionAttempt attempt = new SessionAttempt(
            SessionId.CreateNew(),
            topicResult.Value,
            attemptIndex: 1,
            createdAtUtc: _timeProvider.UtcNow);

        Result saveResult = _sessionRepository.Save(attempt);
        if (saveResult.IsFailure)
        {
            return Result<SessionAttempt>.Failure(saveResult.ErrorCode, saveResult.ErrorMessage);
        }

        _logger.LogInfo($"Started session {attempt.SessionId} for topic {attempt.Topic.Id}.");
        return Result<SessionAttempt>.Success(attempt);
    }
}
}
