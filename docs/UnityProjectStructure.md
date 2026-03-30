# Agora VR Unity Project Structure

- 문서 버전: v0.1
- 작성일: 2026-03-30
- 상태: Draft
- 연관 문서: [PRD](./PRD.md), [Architecture](./Architecture.md), [MVPTaskBreakdown](./MVPTaskBreakdown.md)

## 1. 목적

이 문서는 Agora VR MVP를 Unity에서 유지보수 가능하게 구현하기 위한 폴더 구조, 스크립트 구조, 책임 분리 원칙을 정의한다.

핵심 목표는 다음과 같다.

- 모든 클래스가 하나의 명확한 책임만 가지게 한다.
- 버그가 생겼을 때 수정 범위를 빠르게 좁힐 수 있게 한다.
- 기능 추가 시 기존 코드를 덜 건드리도록 의존성을 통제한다.
- 씬, UI, 오디오, AI, 세션 로직이 뒤섞이지 않도록 레이어를 분리한다.

## 2. 최상위 원칙

- 하나의 클래스는 하나의 이유로만 변경되어야 한다.
- MonoBehaviour는 Unity 연결과 화면 제어만 담당하고, 도메인 로직은 일반 C# 클래스로 분리한다.
- 데이터 구조와 동작 로직을 분리한다.
- 외부 시스템 호출은 반드시 adapter 또는 service 뒤에 둔다.
- 상태 전환은 명시적인 state machine으로 관리한다.
- 한 클래스가 다른 시스템 3개 이상을 직접 조정하기 시작하면 분리 신호로 본다.
- 전역 싱글턴은 최소화하고, 필요 시 composition root에서만 생성한다.

## 3. 권장 폴더 구조

```text
Assets/
  AgoraVR/
    _Project/
      Scenes/
        Bootstrap/
        Hub/
        Rehearsal/
        Feedback/
      Settings/
      RenderPipeline/
      Addressables/

    Runtime/
      Bootstrap/
      Composition/
      Common/
        Logging/
        Time/
        IDs/
        Results/
        Extensions/

      Domain/
        Session/
        Topic/
        Feedback/
        Transcript/
        Audience/

      Application/
        SessionFlow/
        AudioCapture/
        TranscriptProcessing/
        QuestionGeneration/
        FeedbackGeneration/
        Retry/
        Telemetry/

      Infrastructure/
        Auth/
        Api/
        Storage/
        STT/
        LLM/
        Analytics/
        Persistence/
        Audio/

      Presentation/
        UI/
          Common/
          HUD/
          Panels/
          Feedback/
        World/
          Stage/
          Audience/
          Lighting/
        Controllers/
        ViewModels/

      Features/
        RehearsalSession/
          Installers/
          Flow/
          UI/
          World/
        FeedbackReview/
        HubNavigation/

    Editor/
      Build/
      Validation/
      Tools/

    Tests/
      EditMode/
        Domain/
        Application/
      PlayMode/
        Features/

    Art/
    Audio/
    Prefabs/
    Materials/
    Animations/
```

## 4. Assembly Definition 권장 구조

`asmdef`는 반드시 쪼개는 편이 좋다. 그래야 컴파일 시간과 의존성이 함께 관리된다.

권장 assemblies:

- `AgoraVR.Common`
- `AgoraVR.Domain`
- `AgoraVR.Application`
- `AgoraVR.Infrastructure`
- `AgoraVR.Presentation`
- `AgoraVR.Features`
- `AgoraVR.Editor`
- `AgoraVR.Tests.EditMode`
- `AgoraVR.Tests.PlayMode`

의존성 규칙:

- `Common`은 아무것도 참조하지 않는다.
- `Domain`은 `Common`만 참조한다.
- `Application`은 `Domain`, `Common`만 참조한다.
- `Infrastructure`는 `Application`, `Domain`, `Common`을 참조할 수 있다.
- `Presentation`은 `Application`, `Domain`, `Common`을 참조할 수 있다.
- `Features`는 위 레이어를 조합하지만, 하위 레이어가 `Features`를 참조하면 안 된다.
- `Editor`는 런타임 어셈블리를 참조할 수 있지만 반대는 금지한다.

