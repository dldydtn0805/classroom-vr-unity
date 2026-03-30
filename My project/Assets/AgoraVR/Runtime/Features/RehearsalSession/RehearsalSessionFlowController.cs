using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AgoraVR.Common.Logging;
using AgoraVR.Common.Results;
using AgoraVR.Common.Time;
using AgoraVR.Domain.Session;
using AgoraVR.Domain.Topic;
using AgoraVR.Infrastructure.Audio;
using AgoraVR.Infrastructure.Logging;
using AgoraVR.Presentation.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace AgoraVR.Features.RehearsalSession
{
    public sealed class RehearsalSessionFlowController : MonoBehaviour
    {
        [Serializable]
        private struct TopicOption
        {
            public string Id;
            public string Label;
        }

        private enum CountdownMode
        {
            None = 0,
            Preparation = 1,
            PresentationRecording = 2,
            QuestionLoading = 3,
            AnswerRecording = 4,
            FeedbackLoading = 5
        }

        private enum RecordingMode
        {
            None = 0,
            Presentation = 1,
            Answer = 2
        }

        [Header("Dependencies")]
        [SerializeField] private RehearsalSessionInstaller installer;

        [Header("Durations")]
        [SerializeField] private float preparationDurationSeconds = 45f;
        [SerializeField] private float presentationDurationSeconds = 75f;
        [SerializeField] private float questionLoadingSeconds = 2f;
        [SerializeField] private float answerDurationSeconds = 40f;
        [SerializeField] private float feedbackLoadingSeconds = 2f;

        [Header("Topics")]
        [SerializeField] private TopicOption[] topicOptions =
        {
            new TopicOption { Id = "interview_strength", Label = "Interview Strengths" },
            new TopicOption { Id = "speech_technology", Label = "Technology and Daily Life" },
            new TopicOption { Id = "debate_remote_work", Label = "Remote Work Debate" }
        };

        private readonly List<RecordedAudioSegment> _answerSegments = new List<RecordedAudioSegment>();
        private readonly List<TranscribedAudioSegment> _answerTranscripts = new List<TranscribedAudioSegment>();

        private MicrophoneAudioCaptureService _audioCaptureService;
        private CountdownMode _countdownMode;
        private float _countdownRemainingSeconds;
        private SessionAttempt _currentAttempt;
        private int _currentQuestionIndex;
        private RehearsalSessionDebugHud _hud;
        private bool _isLoadingPlayback;
        private IAppLogger _logger;
        private AudioSource _playbackSource;
        private Coroutine _playbackCoroutine;
        private RecordedAudioSegment _presentationSegment;
        private TranscribedAudioSegment _presentationTranscript;
        private RecordingMode _recordingMode;
        private string _recordingStatus = string.Empty;
        private DummyAudioTranscriptionService _transcriptionService;
        private string[] _questions = Array.Empty<string>();

        private void Awake()
        {
            if (installer == null && !TryGetComponent(out installer))
            {
                installer = gameObject.AddComponent<RehearsalSessionInstaller>();
            }

            _logger = new UnityDebugLogger();
            _audioCaptureService = new MicrophoneAudioCaptureService(new SystemTimeProvider(), _logger);
            _transcriptionService = new DummyAudioTranscriptionService();
            _hud = RehearsalSessionDebugHud.CreateIfMissing();
            _playbackSource = gameObject.AddComponent<AudioSource>();
            _playbackSource.playOnAwake = false;
            _playbackSource.spatialBlend = 0f;
            CreatePracticeRoomIfMissing();
        }

        private void Start()
        {
            ShowTopicSelection();
        }

        private void Update()
        {
            if (_countdownMode == CountdownMode.None)
            {
                return;
            }

            _countdownRemainingSeconds = Mathf.Max(0f, _countdownRemainingSeconds - Time.deltaTime);
            _hud.SetRemainingSeconds(_countdownRemainingSeconds);

            if (_countdownRemainingSeconds > 0f)
            {
                return;
            }

            CountdownMode completedMode = _countdownMode;
            _countdownMode = CountdownMode.None;

            switch (completedMode)
            {
                case CountdownMode.Preparation:
                    BeginPresentation();
                    break;
                case CountdownMode.PresentationRecording:
                    StopPresentationRecording();
                    break;
                case CountdownMode.QuestionLoading:
                    BeginAnswerRound();
                    break;
                case CountdownMode.AnswerRecording:
                    StopCurrentAnswerRecording();
                    break;
                case CountdownMode.FeedbackLoading:
                    ShowFeedbackReview();
                    break;
            }
        }

        private void BeginAnswerRound()
        {
            AdvanceToStage(
                SessionStage.AnswerRecording,
                attempt =>
                {
                    _currentAttempt = attempt;
                    _currentQuestionIndex = 0;
                    ShowCurrentQuestionPrompt();
                });
        }

        private void BeginFeedbackLoading()
        {
            AdvanceToStage(
                SessionStage.FeedbackLoading,
                attempt =>
                {
                    _currentAttempt = attempt;
                    _countdownMode = CountdownMode.FeedbackLoading;
                    _countdownRemainingSeconds = feedbackLoadingSeconds;
                    _recordingStatus = "Coach summarizing your saved rehearsal segments.";

                    _hud.ShowScreen(
                        "Generating Feedback",
                        "The coach is stitching together your recorded presentation, answer clips, and one concrete retry focus.",
                        BuildSessionSummary(),
                        _countdownRemainingSeconds,
                        new RehearsalSessionDebugHud.ActionButtonData("Skip Wait", ShowFeedbackReview));
                });
        }

        private void BeginPreparation()
        {
            AdvanceToStage(
                SessionStage.Preparation,
                attempt =>
                {
                    _currentAttempt = attempt;
                    _countdownMode = CountdownMode.Preparation;
                    _countdownRemainingSeconds = preparationDurationSeconds;
                    _recordingStatus = "Preparation timer is running.";

                    _hud.ShowScreen(
                        "Preparation",
                        $"Topic: {_currentAttempt.Topic.Title}\n\n{_currentAttempt.Topic.Prompt}\n\nTake a beat, structure your opening, and get ready to record the full presentation.",
                        BuildSessionSummary(),
                        _countdownRemainingSeconds,
                        new RehearsalSessionDebugHud.ActionButtonData("Start Presentation Now", BeginPresentation));
                });
        }

        private void BeginPresentation()
        {
            AdvanceToStage(
                SessionStage.PresentationRecording,
                attempt =>
                {
                    _currentAttempt = attempt;
                    _recordingStatus = "Presentation stage is live. Start the microphone when you are ready.";
                    ShowPresentationReadyScreen();
                });
        }

        private void BeginQuestionLoading()
        {
            AdvanceToStage(
                SessionStage.QuestionLoading,
                attempt =>
                {
                    _currentAttempt = attempt;
                    _countdownMode = CountdownMode.QuestionLoading;
                    _countdownRemainingSeconds = questionLoadingSeconds;
                    _recordingStatus = "The room is preparing your follow-up questions.";

                    _hud.ShowScreen(
                        "Audience Thinking",
                        "The room goes quiet while the next questions are prepared from your recorded talk.",
                        BuildSessionSummary(),
                        _countdownRemainingSeconds,
                        new RehearsalSessionDebugHud.ActionButtonData("Show Questions", BeginAnswerRound));
                });
        }

        private string BuildAnswerButtonLabel()
        {
            return _currentQuestionIndex < _questions.Length - 1 ? "Skip To Next Question" : "Finish Without Recording";
        }

        private string BuildCaptureDirectory()
        {
            return Path.Combine(
                UnityEngine.Application.persistentDataPath,
                "AgoraVR",
                "RehearsalCaptures",
                _currentAttempt.SessionId.Value,
                $"attempt-{_currentAttempt.AttemptIndex}");
        }

        private string BuildFeedbackBody()
        {
            string firstQuestion = _questions.Length > 0 ? _questions[0] : "How clear is your main claim?";

            return
                $"Saved presentation clip: {BuildSegmentLine(_presentationSegment)}\n" +
                $"Saved answer clips: {Mathf.Max(0, _answerSegments.Count)}\n{BuildAnswerSegmentLines()}\n\n" +
                $"Transcript snapshot:\n{BuildTranscriptSummary()}\n\n" +
                $"Pressure point:\n- The first question still sets the tone: \"{firstQuestion}\".\n\n" +
                $"Retry focus:\n- In your next run, answer with a one-sentence claim, one reason, and one concrete example before expanding.";
        }

        private string BuildSegmentLine(RecordedAudioSegment segment)
        {
            if (segment == null)
            {
                return "none";
            }

            return $"{segment.Label} ({segment.DurationSeconds:F1}s, {Path.GetFileName(segment.FilePath)})";
        }

        private string BuildAnswerSegmentLines()
        {
            if (_answerSegments.Count == 0)
            {
                return "- none";
            }

            string lines = string.Empty;
            for (int i = 0; i < _answerSegments.Count; i++)
            {
                lines += $"- {BuildSegmentLine(_answerSegments[i])}";
                if (i < _answerSegments.Count - 1)
                {
                    lines += "\n";
                }
            }

            return lines;
        }

        private string BuildRecordingFilePath(string fileName)
        {
            return Path.Combine(BuildCaptureDirectory(), fileName);
        }

        private string BuildTranscriptSummary()
        {
            string presentationTranscript = _presentationTranscript == null
                ? "- Presentation: pending"
                : $"- Presentation ({_presentationTranscript.EngineName}): {_presentationTranscript.TranscriptText}";

            string answerTranscripts = BuildAnswerTranscriptLines();
            return $"{presentationTranscript}\n{answerTranscripts}";
        }

        private string BuildAnswerTranscriptLines()
        {
            if (_answerTranscripts.Count == 0)
            {
                return "- Answers: pending";
            }

            string lines = string.Empty;
            for (int i = 0; i < _answerTranscripts.Count; i++)
            {
                lines += $"- {_answerTranscripts[i].AudioSegment.Label} ({_answerTranscripts[i].EngineName}): {_answerTranscripts[i].TranscriptText}";
                if (i < _answerTranscripts.Count - 1)
                {
                    lines += "\n";
                }
            }

            return lines;
        }

        private string BuildSessionSummary()
        {
            if (_currentAttempt == null)
            {
                return "No active session.";
            }

            string segmentSummary =
                $"Saved Clips: {(_presentationSegment == null ? 0 : 1) + _answerSegments.Count}\n" +
                $"Transcripts: {(_presentationTranscript == null ? 0 : 1) + _answerTranscripts.Count}\n" +
                $"Mic: {(_audioCaptureService.HasAvailableDevice ? "Ready" : "Unavailable")}\n" +
                $"Playback: {BuildPlaybackStatus()}\n" +
                $"Status: {_recordingStatus}";

            return
                $"Session {_currentAttempt.SessionId}\n" +
                $"Attempt {_currentAttempt.AttemptIndex} · {_currentAttempt.Topic.Title}\n" +
                $"Stage: {_currentAttempt.CurrentStage}\n" +
                $"{segmentSummary}";
        }

        private string[] BuildQuestions(TopicDefinition topic)
        {
            switch (topic.Category)
            {
                case TopicCategory.Interview:
                    return new[]
                    {
                        "Can you ground that strength in a specific team situation?",
                        "What would a skeptical interviewer say is the weakness behind that strength?",
                        "Why should this answer matter to the role you want?"
                    };
                case TopicCategory.Debate:
                    return new[]
                    {
                        "What is the strongest counterargument to your position?",
                        "Which real-world tradeoff makes your stance harder to defend?",
                        "Why should an undecided audience member move toward your view?"
                    };
                default:
                    return new[]
                    {
                        "What is the clearest one-sentence version of your thesis?",
                        "Which example best proves your point in everyday life?",
                        "Why should the audience care about this beyond convenience?"
                    };
            }
        }

        private void CreatePracticeRoomIfMissing()
        {
            if (GameObject.Find("Rehearsal Practice Room") != null)
            {
                return;
            }

            GameObject roomRoot = new GameObject("Rehearsal Practice Room");
            CreatePrimitive(roomRoot.transform, PrimitiveType.Plane, "Floor", new Vector3(0f, 0f, 6f), new Vector3(2.2f, 1f, 2.2f), new Color(0.24f, 0.27f, 0.32f));
            CreatePrimitive(roomRoot.transform, PrimitiveType.Cylinder, "Stage", new Vector3(0f, 0.45f, 6f), new Vector3(2.5f, 0.35f, 2.5f), new Color(0.79f, 0.72f, 0.58f));
            CreatePrimitive(roomRoot.transform, PrimitiveType.Cube, "Podium", new Vector3(0f, 1.3f, 4.1f), new Vector3(1f, 1.2f, 0.6f), new Color(0.34f, 0.24f, 0.17f));

            Vector3[] audiencePositions =
            {
                new Vector3(-3.4f, 1f, 8.5f),
                new Vector3(-2f, 1f, 9.3f),
                new Vector3(0f, 1f, 9.7f),
                new Vector3(2f, 1f, 9.3f),
                new Vector3(3.4f, 1f, 8.5f)
            };

            for (int i = 0; i < audiencePositions.Length; i++)
            {
                CreatePrimitive(roomRoot.transform, PrimitiveType.Capsule, $"Audience_{i + 1}", audiencePositions[i], new Vector3(0.6f, 1f, 0.6f), new Color(0.52f, 0.61f, 0.72f));
            }
        }

        private static void CreatePrimitive(Transform parent, PrimitiveType primitiveType, string objectName, Vector3 position, Vector3 scale, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.position = position;
            primitive.transform.localScale = scale;

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private RehearsalSessionDebugHud.ActionButtonData[] BuildFeedbackButtons()
        {
            List<RehearsalSessionDebugHud.ActionButtonData> buttons = new List<RehearsalSessionDebugHud.ActionButtonData>();

            if (_presentationSegment != null)
            {
                buttons.Add(new RehearsalSessionDebugHud.ActionButtonData("Play Presentation", PlayPresentationClip));
            }

            for (int i = 0; i < _answerSegments.Count; i++)
            {
                int capturedIndex = i;
                buttons.Add(
                    new RehearsalSessionDebugHud.ActionButtonData(
                        $"Play {_answerSegments[capturedIndex].Label}",
                        () => PlayAnswerClip(capturedIndex)));
            }

            if (_isLoadingPlayback || (_playbackSource != null && _playbackSource.isPlaying))
            {
                buttons.Add(new RehearsalSessionDebugHud.ActionButtonData("Stop Playback", StopPlaybackAndRefresh));
            }

            buttons.Add(new RehearsalSessionDebugHud.ActionButtonData("Retry Same Topic", RetryCurrentSession));
            buttons.Add(new RehearsalSessionDebugHud.ActionButtonData("Choose Another Topic", ShowTopicSelection));
            return buttons.ToArray();
        }

        private string BuildPlaybackStatus()
        {
            if (_isLoadingPlayback)
            {
                return "Loading";
            }

            if (_playbackSource != null && _playbackSource.isPlaying)
            {
                return "Playing";
            }

            return "Idle";
        }

        private void AdvanceToStage(SessionStage stage, Action<SessionAttempt> onSuccess)
        {
            if (installer == null || installer.Coordinator == null)
            {
                ShowError("The rehearsal installer is not ready yet.");
                return;
            }

            if (_currentAttempt == null)
            {
                ShowError("Start a session before advancing stages.");
                return;
            }

            if (_currentAttempt.CurrentStage == stage)
            {
                onSuccess(_currentAttempt);
                return;
            }

            Result<SessionAttempt> result = installer.Coordinator.AdvanceTo(stage);
            if (result.IsFailure)
            {
                ShowError(result.ErrorMessage);
                return;
            }

            onSuccess(result.Value);
        }

        private void MoveToNextQuestionOrFeedback()
        {
            _recordingMode = RecordingMode.None;
            _countdownMode = CountdownMode.None;

            if (_currentQuestionIndex < _questions.Length - 1)
            {
                _currentQuestionIndex++;
                ShowCurrentQuestionPrompt();
                return;
            }

            BeginFeedbackLoading();
        }

        private void ResetAttemptRecordingState()
        {
            if (_audioCaptureService != null && _audioCaptureService.IsCapturing)
            {
                _audioCaptureService.CancelCapture();
            }

            StopPlayback();
            _presentationSegment = null;
            _presentationTranscript = null;
            _answerSegments.Clear();
            _answerTranscripts.Clear();
            _currentQuestionIndex = 0;
            _recordingMode = RecordingMode.None;
            _recordingStatus = "Session ready.";
            _countdownMode = CountdownMode.None;
        }

        private void RetryCurrentSession()
        {
            AdvanceToStage(
                SessionStage.RetryBootstrap,
                attempt =>
                {
                    _currentAttempt = attempt;
                    Result<SessionAttempt> retryResult = installer.Coordinator.RetryCurrentSession();
                    if (retryResult.IsFailure)
                    {
                        ShowError(retryResult.ErrorMessage);
                        return;
                    }

                    _currentAttempt = retryResult.Value;
                    _questions = BuildQuestions(_currentAttempt.Topic);
                    ResetAttemptRecordingState();

                    _hud.ShowScreen(
                        "Retry Ready",
                        $"Attempt {_currentAttempt.AttemptIndex} is prepared on the same topic. The next run will save a fresh presentation clip and answer segments.",
                        BuildSessionSummary(),
                        null,
                        new RehearsalSessionDebugHud.ActionButtonData("Start Preparation", BeginPreparation),
                        new RehearsalSessionDebugHud.ActionButtonData("Pick Another Topic", ShowTopicSelection));
                });
        }

        private void ShowCurrentQuestionPrompt()
        {
            _recordingStatus = $"Question {_currentQuestionIndex + 1} is waiting for your answer.";

            _hud.ShowScreen(
                $"Question {_currentQuestionIndex + 1} of {_questions.Length}",
                $"{_questions[_currentQuestionIndex]}\n\nStart recording when you are ready. A WAV file will be saved for this answer segment.",
                BuildSessionSummary(),
                null,
                new RehearsalSessionDebugHud.ActionButtonData("Start Recording", StartCurrentAnswerRecording),
                new RehearsalSessionDebugHud.ActionButtonData(BuildAnswerButtonLabel(), MoveToNextQuestionOrFeedback));
        }

        private void ShowError(string message)
        {
            _countdownMode = CountdownMode.None;
            _recordingMode = RecordingMode.None;
            _recordingStatus = message;

            _hud.ShowScreen(
                "Flow Blocked",
                message,
                BuildSessionSummary(),
                null,
                new RehearsalSessionDebugHud.ActionButtonData("Back to Topic Selection", ShowTopicSelection));
        }

        private void ShowFeedbackReview()
        {
            AdvanceToStage(
                SessionStage.FeedbackReview,
                attempt =>
                {
                    _currentAttempt = attempt;
                    _countdownMode = CountdownMode.None;
                    _recordingMode = RecordingMode.None;
                    _recordingStatus = "Feedback ready.";
                    RenderFeedbackReview();
                });
        }

        private void ShowPresentationReadyScreen()
        {
            _countdownMode = CountdownMode.None;
            _recordingMode = RecordingMode.None;

            _hud.ShowScreen(
                "Presentation Live",
                $"{_currentAttempt.Topic.Prompt}\n\nThis time the microphone will save a real presentation clip to disk before the question round starts.",
                BuildSessionSummary(),
                null,
                new RehearsalSessionDebugHud.ActionButtonData("Start Recording", StartPresentationRecording),
                new RehearsalSessionDebugHud.ActionButtonData("Skip Recording", BeginQuestionLoading));
        }

        private void ShowPresentationRecordingScreen()
        {
            _hud.ShowScreen(
                "Recording Presentation",
                $"{_currentAttempt.Topic.Prompt}\n\nSpeak as if the audience is already watching. Stop when you are done or let the timer auto-finish the clip.",
                BuildSessionSummary(),
                _countdownRemainingSeconds,
                new RehearsalSessionDebugHud.ActionButtonData("Stop Recording", StopPresentationRecording));
        }

        private void ShowTopicReady()
        {
            _countdownMode = CountdownMode.None;
            _recordingMode = RecordingMode.None;
            _recordingStatus = "Topic selected. Ready to rehearse.";

            _hud.ShowScreen(
                "Topic Locked",
                $"{_currentAttempt.Topic.Title}\nCategory: {_currentAttempt.Topic.Category}\nDifficulty: {_currentAttempt.Topic.Difficulty}\n\n{_currentAttempt.Topic.Prompt}",
                BuildSessionSummary(),
                null,
                new RehearsalSessionDebugHud.ActionButtonData("Start Preparation", BeginPreparation),
                new RehearsalSessionDebugHud.ActionButtonData("Pick Another Topic", ShowTopicSelection));
        }

        private void ShowTopicSelection()
        {
            ResetAttemptRecordingState();

            RehearsalSessionDebugHud.ActionButtonData[] buttons = new RehearsalSessionDebugHud.ActionButtonData[topicOptions.Length];
            for (int i = 0; i < topicOptions.Length; i++)
            {
                TopicOption option = topicOptions[i];
                buttons[i] = new RehearsalSessionDebugHud.ActionButtonData(
                    $"Start: {option.Label}",
                    () => StartSession(option.Id));
            }

            string micStatus = _audioCaptureService.HasAvailableDevice
                ? "Microphone detected. New rehearsal clips will be saved locally."
                : "No microphone detected yet. You can still move through the flow, but recording will fail until a device is available.";

            _hud.ShowScreen(
                "Choose a Rehearsal Topic",
                $"This build now records real presentation and answer clips as local WAV files.\n\n{micStatus}",
                _currentAttempt == null ? "No active session." : BuildSessionSummary(),
                null,
                buttons);
        }

        private void PlayAnswerClip(int index)
        {
            if (index < 0 || index >= _answerSegments.Count)
            {
                _recordingStatus = "That answer clip is no longer available.";
                RenderFeedbackReview();
                return;
            }

            PlaySegment(_answerSegments[index]);
        }

        private void PlayPresentationClip()
        {
            if (_presentationSegment == null)
            {
                _recordingStatus = "No presentation clip has been saved yet.";
                RenderFeedbackReview();
                return;
            }

            PlaySegment(_presentationSegment);
        }

        private void PlaySegment(RecordedAudioSegment segment)
        {
            if (segment == null)
            {
                return;
            }

            if (!File.Exists(segment.FilePath))
            {
                _recordingStatus = $"Missing audio file: {Path.GetFileName(segment.FilePath)}";
                RenderFeedbackReview();
                return;
            }

            StopPlayback();
            _playbackCoroutine = StartCoroutine(PlaySegmentRoutine(segment));
        }

        private IEnumerator PlaySegmentRoutine(RecordedAudioSegment segment)
        {
            _isLoadingPlayback = true;
            _recordingStatus = $"Loading {segment.Label}...";
            RenderFeedbackReview();

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new Uri(segment.FilePath).AbsoluteUri, AudioType.WAV))
            {
                yield return request.SendWebRequest();

                _isLoadingPlayback = false;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    _recordingStatus = $"Playback failed: {request.error}";
                    _playbackCoroutine = null;
                    RenderFeedbackReview();
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    _recordingStatus = "Playback failed: Unity could not load the saved WAV clip.";
                    _playbackCoroutine = null;
                    RenderFeedbackReview();
                    yield break;
                }

                _playbackSource.clip = clip;
                _playbackSource.Play();
                _recordingStatus = $"Playing {segment.Label}.";
                RenderFeedbackReview();

                yield return new WaitWhile(() => _playbackSource != null && _playbackSource.isPlaying);

                if (_playbackSource != null && _playbackSource.clip == clip)
                {
                    _playbackSource.clip = null;
                }

                Destroy(clip);
            }

            _recordingStatus = $"Finished playing {segment.Label}.";
            _playbackCoroutine = null;
            RenderFeedbackReview();
        }

        private void RenderFeedbackReview()
        {
            _hud.ShowScreen(
                "Feedback Review",
                BuildFeedbackBody(),
                BuildSessionSummary(),
                null,
                BuildFeedbackButtons());
        }

        private void TryTranscribeAnswer(RecordedAudioSegment segment, int questionIndex)
        {
            Result<TranscribedAudioSegment> result = _transcriptionService.TranscribeAnswer(
                segment,
                questionIndex >= 0 && questionIndex < _questions.Length ? _questions[questionIndex] : string.Empty,
                questionIndex);

            if (result.IsFailure)
            {
                _recordingStatus = $"Saved answer {questionIndex + 1} clip, but transcript is unavailable.";
                return;
            }

            _answerTranscripts.Add(result.Value);
            _recordingStatus = $"Saved answer {questionIndex + 1} clip and generated transcript preview.";
        }

        private void TryTranscribePresentation(RecordedAudioSegment segment)
        {
            Result<TranscribedAudioSegment> result = _transcriptionService.TranscribePresentation(segment, _currentAttempt.Topic);
            if (result.IsFailure)
            {
                _recordingStatus = "Saved presentation clip, but transcript is unavailable.";
                return;
            }

            _presentationTranscript = result.Value;
            _recordingStatus = "Saved presentation clip and generated transcript preview.";
        }

        private void StartCurrentAnswerRecording()
        {
            if (!_audioCaptureService.HasAvailableDevice)
            {
                ShowError("No microphone device is available. Connect or enable one, then try again.");
                return;
            }

            string filePath = BuildRecordingFilePath($"answer-{_currentQuestionIndex + 1}.wav");
            Result captureResult = _audioCaptureService.BeginCapture(filePath, Mathf.CeilToInt(answerDurationSeconds));
            if (captureResult.IsFailure)
            {
                ShowError(captureResult.ErrorMessage);
                return;
            }

            _recordingMode = RecordingMode.Answer;
            _countdownMode = CountdownMode.AnswerRecording;
            _countdownRemainingSeconds = answerDurationSeconds;
            _recordingStatus = $"Recording answer {_currentQuestionIndex + 1} to {Path.GetFileName(filePath)}.";

            _hud.ShowScreen(
                $"Recording Answer {_currentQuestionIndex + 1}",
                $"{_questions[_currentQuestionIndex]}\n\nAnswer naturally. This segment will be saved when you stop recording.",
                BuildSessionSummary(),
                _countdownRemainingSeconds,
                new RehearsalSessionDebugHud.ActionButtonData("Stop Recording", StopCurrentAnswerRecording));
        }

        private void StartPresentationRecording()
        {
            if (!_audioCaptureService.HasAvailableDevice)
            {
                ShowError("No microphone device is available. Connect or enable one, then try again.");
                return;
            }

            string filePath = BuildRecordingFilePath("presentation.wav");
            Result captureResult = _audioCaptureService.BeginCapture(filePath, Mathf.CeilToInt(presentationDurationSeconds));
            if (captureResult.IsFailure)
            {
                ShowError(captureResult.ErrorMessage);
                return;
            }

            _recordingMode = RecordingMode.Presentation;
            _countdownMode = CountdownMode.PresentationRecording;
            _countdownRemainingSeconds = presentationDurationSeconds;
            _recordingStatus = $"Recording presentation to {Path.GetFileName(filePath)}.";
            ShowPresentationRecordingScreen();
        }

        private void StartSession(string topicId)
        {
            if (installer == null || installer.Coordinator == null)
            {
                ShowError("The rehearsal installer is not ready yet.");
                return;
            }

            Result<SessionAttempt> startResult = installer.Coordinator.StartNewSession(topicId);
            if (startResult.IsFailure)
            {
                ShowError(startResult.ErrorMessage);
                return;
            }

            _currentAttempt = startResult.Value;
            _questions = BuildQuestions(_currentAttempt.Topic);
            ResetAttemptRecordingState();
            ShowTopicReady();
        }

        private void StopCurrentAnswerRecording()
        {
            if (_recordingMode != RecordingMode.Answer || !_audioCaptureService.IsCapturing)
            {
                MoveToNextQuestionOrFeedback();
                return;
            }

            Result<RecordedAudioSegment> result = _audioCaptureService.FinishCapture(
                $"Answer {_currentQuestionIndex + 1}",
                SessionStage.AnswerRecording.ToString());

            if (result.IsFailure)
            {
                ShowError(result.ErrorMessage);
                return;
            }

            _answerSegments.Add(result.Value);
            TryTranscribeAnswer(result.Value, _currentQuestionIndex);
            MoveToNextQuestionOrFeedback();
        }

        private void StopPlayback()
        {
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }

            _isLoadingPlayback = false;
            if (_playbackSource == null)
            {
                return;
            }

            if (_playbackSource.isPlaying)
            {
                _playbackSource.Stop();
            }

            if (_playbackSource.clip != null)
            {
                AudioClip clip = _playbackSource.clip;
                _playbackSource.clip = null;
                Destroy(clip);
            }
        }

        private void StopPlaybackAndRefresh()
        {
            StopPlayback();
            _recordingStatus = "Playback stopped.";
            RenderFeedbackReview();
        }

        private void StopPresentationRecording()
        {
            if (_recordingMode != RecordingMode.Presentation || !_audioCaptureService.IsCapturing)
            {
                BeginQuestionLoading();
                return;
            }

            Result<RecordedAudioSegment> result = _audioCaptureService.FinishCapture(
                "Presentation",
                SessionStage.PresentationRecording.ToString());

            if (result.IsFailure)
            {
                ShowError(result.ErrorMessage);
                return;
            }

            _presentationSegment = result.Value;
            TryTranscribePresentation(result.Value);
            _recordingMode = RecordingMode.None;
            BeginQuestionLoading();
        }

        private void OnDestroy()
        {
            if (_audioCaptureService != null && _audioCaptureService.IsCapturing)
            {
                _audioCaptureService.CancelCapture();
            }

            StopPlayback();
        }
    }
}
