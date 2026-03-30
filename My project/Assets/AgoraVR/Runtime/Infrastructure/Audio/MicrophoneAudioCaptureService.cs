using System;
using System.IO;
using AgoraVR.Common.Logging;
using AgoraVR.Common.Results;
using AgoraVR.Common.Time;
using UnityEngine;

namespace AgoraVR.Infrastructure.Audio
{
    public sealed class MicrophoneAudioCaptureService
    {
        private readonly IAppLogger _logger;
        private readonly ITimeProvider _timeProvider;

        private AudioClip _activeClip;
        private string _activeDeviceName = string.Empty;
        private string _activeFilePath = string.Empty;
        private int _activeMaxDurationSeconds;

        public MicrophoneAudioCaptureService(ITimeProvider timeProvider, IAppLogger logger)
        {
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public bool HasAvailableDevice => Microphone.devices != null && Microphone.devices.Length > 0;

        public bool IsCapturing => _activeClip != null;

        public Result BeginCapture(string filePath, int maxDurationSeconds, int preferredSampleRate = 16000)
        {
            if (IsCapturing)
            {
                return Result.Failure(ErrorCode.InvalidStateTransition, "A microphone capture is already in progress.");
            }

            if (!HasAvailableDevice)
            {
                return Result.Failure(ErrorCode.NotFound, "No microphone device is available.");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result.Failure(ErrorCode.InvalidArgument, "Capture file path is empty.");
            }

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            _activeDeviceName = Microphone.devices[0];
            _activeMaxDurationSeconds = Mathf.Max(1, maxDurationSeconds);
            _activeFilePath = filePath;

            _activeClip = Microphone.Start(_activeDeviceName, false, _activeMaxDurationSeconds, preferredSampleRate);
            if (_activeClip == null)
            {
                ResetActiveCapture();
                return Result.Failure(ErrorCode.Unknown, "Unity did not return an audio clip for the microphone capture.");
            }

            _logger.LogInfo($"Started microphone capture on {_activeDeviceName} -> {filePath}.");
            return Result.Success();
        }

        public void CancelCapture()
        {
            if (!IsCapturing)
            {
                return;
            }

            Microphone.End(_activeDeviceName);
            _logger.LogWarning("Cancelled microphone capture before saving a segment.");
            ResetActiveCapture();
        }

        public Result<RecordedAudioSegment> FinishCapture(string label, string stageName)
        {
            if (!IsCapturing)
            {
                return Result<RecordedAudioSegment>.Failure(
                    ErrorCode.InvalidStateTransition,
                    "Cannot finish a microphone capture before one has started.");
            }

            int sampleCount = Microphone.GetPosition(_activeDeviceName);
            if (sampleCount < 0)
            {
                sampleCount = 0;
            }

            AudioClip clip = _activeClip;
            string deviceName = _activeDeviceName;
            string filePath = _activeFilePath;

            Microphone.End(deviceName);

            if (sampleCount <= 0)
            {
                ResetActiveCapture();
                return Result<RecordedAudioSegment>.Failure(
                    ErrorCode.Unknown,
                    "No microphone samples were captured. Check microphone access and try again.");
            }

            int channelCount = clip.channels;
            int clampedSampleCount = Mathf.Min(sampleCount, clip.samples);
            float[] sampleBuffer = new float[clampedSampleCount * channelCount];
            clip.GetData(sampleBuffer, 0);

            WavFileWriter.Write(filePath, sampleBuffer, channelCount, clip.frequency);

            RecordedAudioSegment segment = new RecordedAudioSegment(
                label,
                stageName,
                filePath,
                (float)clampedSampleCount / clip.frequency,
                clampedSampleCount,
                clip.frequency,
                channelCount,
                _timeProvider.UtcNow);

            _logger.LogInfo($"Saved microphone segment {label} ({segment.DurationSeconds:F1}s) to {filePath}.");
            ResetActiveCapture();
            return Result<RecordedAudioSegment>.Success(segment);
        }

        private void ResetActiveCapture()
        {
            _activeClip = null;
            _activeDeviceName = string.Empty;
            _activeFilePath = string.Empty;
            _activeMaxDurationSeconds = 0;
        }
    }
}