즉, 아래 방향만 허용한다.

```text
Features -> Presentation -> Application -> Domain -> Common
Features -> Infrastructure -> Application -> Domain -> Common
```

## 5. 레이어별 책임

### Common

역할:

- 범용 유틸리티
- 공통 결과 타입
- 식별자 래퍼
- 공통 인터페이스

넣어도 되는 것:

- `Result`
- `ErrorCode`
- `ITimeProvider`
- `ILogger`
- `SessionId`

넣으면 안 되는 것:

- 세션 비즈니스 로직
- Unity API 직접 호출

### Domain

역할:

- 제품의 핵심 규칙과 상태 모델
- 세션, 질문, 피드백, 주제에 대한 순수 모델

넣어도 되는 것:

- `SessionAttempt`
- `SessionStage`
- `QuestionType`
- `FeedbackReport`
- `TopicDefinition`

넣으면 안 되는 것:

- HTTP 호출
- 파일 저장
- MonoBehaviour
- UI 문자열 조립

### Application

역할:

- 유스케이스 실행
- 도메인 객체 조합
- 흐름 제어
- 포트 인터페이스 정의

넣어도 되는 것:

- `StartSessionUseCase`
- `SubmitPresentationSegmentUseCase`
- `GenerateQuestionsUseCase`
- `CompleteSessionUseCase`
- `RetrySessionUseCase`

넣으면 안 되는 것:

- Unity 씬 참조
- 실제 API SDK 코드
- 패널 열고 닫기 같은 화면 로직

### Infrastructure

역할:

- 외부 시스템 연동
- 저장소 구현
- API 클라이언트
- STT, LLM, Analytics 어댑터

넣어도 되는 것:

- `HttpSessionApiClient`
- `OpenAiQuestionGenerator`
- `WhisperTranscriptService`
- `PlayerPrefsTokenStore`
- `UnityMicrophoneRecorder`

넣으면 안 되는 것:

- 세션 화면 흐름 판단
- 피드백 표시 방식 결정

### Presentation

역할:

- UI 표시
- 월드 오브젝트 시각 제어
- 유저 입력 수집
- Application 호출 결과를 보여주기

넣어도 되는 것:

- `PreparationTimerView`
- `QuestionPanelView`
- `FeedbackCardView`
- `AudienceLookDirector`
- `StageLightingController`

넣으면 안 되는 것:

- STT 요청 생성
- 질문 품질 규칙 판단
- 세션 저장소 직접 조작

### Features

역할:

- 특정 사용자 기능 단위의 조립
- 씬/프리팹/프레젠테이션/유스케이스 연결

넣어도 되는 것:

- `RehearsalSessionInstaller`
- `RehearsalSessionFlowCoordinator`
- `FeedbackReviewInstaller`

넣으면 안 되는 것:

- 범용 유틸 남발
- 여러 기능이 함께 참조해야 하는 핵심 로직

## 6. 씬 구조 원칙

권장 씬:

- `BootstrapScene`
- `AgoraHubScene`
- `RehearsalStageScene`
- `FeedbackReviewScene`

원칙:

- 씬은 기능 단위로 자른다.
- 씬 전환 로직은 `SceneFlowService` 같은 단일 진입점에서 관리한다.
- 씬 안의 오브젝트는 최대한 프리팹과 installer를 통해 연결한다.
- 씬 하나에 모든 기능을 넣는 구조는 피한다.

## 7. 클래스 유형별 규칙

### MonoBehaviour

역할:

- Unity 이벤트 수신
- 컴포넌트 참조 연결
- View 업데이트
- 입력 이벤트 전달

규칙:

- 비즈니스 규칙을 넣지 않는다.
- 다른 MonoBehaviour 여러 개를 직접 오케스트레이션하지 않는다.
- 길어지면 `Binder`, `View`, `Controller`로 쪼갠다.

나쁜 예:

- 녹음 시작
- 업로드 호출
- 질문 생성 요청
- UI 열기
- 로그 전송

위 다섯 개를 하나의 `SessionManager : MonoBehaviour`가 다 하는 구조

