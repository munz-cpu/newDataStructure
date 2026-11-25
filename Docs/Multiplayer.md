# DivineWar 멀티플레이

타이틀에서 직업과 스킬을 고른 뒤 참가 코드 입력란 위의 **방 생성**을 누른다. 입력란에 표시된 코드를 친구에게 보내고, 친구가 같은 코드를 입력해 **코드로 참가**를 누르면 전투가 시작된다. 방을 만든 플레이어는 청록 팀을 맡고 친구는 적색 팀을 맡는다. 대기 중 **취소**를 누르면 방이 닫힌다.

## 전용 서버

서버 한 개가 두 팀의 전투를 진행한다. 서버는 UGS에 비공개 세션을 만들고 로그에 참가 코드를 출력한다. 두 플레이어는 Windows 또는 Linux 타이틀 화면에서 같은 코드를 입력하고 직업을 고른다. 플레이어 두 명과 서버가 각자 세션 슬롯 하나를 사용한다. 서버가 팀을 배정한 뒤 양쪽 명령을 검증하고 전장 상태를 전송한다. 경기 종료 또는 연결 중단 뒤에는 새 방을 연다. AI 연습전은 혼자 시작할 수 있다.

## Unity Cloud 설정

1. 저장소의 `ProjectSettings/ProjectSettings.asset`에 기록된 Cloud Project ID와 같은 프로젝트를 Unity Dashboard에서 선택한다.
2. UGS 기본 환경인 `production`에서 Authentication 익명 로그인 및 Multiplayer의 Lobby·Relay 사용 상태를 확인한다.
3. 서버와 플레이어는 같은 환경과 프로토콜 버전을 사용해야 한다. 개발 빌드와 Editor에서 별도 환경을 사용하려면 Dashboard에 환경을 생성한 뒤 서버와 두 플레이어를 모두 같은 `DIVINEWAR_UGS_ENVIRONMENT` 값으로 실행한다. 운영 빌드는 기본 환경을 사용한다. 서버는 컴파일 시 `DIVINEWAR_DEDICATED`로 구분한다. 각 서버 인스턴스에 별도의 인증 프로필 또는 서비스 계정을 위한 OS 사용자 디렉터리를 사용한다.

## 빌드 및 배포

GitHub Actions에 GameCI의 `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`를 등록한다. GitHub의 `production` 환경에는 Required reviewers를 설정한다. [GameCI 라이선스 설정](https://game.ci/docs/github/activation/)

| 실행 시점 | 결과 |
| --- | --- |
| main 대상 PR | Unity 컴파일과 Main 씬의 전투 규칙·직렬화 검사를 실행한다. 결과는 `battle-validation`에 기록한다. 포크 PR은 저장소 라이선스를 사용할 수 없어 이 검사가 제한된다. |
| main에 반영 또는 수동 실행 | Linux 서버, Linux와 Windows x64 클라이언트를 빌드한다. Linux 네이티브, Windows 네이티브, Linux·Windows 혼합 조합에서 전용 서버 접속을 검사한다. 혼합 검사의 Windows 실행 파일은 Wine으로 실행한다. |
| main에서 production 선택 | 같은 커밋의 개발 검사와 환경 승인을 거쳐 운영용 서버 tar.gz와 Windows ZIP, Linux tar.gz를 생성한다. |
| Deploy dedicated server 수동 실행 | 선택한 환경에 성공한 빌드의 서버를 설치하고 새 방 등록을 검증한다. 호스트와 배포 키 준비는 [전용 서버 배포 안내](../Ops/README.md)를 따른다. |

## 로컬 실행

Unity 6000.6.3f1의 Linux 및 Windows Build Support가 필요하다. 다른 에디터가 사용 중인 프로젝트는 별도 체크아웃에서 빌드한다.

```bash
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" -buildTarget StandaloneLinux64 -executeMethod DivineWarBuild.ValidateAndBuildDevelopment
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" -buildTarget StandaloneLinux64 -executeMethod DivineWarBuild.ServerDevelopment
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" -buildTarget StandaloneWindows64 -executeMethod DivineWarBuild.WindowsDevelopment
./Builds/server/development/DivineWarServer.x86_64 -batchmode -nographics -logFile -
```

운영자는 서버 로그의 `[DivineWar] JOIN_CODE=` 뒤에 표시되는 코드만 두 참가자에게 전달한다. Windows 사용자는 배포 ZIP 전체를 풀고 `DivineWar.exe`를 실행한다. Linux 사용자는 패키지를 풀고 `DivineWar.x86_64`를 실행한다. 두 사람 모두 타이틀에서 직업을 고르고 코드를 입력한 뒤 참가한다.

## 자동 검증

```bash
bash .github/scripts/relay-smoke.sh Builds/development/DivineWar.x86_64 --offline-only
bash .github/scripts/relay-smoke.sh Builds/development/DivineWar.x86_64 Builds/server/development/DivineWarServer.x86_64
xvfb-run -a bash .github/scripts/relay-smoke.sh Builds/development/DivineWar.x86_64 Builds/server/development/DivineWarServer.x86_64 --windows-peer Builds/windows/development/DivineWar.exe
```

온라인 검사는 서버와 플레이어 두 개를 실행한다. 서로 다른 인증 ID와 팀 배정, 양 팀 명령의 서버 처리, 양쪽에서 받은 상태, 두 번째 방 입장까지 검증한다. Windows 네이티브 CI는 같은 서버 코드를 Windows 테스트 프로세스로 빌드해 PowerShell로 실행한다. 실제 UGS 환경에서 검사가 성공해야 운영 빌드로 진행할 수 있다.

UGS 관리 권한을 준비하는 동안에는 `DivineWarBuild.LocalValidationLinux`와 `DivineWarBuild.LocalValidationWindows`로 별도의 로컬 검사용 실행 파일을 만들 수 있다. 위 명령의 경로를 `Builds/qa/` 아래로 바꾸면 전용 서버와 클라이언트가 같은 컴퓨터에서 두 경기를 실행한다. 이 검사는 팀 배정, 전투 명령, 전장 상태 전송과 재입장을 다룬다. UGS 인증과 Relay 연결은 일반 개발 빌드로 별도 검증한다.
