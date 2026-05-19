# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

요트(Yacht) 주사위 게임을 Unity로 구현한 프로젝트입니다. 로컬 2인 플레이와, Unity Netcode for GameObjects + Unity Relay 를 사용한 네트워크 멀티플레이를 모두 지원합니다.

- Unity 버전: **6000.1.2f1** (`ProjectSettings/ProjectVersion.txt`)
- 렌더 파이프라인: Universal Render Pipeline 17.1.0
- 주요 외부 의존성(`Packages/manifest.json`에 git URL로 핀):
  - **UniTask** (Cysharp): 모든 비동기 흐름의 기본 단위
  - **UniRx** (neuecc): `ReactiveCommand`, `ReactiveCollection`, `OnClickAsObservable` 등
  - **DiLib** (Feverfew826): `Containers.ProjectContext` 기반 정적 DI 컨테이너
  - **UnityIngameDebugConsole** (yasirkula)
  - `com.unity.netcode.gameobjects`, `com.unity.services.multiplayer`, `com.unity.addressables`, `com.unity.localization`

## 빌드 / 실행 / 테스트

이 리포지토리에는 별도의 빌드 스크립트나 CLI 테스트 진입점이 없습니다. 모든 작업은 Unity Editor(6000.1.2f1)를 통해 수행합니다.

- 에디터에서 실행 시 시작 씬은 빌드 인덱스 0번이어야 합니다. `Main.Boot()`이 `buildIndex == 0` 일 때만 정식 `MainAsync` 흐름(타이틀 → 게임)을 타고, 그 외 씬에서 바로 Play를 누르면 `PlaySceneAsync`가 해당 씬의 매니저(`LocalGameManager`/`NetworkGameManager`)를 단독 실행 모드로 구동합니다.
- 씬 파일: `Assets/_Scenes/TitleScene.unity`, `GameScene.unity`, `NetworkGameScene.unity`
- 네트워크 호스트/클라이언트 동시 테스트는 `com.unity.multiplayer.playmode`를 사용합니다 (`AuthenticatedRelayNetworkFacade`가 `Unity.Multiplayer.Playmode` 네임스페이스를 직접 참조).
- 솔루션 파일은 IDE용 두 가지가 함께 존재합니다: `YaChu.sln`(레거시 추정)과 `YachuDice.sln`. IDE에서 열 때는 `YachuDice.sln`을 사용하세요.

## 아키텍처 — 전체 실행 흐름

이 프로젝트의 가장 핵심적인 설계 의도는 **씬 하나당 매니저 클래스 하나, 그 매니저의 동작 전체를 하나의 `async UniTask` 메서드로 표현**하는 것입니다. 이 패턴을 이해하는 것이 코드를 빠르게 읽는 열쇠입니다.

### 진입점

- `Assets/_Scripts/Project/Main.cs` — `public static class Main`
  - `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` 가 붙은 `Boot()`이 유일한 OS-레벨 진입점.
  - `MainAsync(CancellationToken)`이 게임 전체 수명을 담는 무한 루프: `TitleSceneManager.WaitUserInputAsync` → 사용자 선택에 따라 `GameScene` 또는 `NetworkGameScene` 로드 → 해당 씬의 매니저 `PlayGameAsync` → 타이틀로 복귀.
  - Cancellation은 `Application.exitCancellationToken` 으로 전파됩니다.

### 씬별 매니저 메서드 규약

각 씬의 매니저는 **하나의 비동기 메서드**가 씬 전체 동작을 대표하도록 작성되어 있습니다. 새 씬을 추가하거나 기존 씬의 흐름을 바꿀 때 이 규약을 따라야 다른 씬의 코드와 일관성이 유지됩니다.

| 씬 | 매니저 | 메인 메서드 |
|---|---|---|
| TitleScene | `TitleSceneManager` | `WaitUserInputAsync(ct) → UserInput` |
| GameScene | `LocalGameManager` | `PlayGameAsync(LocalGameParameter, ct) → LocalGameResult` |
| NetworkGameScene | `NetworkGameManager : NetworkBehaviour` | `PlayGameAsync(NetworkGameParameter, AuthenticatedRelayNetworkFacade, ct) → NetworkGameResult` |

매니저는 입력 파라미터를 받아 씬 동작을 끝까지 수행하고 결과를 반환합니다. `Main`이 씬 전환의 책임을 지므로, 매니저 내부에서 `SceneManager.LoadScene` 같은 호출은 하지 않습니다.

### Assembly Definition 분할

`Assets/_Scripts/` 아래 각 폴더는 자체 `.asmdef`를 가지며, 의존 방향이 한쪽으로만 흐릅니다. 새 클래스를 만들 때 어느 asmdef에 속해야 하는지를 먼저 결정하세요.

- `YachuDice.Project` — 진입점. 다른 모든 어셈블리를 참조하는 유일한 상위 레이어.
- `YachuDice.Title`, `YachuDice.Game`, `YachuDice.NetworkGame` — 씬별 어셈블리. **씬 간 직접 참조 없음.** 씬 사이의 중계는 `Project`(`Main.cs`)가 담당.
- `YachuDice.AuthenticatedRelayNetworkFacade` — Authentication + Relay + Netcode 를 하나의 `IDisposable` 파사드로 묶음. 호스트/클라이언트 시작, JoinCode UI 표시·입력까지 포함.
- `YachuDice.Authentication`, `YachuDice.Relay`, `YachuDice.UnityServices` — 위 파사드가 사용하는 저수준 래퍼.
- `YachuDice.AddressableWrapper` — Addressables 비동기 로드를 `DisposableInstanceHandle<T>` 형태로 추상화. `NetworkManager.prefab` 등 런타임 프리팹 로딩에 사용.
- `YachuDice.Utilities`, `YachuDice.Primitive` — 공용 유틸. `OnAnyClickAsync` 등 UI/UniTask 헬퍼.
- `YachuDice.Environment.*` — 플랫폼 추상화(아래 항목 참고).