좋은 예:

- `SessionScreenController`는 버튼 입력을 받아 `IStartSessionUseCase`를 호출한다.
- `PreparationTimerView`는 남은 시간을 표시한다.
- `AudioRecordButtonView`는 녹음 시작 이벤트만 발행한다.

### UseCase 클래스

역할:

- 한 유저 액션 또는 한 시스템 액션의 비즈니스 흐름 수행

규칙:

- 이름은 동사로 시작한다.
- 한 메서드의 목적이 선명해야 한다.
- 외부 의존성은 인터페이스로 주입받는다.

예시:

- `StartSessionUseCase`
- `UploadAudioSegmentUseCase`
- `GenerateSessionFeedbackUseCase`

### Repository / Gateway

역할:

- 데이터 읽기/쓰기 캡슐화

규칙:

- 저장소는 데이터를 제공하고 저장한다.
- 비즈니스 규칙 판단은 하지 않는다.

예시:

- `ISessionRepository`
- `ISessionApiClient`
- `ITranscriptRepository`

### Service

역할:

- 독립 기능 수행

규칙:

- service라는 이름을 남발하지 않는다.
- 계산, 변환, 외부 호출처럼 성격이 선명할 때만 사용한다.

예시:

- `TranscriptNormalizer`
- `FeedbackSummaryMapper`
- `AudienceReactionPlanner`

### Coordinator

역할:

- 여러 use case와 view를 연결하는 feature 단위 조정자

규칙:

- 오직 feature 범위에서만 존재한다.
- 전역 매니저처럼 커지면 다시 쪼갠다.

예시:

- `RehearsalSessionFlowCoordinator`

## 8. Agora VR 기준 추천 스크립트 구조

### Bootstrap

- `AppBootstrapper`
- `ProjectInstaller`
- `SceneFlowService`

### Domain.Session

- `SessionAttempt`
- `SessionAttemptStatus`
- `SessionStage`
- `SessionTimerPolicy`

### Domain.Topic

- `TopicDefinition`
- `TopicCategory`
- `DifficultyLevel`

### Domain.Feedback

- `FeedbackReport`
- `FeedbackStrength`
- `FeedbackImprovement`

### Application.SessionFlow

- `StartSessionUseCase`
- `AdvanceSessionStageUseCase`
- `RetrySessionUseCase`
- `GetSessionSummaryUseCase`

### Application.AudioCapture

- `RecordPresentationSegmentUseCase`
- `RecordAnswerSegmentUseCase`
- `UploadAudioSegmentUseCase`

### Application.QuestionGeneration

- `GenerateQuestionsUseCase`
- `GetQuestionSetUseCase`

### Application.FeedbackGeneration

- `GenerateFeedbackUseCase`
- `GetFeedbackReportUseCase`

### Infrastructure.Api

- `HttpSessionApiClient`
- `SessionApiEndpoints`
- `ApiRequestFactory`

### Infrastructure.STT

- `SpeechToTextJobClient`
- `TranscriptPollingClient`

### Infrastructure.LLM

- `QuestionGeneratorClient`
- `FeedbackGeneratorClient`
- `LlmResponseValidator`

### Presentation.UI

- `TopicSelectionPanelView`
- `PreparationTimerView`
- `QuestionPanelView`
- `FeedbackPanelView`
- `RetryButtonView`

### Presentation.World

- `StageLightingController`
- `AudienceLookDirector`
- `AudienceReactionController`
- `StageAmbienceController`

### Features.RehearsalSession

- `RehearsalSessionInstaller`
- `RehearsalSessionFlowCoordinator`
- `RehearsalSessionContext`

## 9. 절대 피해야 할 구조

- `GameManager` 하나에 세션 전체를 몰아넣는 구조
- `UIManager`가 모든 패널과 흐름을 다 아는 구조
- `AudioManager`가 녹음, 업로드, 전사 상태, UI까지 함께 가지는 구조
- MonoBehaviour에서 직접 HTTP 요청을 보내는 구조
- ScriptableObject에 실행 로직을 과도하게 넣는 구조
- static 전역 상태에 세션 데이터를 보관하는 구조

