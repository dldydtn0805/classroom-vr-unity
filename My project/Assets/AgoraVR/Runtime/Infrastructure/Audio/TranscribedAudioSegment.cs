namespace AgoraVR.Infrastructure.Audio
{
    public sealed class TranscribedAudioSegment
    {
        public TranscribedAudioSegment(RecordedAudioSegment audioSegment, string transcriptText, string engineName)
        {
            AudioSegment = audioSegment;
            TranscriptText = transcriptText ?? string.Empty;
            EngineName = engineName ?? string.Empty;
        }

        public RecordedAudioSegment AudioSegment { get; }

        public string TranscriptText { get; }

        public string EngineName { get; }
    }
}
