# Agora VR MVP Task Breakdown

- 문서 버전: v0.1
- 작성일: 2026-03-30
- 상태: Draft
- 연관 문서: [PRD](./PRD.md), [Architecture](./Architecture.md), [Brand](./Brand.md)

## 1. 목적

이 문서는 Agora VR MVP를 실제 개발 가능한 작업 단위로 분해한다.

목표는 다음과 같다.

- MVP 범위를 구현 에픽과 태스크로 정리한다.
- 무엇을 먼저 만들고 무엇을 나중에 미룰지 명확히 한다.
- 클라이언트, 백엔드, AI, 콘텐츠 작업의 선행관계를 드러낸다.
- 첫 플레이 가능한 빌드까지의 경로를 짧게 만든다.

## 2. MVP 한 줄 범위

- 혼자서도 가능한 VR 발표 리허설
- 1인 발표 후 AI 질문 2~3개
- 조리성 중심 피드백 제공
- 바로 다시 해볼 수 있는 짧은 루프

## 3. 작업 우선순위 원칙

- 가장 먼저 검증해야 하는 것은 VR 공간의 긴장감과 세션 루프 완성도다.
- 다듬기보다 end-to-end 동작이 우선이다.
- AI 품질 고도화보다 먼저 입력/출력 파이프라인이 안정적으로 이어져야 한다.
- 멋진 허브보다 발표 무대와 피드백 루프가 먼저다.
- 운영 도구와 계정 복잡도는 MVP 이후로 미룬다.

## 4. 마일스톤

### M0. Foundation

- 앱 실행
- 기본 씬 전환
- 세션 상태 머신 동작
- 더미 주제 선택 가능

### M1. First Playable

- 준비 -> 발표 -> 질문 -> 답변 -> 피드백 -> 재도전까지 전체 루프 동작
- 질문과 피드백은 더미 또는 초기 AI로 동작 가능
- 연단과 청중 기본 연출 포함

### M2. AI Integrated MVP

- 실제 음성 업로드 및 전사 연동
- LLM 기반 질문 생성
- 구조적 피드백 생성
- 세션 리포트 저장

### M3. Polished MVP

- 실패 폴백 UX
- 재도전 최적화
- 기초 분석 이벤트
- 성능 안정화 및 콘텐츠 품질 개선

## 5. 에픽 개요

1. Core Session Flow
2. VR Stage and Audience
3. Audio Capture and Upload
4. Backend Session API
5. Speech-to-Text Pipeline
6. AI Question and Feedback
7. Feedback Experience
8. Topic and Content System
9. Telemetry and Stability
10. Release Readiness

## 6. 에픽별 상세

### Epic 1. Core Session Flow

목표:

- 사용자가 Quest에서 세션을 시작하고, 단계별로 진행하며, 재도전까지 할 수 있게 만든다.

핵심 태스크:

- 앱 부트스트랩과 기본 내비게이션 구성
- 세션 상태 머신 구현
- TopicReady, Preparation, PresentationRecording, QuestionLoading, AnswerRecording, FeedbackReview 상태 UI 연결
- 단계별 타이머와 전환 규칙 구현
- 세션 종료 후 재도전 분기 처리

완료 기준:

- 더미 데이터만으로도 전체 세션이 끊김 없이 한 바퀴 돈다.

우선순위:

- P0

### Epic 2. VR Stage and Audience

목표:

- 사용자가 무대에 선 감각과 청중 시선을 체감할 수 있는 최소 연출을 만든다.

핵심 태스크:

- 허브 아고라 최소 버전 구성
- 발표 연단 씬 제작
- 청중석 배치와 NPC 프리팹 연결
- 발표 시작, 질문 도착, 피드백 전환에 따른 조명 변화
- 청중 시선 집중과 웅성거림, 박수, 정적 연출 추가

완료 기준:

- 사용자가 발표 단계와 질문 단계를 공간적으로 분명히 느낄 수 있다.

우선순위:

- P0

### Epic 3. Audio Capture and Upload

목표:

