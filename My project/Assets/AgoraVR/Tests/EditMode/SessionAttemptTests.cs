using System;
using AgoraVR.Application.Retry;
using AgoraVR.Common.IDs;
using AgoraVR.Common.Logging;
using AgoraVR.Common.Time;
using AgoraVR.Domain.Session;
using AgoraVR.Domain.Topic;
using AgoraVR.Infrastructure.Persistence;
using NUnit.Framework;

namespace AgoraVR.Tests.EditMode
{

public sealed class SessionAttemptTests
{
    [Test]
    public void AdvanceTo_AllowsExpectedMvpStageOrder()
    {
        SessionAttempt attempt = CreateAttempt();
        DateTimeOffset now = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero);

        Assert.That(attempt.AdvanceTo(SessionStage.Preparation, now).IsSuccess, Is.True);
        Assert.That(attempt.AdvanceTo(SessionStage.PresentationRecording, now.AddSeconds(1)).IsSuccess, Is.True);
        Assert.That(attempt.AdvanceTo(SessionStage.QuestionLoading, now.AddSeconds(2)).IsSuccess, Is.True);
        Assert.That(attempt.AdvanceTo(SessionStage.AnswerRecording, now.AddSeconds(3)).IsSuccess, Is.True);
        Assert.That(attempt.AdvanceTo(SessionStage.FeedbackLoading, now.AddSeconds(4)).IsSuccess, Is.True);
        Assert.That(attempt.AdvanceTo(SessionStage.FeedbackReview, now.AddSeconds(5)).IsSuccess, Is.True);
        Assert.That(attempt.AdvanceTo(SessionStage.RetryBootstrap, now.AddSeconds(6)).IsSuccess, Is.True);
        Assert.That(attempt.AdvanceTo(SessionStage.TopicReady, now.AddSeconds(7)).IsSuccess, Is.True);
    }

    [Test]
    public void AdvanceTo_RejectsSkippedStages()
    {
        SessionAttempt attempt = CreateAttempt();
        DateTimeOffset now = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero);

        var result = attempt.AdvanceTo(SessionStage.AnswerRecording, now);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorMessage, Does.Contain("Cannot transition"));
        Assert.That(attempt.CurrentStage, Is.EqualTo(SessionStage.TopicReady));
    }

    [Test]
    public void RetrySessionUseCase_CreatesNewAttemptOnSameTopic()
    {
        InMemorySessionRepository repository = new InMemorySessionRepository();
        SessionAttempt originalAttempt = CreateAttempt();
        repository.Save(originalAttempt);

        RetrySessionUseCase useCase = new RetrySessionUseCase(
            repository,
            new FixedTimeProvider(new DateTimeOffset(2026, 3, 30, 10, 0, 0, TimeSpan.Zero)),
            new TestLogger());

        var retryResult = useCase.Execute(originalAttempt.SessionId);

        Assert.That(retryResult.IsSuccess, Is.True);
        Assert.That(retryResult.Value.SessionId, Is.Not.EqualTo(originalAttempt.SessionId));
        Assert.That(retryResult.Value.AttemptIndex, Is.EqualTo(originalAttempt.AttemptIndex + 1));
        Assert.That(retryResult.Value.Topic.Id, Is.EqualTo(originalAttempt.Topic.Id));
        Assert.That(retryResult.Value.CurrentStage, Is.EqualTo(SessionStage.TopicReady));
    }

    private static SessionAttempt CreateAttempt()
    {
        return new SessionAttempt(
            SessionId.CreateNew(),
            new TopicDefinition(
                "interview_strength",
                "Interview Strengths",
                TopicCategory.Interview,
                DifficultyLevel.Easy,
                "Describe one of your strengths and explain why it matters in a team."),
            attemptIndex: 1,
            createdAtUtc: new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero));
    }

    private sealed class FixedTimeProvider : ITimeProvider
    {
        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class TestLogger : IAppLogger
    {
        public void LogError(string message)
        {
        }

        public void LogInfo(string message)
        {
        }

        public void LogWarning(string message)
        {
        }
    }
}
}
