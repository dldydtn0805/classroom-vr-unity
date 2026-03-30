namespace AgoraVR.Domain.Topic
{

public sealed class TopicDefinition
{
    public TopicDefinition(string id, string title, TopicCategory category, DifficultyLevel difficulty, string prompt)
    {
        Id = id;
        Title = title;
        Category = category;
        Difficulty = difficulty;
        Prompt = prompt;
    }

    public string Id { get; }

    public string Title { get; }

    public TopicCategory Category { get; }

    public DifficultyLevel Difficulty { get; }

    public string Prompt { get; }
}
}
