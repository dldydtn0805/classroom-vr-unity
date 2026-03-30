using System.Collections.Generic;

namespace AgoraVR.Domain.Feedback
{

public sealed class FeedbackReport
{
    public FeedbackReport(
        IReadOnlyList<string> strengths,
        IReadOnlyList<string> improvements,
        string retryTask)
    {
        Strengths = strengths ?? new List<string>();
        Improvements = improvements ?? new List<string>();
        RetryTask = retryTask ?? string.Empty;
    }

    public IReadOnlyList<string> Strengths { get; }

    public IReadOnlyList<string> Improvements { get; }

    public string RetryTask { get; }
}
}
