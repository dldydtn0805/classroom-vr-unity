namespace AgoraVR.Domain.Session
{

public enum SessionStage
{
    Idle = 0,
    TopicReady = 1,
    Preparation = 2,
    PresentationRecording = 3,
    QuestionLoading = 4,
    AnswerRecording = 5,
    FeedbackLoading = 6,
    FeedbackReview = 7,
    RetryBootstrap = 8
}
}
