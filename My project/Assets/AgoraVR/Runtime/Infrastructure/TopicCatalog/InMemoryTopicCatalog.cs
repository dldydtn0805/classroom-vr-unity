using System.Collections.Generic;
using AgoraVR.Application.Abstractions;
using AgoraVR.Common.Results;
using AgoraVR.Domain.Topic;

namespace AgoraVR.Infrastructure.TopicCatalog
{

public sealed class InMemoryTopicCatalog : ITopicCatalog
{
    private readonly Dictionary<string, TopicDefinition> _topics = new Dictionary<string, TopicDefinition>
    {
        {
            "interview_strength",
            new TopicDefinition(
                "interview_strength",
                "Interview Strengths",
                TopicCategory.Interview,
                DifficultyLevel.Easy,
                "Describe one of your strengths and explain why it matters in a team.")
        },
        {
            "speech_technology",
            new TopicDefinition(
                "speech_technology",
                "Technology and Daily Life",
                TopicCategory.Speech,
                DifficultyLevel.Normal,
                "Argue whether technology has improved everyday communication overall.")
        },
        {
            "debate_remote_work",
            new TopicDefinition(
                "debate_remote_work",
                "Remote Work Debate",
                TopicCategory.Debate,
                DifficultyLevel.Hard,
                "Take a position on whether remote work should remain the default for knowledge workers.")
        }
    };

    public Result<TopicDefinition> GetDefaultTopic()
    {
        return GetById("interview_strength");
    }

    public Result<TopicDefinition> GetById(string topicId)
    {
        if (string.IsNullOrWhiteSpace(topicId))
        {
            return Result<TopicDefinition>.Failure(ErrorCode.InvalidArgument, "Topic id is empty.");
        }

        if (!_topics.TryGetValue(topicId, out TopicDefinition topic))
        {
            return Result<TopicDefinition>.Failure(
                ErrorCode.NotFound,
                $"Topic {topicId} was not found.");
        }

        return Result<TopicDefinition>.Success(topic);
    }
}
}
