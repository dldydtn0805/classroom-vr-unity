using AgoraVR.Application.SessionFlow;
using AgoraVR.Application.Retry;
using AgoraVR.Common.Logging;
using AgoraVR.Common.Time;
using AgoraVR.Infrastructure.Logging;
using AgoraVR.Infrastructure.Persistence;
using AgoraVR.Infrastructure.TopicCatalog;
using UnityEngine;

namespace AgoraVR.Features.RehearsalSession
{

public sealed class RehearsalSessionInstaller : MonoBehaviour
{
    public RehearsalSessionCoordinator Coordinator { get; private set; }

    private void Awake()
    {
        IAppLogger logger = new UnityDebugLogger();
        ITimeProvider timeProvider = new SystemTimeProvider();
        InMemorySessionRepository sessionRepository = new InMemorySessionRepository();
        InMemoryTopicCatalog topicCatalog = new InMemoryTopicCatalog();

        StartSessionUseCase startSessionUseCase = new StartSessionUseCase(
            sessionRepository,
            topicCatalog,
            timeProvider,
            logger);

        AdvanceSessionStageUseCase advanceSessionStageUseCase = new AdvanceSessionStageUseCase(
            sessionRepository,
            timeProvider,
            logger);

        RetrySessionUseCase retrySessionUseCase = new RetrySessionUseCase(
            sessionRepository,
            timeProvider,
            logger);

        Coordinator = new RehearsalSessionCoordinator(
            startSessionUseCase,
            advanceSessionStageUseCase,
            retrySessionUseCase,
            logger);
    }
}
}