- 발표와 답변 음성을 세그먼트 단위로 안정적으로 저장하고 업로드한다.

핵심 태스크:

- 마이크 권한 처리
- 발표/답변 구간별 녹음 시작/종료
- 로컬 버퍼 또는 파일 인코딩
- 업로드 큐 구현
- 실패 시 재시도와 사용자 상태 표시

완료 기준:

- 세션당 발표 1개와 답변 2~3개의 음성 파일이 서버로 전달된다.

우선순위:

- P0

### Epic 4. Backend Session API

목표:

- 세션 생성, 상태 저장, 세그먼트 등록, 결과 조회를 담당하는 기본 서버를 만든다.

핵심 태스크:

- 인증이 단순한 개발용 사용자 모델 정의
- `POST /sessions`
- `GET /sessions/{id}`
- `POST /sessions/{id}/segments`
- `POST /sessions/{id}/retry`
- 세션과 시도 데이터 모델 설계

완료 기준:

- 클라이언트가 백엔드 기준으로 세션을 생성하고 진행 상태를 조회할 수 있다.

우선순위:

- P0

### Epic 5. Speech-to-Text Pipeline

목표:

- 업로드된 음성 파일을 전사하여 질문/피드백 입력으로 사용할 수 있게 만든다.

핵심 태스크:

- 업로드 파일 저장 정책 수립
- STT 작업 큐 구현
- 발표/답변 구간별 transcript 저장
- 실패 상태와 재처리 상태 관리
- transcript 정규화 로직 구현

완료 기준:

- 발표와 답변 세그먼트에 대해 텍스트 전사 결과를 조회할 수 있다.

우선순위:

- P0

### Epic 6. AI Question and Feedback

목표:

- transcript 기반 질문 2~3개와 구조적 피드백을 생성한다.

핵심 태스크:

- 질문 생성 프롬프트 템플릿 작성
- 피드백 생성 프롬프트 템플릿 작성
- 응답 JSON 스키마 설계
- 질문 생성 API 연결
- 피드백 생성 API 연결
- LLM 실패 시 폴백 질문 세트 제공

완료 기준:

- 세션 종료 후 실제 transcript 기반 질문과 피드백이 반환된다.

우선순위:

- P0

### Epic 7. Feedback Experience

목표:

- 피드백을 사용자가 바로 이해하고 재도전으로 연결되게 보여준다.

핵심 태스크:

- 피드백 카드 UI 설계
- 강점, 개선 포인트, 재도전 과제 노출
- 이전 시도 대비 힌트 노출
- 즉시 재도전 CTA 연결

완료 기준:

- 사용자가 세션 종료 후 무엇을 바꿔서 다시 해볼지 이해할 수 있다.

우선순위:

- P1

### Epic 8. Topic and Content System

목표:

- 초기 주제 카테고리와 난이도 체계를 최소 운영 가능한 수준으로 만든다.

핵심 태스크:

- 주제 데이터 스키마 정의
- 면접 답변, 짧은 발표, 찬반 주장 카테고리 구성
- 쉬움/보통/어려움 난이도 태깅
- 위험 주제 필터링 규칙 수립
- 초기 주제 20~30개 작성

완료 기준:

- 사용자가 최소한의 다양성을 느끼며 반복 리허설을 할 수 있다.

우선순위:

- P1

### Epic 9. Telemetry and Stability

목표:

- MVP 품질을 판단할 최소 이벤트와 실패 로그를 수집한다.

핵심 태스크:

- 세션 시작/완료/재도전 이벤트
- 음성 업로드 실패 로그
- STT 실패 로그
- 질문/피드백 생성 실패 로그
- 클라이언트 크래시 및 FPS 수집

완료 기준:

- 플레이 테스트 후 어느 지점에서 세션이 끊기거나 이탈하는지 볼 수 있다.

우선순위:

- P1

### Epic 10. Release Readiness

목표:

- 내부 테스트 가능한 Quest 빌드와 최소 운영 정책을 준비한다.

핵심 태스크:

