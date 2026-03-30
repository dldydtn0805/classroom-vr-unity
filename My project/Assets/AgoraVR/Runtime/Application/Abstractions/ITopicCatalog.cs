using AgoraVR.Common.Results;
using AgoraVR.Domain.Topic;

namespace AgoraVR.Application.Abstractions
{

public interface ITopicCatalog
{
    Result<TopicDefinition> GetDefaultTopic();

    Result<TopicDefinition> GetById(string topicId);
}
}
