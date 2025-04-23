# YachuDice
주사위 게임 요트(Yacht)를 구현한 것입니다.\
![Picture of YachuDice](./Images/Image01.PNG)\
이 프로젝트는 친구들 사이에 51 Worldwide Games의 Yacht Dice가 유행하던 시기에 간단하게 구현해 본 것을 정리하여 업로드한 것입니다.

## 상세 설명
`await/async` 키워드와 [UniTask](https://github.com/Cysharp/UniTask)를 중점적으로 사용하여 구현하였습니다.\
씬 별로 AssemblyDefinition으로 코드가 분리되어 있어, 특정 부분의 구현 복잡성이 프로젝트 전체로 전파되지 않게 구성되어 있습니다.\
Title 씬의 어셈블리와 Game 씬의 어셈블리를 구분하고, 실행의 중점이되는 Project 어셈블리를 두어, 전체 게임 실행 흐름 관리 및 어셈블리 간의 중계를 하도록 작성되었습니다.\
실험의 일환으로 게임 전체의 실행 흐름의 시작점이 되는 `Main`클래스를 만들어봤습니다.\
Project 어셈블리의 `Main.cs` 파일에는 `Main`이라는 정적 클래스가 구현되어있고, 실행의 시작점이 됩니다.\
`Main`클래스에 `Boot()` 메서드를 만든 뒤, `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`를 붙여, 실행의 시작점이 되도록 구성하였습니다.\
각 씬마다 실행 흐름이 작성된 매니저 클래스들이 있고, 각 매니저 클래스에는 기능의 중심이 되는 하나의 비동기 메서드가 있습니다.\
이 비동기 메서드는 씬에 필요한 입력 인자를 받아, 씬 전체의 동작을 수행한 뒤 결과를 반환합니다. 이것으로 하나의 씬의 동작을 하나의 비동기 메서드로 단순화하여 생각할 수 있게 됩니다.\
씬의 전체 구현을 단순하게 정리할 수 있게 되는 것으로, 프로젝트 전체 시스템 설계 시 사고를 단순하게 유지할 수 있을 것으로 기대됩니다.

스크립트 에디터로 VisualStudio 사용 시, 기본적인 코딩 컨벤션 검사가 적용되도록 .vsconfig 파일을 포함하였습니다.

3D 모델링된 주사위를 던지고 결과를 확인하도록 구현하였습니다.
주사위의 윗면을 선정하는 데에는 다음과 같은 방법을 사용했습니다.
 1. 각 면의 가운데에 빈 GameObject를 추가합니다.
 2. 그렇게 추가한 6개의 GameObject 중 가장 Y 좌표 값이 큰 것을 선택하면 어떤 면이 윗면인지 알 수 있습니다.
 
족보 검사 메서드는 ChatGPT가 초안을 작성했습니다.

## 기타 사항
.editorconfig 파일에는 제가 선호하는 코드 서식이 지정되어 있습니다.

## ToDo 리스트
 - BGM 재생에 MusicStack 프로젝트 사용하기.
 - 선수 이름 및 인원 수 지정.
 - 주사위를 다시 굴릴 때, 다시 굴린 주사위들이 홀드해둔 주사위들에 부딪혀 움직임이 덜 즐거운 문제를 해결.
 - 코드 정리 및 주석 추가.
 - 주사위를 던지는 더 재미있는 방법을 생각하기.
 - 족보 달성 시 화려한 UI를 표시하여 더 신나게 하기.
 - 프로토타입 같은 UI 를 개선하기.
 - 게임이 끝난 후 다시 시작하는 흐름 만들기.
 - PC 버전 빌드.
 - 컨트롤러 지원.

## 사용된 오픈소스 라이센스(축복 받으세요.)
Dice by TheBoss009SS -- https://sketchfab.com/3d-models/dice-d796ac8f56db4dc78ed18be534939225 -- License: Attribution 4.0  
Plastic lid snap.wav by SDLx -- https://freesound.org/s/211171/ -- License: Attribution 3.0  
Background Music by Migfus20 -- https://freesound.org/s/609562/ -- License: Attribution 4.0  
UniTask by Cysharp -- https://github.com/Cysharp/UniTask -- License: MIT  
UniRx by neuecc -- https://github.com/neuecc/UniRx -- License: MIT  