- Quest 빌드 파이프라인 정리
- 환경별 API 엔드포인트 분기
- 기본 로그인/약관/개인정보 고지 문구 초안
- 음성 보관 정책 초안
- 내부 테스트 체크리스트 작성

완료 기준:

- 내부 팀이 반복적으로 설치, 실행, 피드백 수집을 할 수 있다.

우선순위:

- P1

## 7. 선행관계

- Epic 1은 모든 작업의 출발점이다.
- Epic 2는 Epic 1과 병렬 가능하지만, First Playable 전에는 합쳐져야 한다.
- Epic 3과 Epic 4는 함께 진행되어야 한다.
- Epic 5는 Epic 3, Epic 4 완료 후 본격 진행 가능하다.
- Epic 6은 Epic 5 산출물 없이도 더미 transcript로 선행 개발 가능하다.
- Epic 7은 Epic 6이 최소 형태로 동작해야 의미가 있다.
- Epic 8은 Epic 1과 병렬 가능하다.
- Epic 9는 M1 이후 바로 붙이는 것이 좋다.
- Epic 10은 M2 이후 본격화하되, 빌드 파이프라인은 초기에 잡아두는 편이 좋다.

## 8. 권장 구현 순서

1. Epic 1 Core Session Flow
2. Epic 2 VR Stage and Audience
3. Epic 4 Backend Session API
4. Epic 3 Audio Capture and Upload
5. Epic 5 Speech-to-Text Pipeline
6. Epic 6 AI Question and Feedback
7. Epic 7 Feedback Experience
8. Epic 8 Topic and Content System
9. Epic 9 Telemetry and Stability
10. Epic 10 Release Readiness

## 9. 첫 2주 스프린트 권장 범위

### Sprint 1

- Unity 프로젝트 구조 정리
- 기본 씬 전환
- 세션 상태 머신 초안
- 발표 무대와 청중 placeholder 배치
- 준비 타이머, 발표 단계, 더미 질문 단계 연결
- 더미 피드백 화면과 재도전 버튼 구현

목표:

- AI 없이도 발표 리허설 형태가 보이는 First Playable 만들기

### Sprint 2

- 음성 녹음 및 업로드
- 세션 API 초안
- STT 연동
- transcript 저장
- 초기 질문 생성 API 연결

목표:

- 사용자가 실제로 말한 내용이 다음 질문으로 이어지는 흐름 만들기

## 10. 추천 역할 분담

### Unity / XR

- 세션 플로우
- 씬 전환
- 무대 연출
- NPC 연출
- 오디오 캡처

### Backend

- 세션 API
- 업로드 처리
- STT 큐
- 저장 구조
- 분석 이벤트

### AI / Prompt

- 질문 프롬프트
- 피드백 프롬프트
- 안전 필터
- 출력 스키마 검증

### Content / Product

- 주제 설계
- 난이도 기준
- 피드백 카피 가이드
- 플레이테스트 스크립트

## 11. 지금 당장 미뤄도 되는 것

- 다인 토론
- 실시간 다른 사용자 아바타
- 자유 주제 업로드
- 고급 허브 월드
- 랭킹과 배지
- 정교한 아바타 커스터마이징
- 교육기관용 관리자 콘솔

## 12. 가장 먼저 검증할 질문

1. 무대와 청중 연출만으로 실제 긴장감이 생기는가
2. 발표 후 AI 질문 2~3개가 충분히 압박감 있게 느껴지는가
3. 피드백이 재도전 행동을 실제로 유도하는가
4. 사용자가 5분에서 8분 세션을 부담 없이 반복하는가

## 13. 현재 기준 권장 결론

지금 팀이 가장 먼저 만들어야 하는 것은 "모든 걸 조금씩 갖춘 앱"이 아니라, "한 번 서보고 다시 해보고 싶어지는 발표 리허설 루프"다.

즉, MVP 개발은 다음 우선순위로 집중하는 것이 맞다.

- 세션 상태 머신
- 무대와 청중의 긴장감
- 음성 입력과 전사
- 질문 생성
- 피드백과 재도전