이런 구조는 처음엔 빠르지만, 버그가 생기면 수정 범위가 넓어지고 회귀 위험이 커진다.

## 10. 버그 수정 가능성을 높이는 설계 규칙

### 상태는 한 곳에서만 바꾼다

- 세션 단계는 `SessionFlowCoordinator` 또는 `SessionStateMachine`만 변경할 수 있어야 한다.
- View가 직접 상태를 바꾸면 안 된다.

### 입력과 출력 경로를 분리한다

- 버튼 입력은 controller에서 받고
- 실제 동작은 use case가 수행하고
- 결과 반영은 view가 담당한다

### 외부 연동은 항상 감싼다

- OpenAI, STT, 저장소 SDK를 바로 여기저기서 호출하지 않는다.
- wrapper나 gateway 뒤에 둬야 나중에 교체와 테스트가 가능하다.

### DTO와 Domain Model을 분리한다

- API 응답 모델과 실제 게임 내 모델을 같은 클래스로 쓰지 않는다.
- 변환 mapper를 둔다.

### 로그 포인트를 표준화한다

- 세션 시작
- 상태 전환
- 녹음 시작/완료
- 업로드 성공/실패
- 질문 생성 요청/응답
- 피드백 생성 요청/응답

이 포인트는 공통 logger를 통해 남겨야 한다.

## 11. 네이밍 규칙

- `View`: 화면 표시 전용
- `Controller`: 입력 처리와 view 연결
- `Coordinator`: feature 흐름 조정
- `UseCase`: 유스케이스 실행
- `Repository`: 데이터 저장/조회
- `Client`: 외부 API 호출
- `Mapper`: DTO <-> Domain 변환
- `Installer`: 의존성 구성

이 규칙을 유지하면 파일 이름만 봐도 책임을 유추할 수 있다.

## 12. ScriptableObject 사용 원칙

좋은 용도:

- 밸런스 값
- 주제 데이터
- 시각 연출 설정값
- 오디오 볼륨 설정

좋지 않은 용도:

- 세션 실행 상태 저장
- 유스케이스 로직 실행
- 외부 API 호출

즉, ScriptableObject는 설정과 데이터 자산에 가깝게 사용한다.

## 13. 테스트 전략

### EditMode 테스트

- Domain 규칙 테스트
- UseCase 테스트
- Mapper 테스트
- 상태 머신 테스트

### PlayMode 테스트

- 씬 진입
- 세션 단계 전환
- 피드백 화면 진입
- 재도전 루프

우선 테스트 대상:

- `SessionStateMachine`
- `StartSessionUseCase`
- `RetrySessionUseCase`
- `TranscriptNormalizer`
- `FeedbackSummaryMapper`

## 14. 추천 시작 구조

가장 처음에는 아래 정도만 만들어도 충분하다.

```text
Assets/AgoraVR/Runtime/Bootstrap
Assets/AgoraVR/Runtime/Common
Assets/AgoraVR/Runtime/Domain
Assets/AgoraVR/Runtime/Application
Assets/AgoraVR/Runtime/Infrastructure
Assets/AgoraVR/Runtime/Presentation
Assets/AgoraVR/Runtime/Features/RehearsalSession
Assets/AgoraVR/Tests/EditMode
Assets/AgoraVR/Tests/PlayMode
```

처음부터 모든 폴더를 크게 만들기보다, 레이어 규칙을 먼저 고정하고 기능이 늘 때 하위 폴더를 확장하는 편이 낫다.

## 15. 현재 기준 권장 결론

유지보수 가능한 Unity 구조의 핵심은 폴더를 예쁘게 나누는 것이 아니라, 각 클래스가 자기 책임 외의 일을 못 하게 막는 것이다.

즉, Agora VR 구현에서는 다음 원칙을 강하게 지키는 것이 맞다.

- MonoBehaviour는 얇게 유지한다.
- 세션 규칙은 Domain/Application에 둔다.
- 외부 연동은 Infrastructure 뒤로 숨긴다.
- 기능 조립은 Feature 단위 coordinator에서만 한다.
- 상태 전환과 로그 포인트를 명시적으로 관리한다.
