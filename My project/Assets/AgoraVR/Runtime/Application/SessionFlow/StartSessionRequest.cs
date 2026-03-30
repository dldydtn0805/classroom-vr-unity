namespace AgoraVR.Application.SessionFlow
{

public readonly struct StartSessionRequest
{
    public StartSessionRequest(string topicId)
    {
        TopicId = topicId;
    }

    public string TopicId { get; }
}
}
