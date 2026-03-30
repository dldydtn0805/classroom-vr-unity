using System;
using AgoraVR.Common.IDs;
using AgoraVR.Common.Results;
using AgoraVR.Domain.Topic;

namespace AgoraVR.Domain.Session
{

public sealed class SessionAttempt
{
    public SessionAttempt(
        SessionId sessionId,
        TopicDefinition topic,
        int attemptIndex,
        DateTimeOffset createdAtUtc)
    {
        SessionId = sessionId;
        Topic = topic;
        AttemptIndex = attemptIndex;
        CurrentStage = SessionStage.TopicReady;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public SessionId SessionId { get; }

    public TopicDefinition Topic { get; }

    public int AttemptIndex { get; }

    public SessionStage CurrentStage { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Result AdvanceTo(SessionStage nextStage, DateTimeOffset changedAtUtc)
    {
        if (!IsAllowedTransition(CurrentStage, nextStage))
        {
            return Result.Failure(
                ErrorCode.InvalidStateTransition,
                $"Cannot transition session from {CurrentStage} to {nextStage}.");
        }

        CurrentStage = nextStage;
        UpdatedAtUtc = changedAtUtc;
        return Result.Success();
    }

    public SessionAttempt CreateRetry(SessionId newSessionId, DateTimeOffset createdAtUtc)
    {
        return new SessionAttempt(newSessionId, Topic, AttemptIndex + 1, createdAtUtc);
    }

    private static bool IsAllowedTransition(SessionStage currentStage, SessionStage nextStage)
    {
        return currentStage switch
        {
            SessionStage.TopicReady => nextStage == SessionStage.Preparation,
            SessionStage.Preparation => nextStage == SessionStage.PresentationRecording,
            SessionStage.PresentationRecording => nextStage == SessionStage.QuestionLoading,
            SessionStage.QuestionLoading => nextStage == SessionStage.AnswerRecording,
            SessionStage.AnswerRecording => nextStage == SessionStage.FeedbackLoading,
            SessionStage.FeedbackLoading => nextStage == SessionStage.FeedbackReview,
            SessionStage.FeedbackReview => nextStage == SessionStage.RetryBootstrap,
            SessionStage.RetryBootstrap => nextStage == SessionStage.TopicReady,
            _ => false
        };
    }
}
}
