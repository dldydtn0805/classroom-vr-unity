using System.Text;
using AgoraVR.Common.Results;
using AgoraVR.Domain.Topic;

namespace AgoraVR.Infrastructure.Audio
{
    public sealed class DummyAudioTranscriptionService
    {
        public Result<TranscribedAudioSegment> TranscribeAnswer(RecordedAudioSegment segment, string questionPrompt, int questionIndex)
        {
            if (segment == null)
            {
                return Result<TranscribedAudioSegment>.Failure(ErrorCode.InvalidArgument, "Audio segment is missing.");
            }

            string transcriptText = BuildAnswerTranscript(segment, questionPrompt, questionIndex);
            return Result<TranscribedAudioSegment>.Success(new TranscribedAudioSegment(segment, transcriptText, "Dummy STT"));
        }

        public Result<TranscribedAudioSegment> TranscribePresentation(RecordedAudioSegment segment, TopicDefinition topic)
        {
            if (segment == null)
            {
                return Result<TranscribedAudioSegment>.Failure(ErrorCode.InvalidArgument, "Audio segment is missing.");
            }

            if (topic == null)
            {
                return Result<TranscribedAudioSegment>.Failure(ErrorCode.InvalidArgument, "Topic is missing.");
            }

            string transcriptText = BuildPresentationTranscript(segment, topic);
            return Result<TranscribedAudioSegment>.Success(new TranscribedAudioSegment(segment, transcriptText, "Dummy STT"));
        }

        private static string BuildAnswerTranscript(RecordedAudioSegment segment, string questionPrompt, int questionIndex)
        {
            if (segment.DurationSeconds < 1.5f)
            {
                return "Short answer detected. The speaker likely gave a brief placeholder response and should expand with one reason and one example.";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Question ");
            builder.Append(questionIndex + 1);
            builder.Append(": ");
            builder.Append(string.IsNullOrWhiteSpace(questionPrompt) ? "Follow-up prompt unavailable." : questionPrompt);
            builder.Append(" ");
            builder.Append("Transcript preview: The speaker responds with a direct claim, adds supporting context, and closes with a practical example to steady the answer.");

            if (segment.DurationSeconds >= 8f)
            {
                builder.Append(" The pacing suggests enough room for a stronger structure, but the core idea is present.");
            }

            return builder.ToString();
        }

        private static string BuildPresentationTranscript(RecordedAudioSegment segment, TopicDefinition topic)
        {
            if (segment.DurationSeconds < 2f)
            {
                return "Very short presentation detected. The opening likely did not reach a full thesis yet.";
            }

            switch (topic.Category)
            {
                case TopicCategory.Interview:
                    return "Transcript preview: I would describe my strength as reliable collaboration. In team projects I usually organize the work, keep communication clear, and help the group stay calm when priorities change.";
                case TopicCategory.Debate:
                    return "Transcript preview: I support remote work as the default for many knowledge workers because it improves focus, widens hiring access, and gives teams more flexibility when they coordinate intentionally.";
                default:
                    return "Transcript preview: Technology has improved everyday communication overall because it makes contact faster, lowers distance barriers, and gives people more ways to stay connected across daily life.";
            }
        }
    }
}