### Environment 추상화 (플랫폼 의존 + Development 플래그)

플랫폼별 동작은 컴파일러 디파인으로 분기된 `EnvironmentInjector`가 `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`에서 한 번 주입합니다.

```
Assets/_Scripts/Environment/EnvironmentInjector.cs
  → UNITY_EDITOR / UNITY_STANDALONE_WIN / UNITY_ANDROID 분기로
    Containers.ProjectContext.Set<IEnvironment>(...) 호출
```

호출 측은 항상 `Containers.ProjectContext.Get<IEnvironment>()` 로만 접근합니다 (`IEnvironment.IsMobilePlatform`, `ExitGame()` 등). 새로운 플랫폼 분기를 추가하려면 `Environment/{Platform}Environment/` 하위에 별도 asmdef를 만들고 `EnvironmentInjector`의 `#if` 사슬에 추가하세요.

`Development` 플래그도 같은 패턴이지만 **`defineConstraints: ["DEVELOPMENT_BUILD"]`** 로 두 개의 asmdef(`Development.True`, `Development.False`) 중 하나만 빌드에 포함되도록 되어 있습니다. 동일한 `YachuDice.Environment.Development` 네임스페이스/타입을 양쪽에서 정의하여 호출 측 코드는 분기 없이 `Development.IsDevelopment` 만 읽으면 됩니다.

### 네트워크 게임 흐름

`Main`에서 호스트/클라이언트 분기:

1. `AuthenticatedRelayNetworkFacade.GetDisposableInstance()` — 동시에 한 인스턴스만 살아있도록 `_isExistCurrentlyWorkingInstance` 정적 플래그로 보호.
2. 호스트: `StartHostThenShowJoinCodeAsync` — Relay 할당 + JoinCode 모달 표시.  
   클라이언트: `RetrieveJoinCodeThenConnectToHostAsync` — JoinCode 입력 모달 후 접속.
3. `NetworkManager.SceneManager.LoadScene("NetworkGameScene")` 호출 후 `LoadComplete` + `LoadEventCompleted` 이벤트를 `UniTaskCompletionSource`로 await.
4. 로드된 씬의 `NetworkGameManager`를 찾아 `PlayGameAsync(..., facade, ct)` 호출.

`NetworkGameManager`는 호스트/클라이언트 분기 메서드(`PlayGameAsHostAsync`/`PlayGameAsClientAsync`)를 따로 두고, `ReactiveCommand`와 NGO RPC(`UpdateKeepButtonsRpc` 등)를 조합해 상태 동기화를 수행합니다. 클라이언트 단절은 `OnClientDisconnectCallback` 으로 잡아 모달을 띄운 뒤 `GameElementContainer.Quit()`로 게임 루프의 `QuitCancellationToken`을 trigger합니다.

### GameElementContainer 패턴

`LocalGameManager`/`NetworkGameManager` 모두 자기 씬 안의 `GameElementContainer`(주사위·점수판·키프 버튼·롤 버튼·일시정지 UI 컨테이너)에 동작을 위임합니다. 게임 규칙 계산은 `GameManagerCommonLogic`(`CalculateCombinationScores`, `ProcessUserChoiceConfirm`)이라는 정적 클래스에 모여있어 로컬과 네트워크가 동일한 로직을 공유합니다. 규칙을 바꿀 때는 이 한 곳만 수정하면 양쪽에 반영됩니다.

게임의 핵심 상수(`DiceNum=5`, `TurnNum=12`, `RollNum=10`, `Combination` enum 12종)는 `Game/GameElementContainer.cs`의 `Constants`와 `Game/Common.cs`에 정의되어 있습니다.

## 코딩 컨벤션

- `.editorconfig` 가 리포 루트에 있으며 코드 스타일을 강제합니다. C# 파일은 **CRLF, 4-space indent, UTF-8, 마지막 줄 개행 없음, 후행 공백 제거**가 기본. 새 파일 작성 시 따르세요.
- 비동기 메서드는 `Task`가 아니라 **`UniTask`/`UniTask<T>`** 를 반환합니다. fire-and-forget 은 `.Forget()` 사용.
- 모든 장기 실행 비동기 메서드는 `CancellationToken` 을 마지막 인자로 받고, 매니저 내부에서는 `CreateLinkedTokenSource(externalCt, destroyCancellationToken, _gameElementContainer.QuitCancellationToken)` 패턴으로 합성합니다.
- DI 컨테이너 접근은 항상 `Feverfew.DiLib.Containers.ProjectContext` 를 사용 — `IEnvironment` 외의 새로운 서비스를 등록할 때도 동일한 컨테이너를 통하세요.
- 네임스페이스는 `dotnet_style_namespace_match_folder = true` 가 켜져있어 폴더 구조와 일치해야 합니다(단, `Project`, `Game`, `NetworkGame`, `Title` asmdef는 `rootNamespace`를 비워두어 글로벌 네임스페이스를 사용합니다 — 기존 컨벤션을 따르세요).
