# 우리의 마블 클라이언트 (our-marble-client)

"모두의 마블"을 모티브로 한 **2~4인 실시간 온라인 보드게임**의 Unity 클라이언트입니다.
화면 렌더링, 말 이동, 연출을 담당하고, 서버([our-marble-server](https://github.com/Our-Marble/our-marble-server))와 WebSocket으로 통신합니다.
주사위 난수와 모든 게임 판정은 서버가 맡고, 클라이언트는 **유저의 선택을 서버에 전달하고 결과를 연출**합니다.

## 핵심 특징

- **서버 권위 구조**: 클라이언트는 구매·건설·인수·매각 같은 선택만 보내고, 결과는 서버 알림을 받아 반영합니다. 조작된 요청은 서버가 거부합니다.
- **연출과 서버 진행의 분리**: 서버는 연출과 관계없이 알림을 보냅니다. 클라이언트는 도착한 메시지를 **메시지 큐**에 쌓고, 연출이 끝나면 차례로 처리합니다. (상태 동기화만 즉시 처리)
- **재접속 복구**: 연결이 끊겨도 게임 중이면 자리가 유지되고, 재접속하면 게임 화면으로 돌아와 서버에 현재 상태(`GAME_STATE_REQUEST`)를 요청합니다.
- **봇과 함께 플레이**: 방장이 대기실에서 봇 수를 정하면 서버가 봇을 진행합니다. 클라이언트는 사람과 같은 알림을 받아 똑같이 연출합니다.
- **연출 단계화**: 주사위, 말 이동, 카드, 구매·건설 팝업, 세계여행, 무인도, 세금 연출을 DOTween으로 구성합니다.

## 기술 스택

| 영역 | 기술 |
| --- | --- |
| 엔진 | Unity 6 (6000.3), C#, URP 2D |
| UI | uGUI, TextMesh Pro |
| 연출 | DOTween |
| 통신 | NativeWebSocket (게임·로비), UnityWebRequest (로그인·회원가입 HTTP) |
| 직렬화 | Newtonsoft.Json |
| 데이터 | ScriptableObject (보드·카드 데이터), JSON (땅 가격표) |

## 아키텍처

```
서버 ◀──WebSocket {type, ...}──▶ WebSocketNetwork        연결·송수신 (JWT를 핸드셰이크 헤더로 전달)
                                      │
                         ┌────────────┴────────────┐
                    LobbyNetwork               GameNetwork      type별 메시지 해석, DTO 변환
                         │                         │
                   (이벤트 발행)             (메시지 큐 → 이벤트 발행)
                         │                         │
                   LobbyManager               GameManager        게임 규칙·상태 반영
                         │                         │
                      UIManager ◀──────────▶ BoardManager        팝업·HUD / 말·타일 연출
```

- **네트워크와 게임 로직을 이벤트로 분리**: 네트워크 계층은 누가 받는지 몰라도 되고, 게임 씬이 다시 로드돼도 이전 구독이 남지 않습니다.
- **메시지 큐**: 연출 중에 도착한 알림을 보관했다가 순서대로 처리해, 연출과 서버 진행이 충돌하지 않습니다.
- **`SceneFlow`**: 로그인 → 로비 → 게임 → 같은 방 복귀의 씬 이동을 한 곳에서 관리합니다.
- **같은 규칙으로 계산**: 통행료, 월급, 파산처럼 서버가 알림을 보내지 않는 처리는 클라이언트가 같은 규칙으로 계산합니다. 규칙을 바꿀 때는 서버와 함께 맞춰야 합니다.
- **설정은 서버가 기준**: 초기 자금, 월급, 최대 라운드 등은 대기실의 `ROOM_UPDATED`와 재접속 시 `GAME_STATE_SYNC`의 `config`를 따릅니다.

## 폴더 구조

```
Assets
├─ 01.Scenes            게임 씬
├─ 02.Scripts
│  ├─ Board/            보드, 타일, 말, 특수 칸 연출 (무인도·세금·세계여행)
│  ├─ Bot/              봇 판단 원본 (판단은 서버에서 실행)
│  ├─ Card/             황금열쇠 데이터
│  ├─ Chat/             대기방 채팅
│  ├─ Manager/          GameManager, LobbyManager, LoginManager, UIManager, BoardManager,
│  │                    PlayerManager, PropertyManager, EconomyManager, CardManager, SceneFlow
│  ├─ Network/          WebSocket·HTTP 통신, 메시지 type, 요청/알림 DTO (Game, Lobby)
│  ├─ Property/         땅 데이터, 건물 단계, 매각 자동 선택 방식
│  ├─ State/            GameState, PlayerState, PropertyState, GameConfig, 방 스냅샷
│  ├─ UI/               팝업·HUD View, 공통 컴포넌트, 에디터 도구
│  └─ Util/             Singleton, 순위 계산
├─ 03.Prefabs           UI·카드 프리팹
├─ 04.ScriptableObject  카드 데이터
├─ 05.Art               보드·플레이어·UI 아트
└─ Resources            땅 가격표 등 데이터, 플레이어 리소스
```

## 주요 화면과 기능

- **로그인**: 회원가입, 이메일 인증번호, JWT 로그인 (서버 HTTP API)
- **로비**: 방 목록(새로고침 쿨타임), 내 전적, 방 만들기·입장
- **대기방**: 준비, 방장의 게임 설정(초기 자금, 월급, 라운드, 제한 시간, 인원, 봇 수), 채팅과 말풍선
- **게임**: 주사위·말 이동 연출, 구매·건설·인수·매각 팝업, 세계여행 선택, 황금열쇠 카드, 턴 타이머, 결과 화면

## 실행 방법

1. Unity Hub에서 **Unity 6 (6000.3)** 으로 프로젝트를 엽니다.
2. 회원가입·로그인 후 로비에서 방을 만들고 플레이합니다.

## 개발 시 주의

- 처음 연 뒤 `.meta` 생성과 컴파일 에러를 확인하고 커밋합니다.
- 아래 파일은 Unity가 에디터 버전·플레이 중에 자동으로 바꾸므로 **커밋에서 제외**합니다.
    - `Packages/packages-lock.json`
    - TextMeshPro 폰트 에셋 `*SDF.asset` (Dynamic 모드)
- 같은 파일이 계속 잡히면 `git update-index --skip-worktree <경로>`로 숨깁니다.
- 커밋 타입: `fix`, `feat`, `docs`, `test`, `refactor`, `style`, `chore`, `comment`, `rename`, `remove`, `design`, `ongoing`, `temp`, `BREAKING CHANGE`

## 팀

| 이름 | 담당 |
| --- | --- |
| 김건욱 | 클라이언트 코어·네트워크, 배포 |
| 한수정 | 서버 네트워크·로비, Redis·다중 서버 |
| 이근희 | 서버 게임 로직, 봇, 타이머 |
| 김현규 | 클라이언트 UI·연출, 대기방 채팅 |
| 김희정 | 서버 인증·전적, 보드·캐릭터 비주얼 |