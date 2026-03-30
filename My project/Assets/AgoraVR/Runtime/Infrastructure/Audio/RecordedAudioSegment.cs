using System;

namespace AgoraVR.Infrastructure.Audio
{
    public sealed class RecordedAudioSegment
    {
        public RecordedAudioSegment(
            string label,
            string stageName,
            string filePath,
            float durationSeconds,
            int sampleCount,
            int sampleRate,
            int channelCount,
            DateTimeOffset recordedAtUtc)
        {
            Label = label ?? string.Empty;
            StageName = stageName ?? string.Empty;
            FilePath = filePath ?? string.Empty;
            DurationSeconds = durationSeconds;
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            ChannelCount = channelCount;
            RecordedAtUtc = recordedAtUtc;
        }

        public string Label { get; }

        public string StageName { get; }

        public string FilePath { get; }

        public float DurationSeconds { get; }

        public int SampleCount { get; }

        public int SampleRate { get; }

        public int ChannelCount { get; }

        public DateTimeOffset RecordedAtUtc { get; }
    }
}
