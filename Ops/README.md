# 전용 서버 배포

서버 프로세스 하나가 2인 방 하나를 운영한다. Windows·Linux 클라이언트는 운영자가 전달한 참가 코드를 입력한다. 서버가 UGS에 접속해 코드를 발급하므로 플레이어에게 서버 주소를 배포할 필요가 없다.

## Linux 호스트 준비

Linux x86_64 호스트에 Unity standalone 플레이어가 사용하는 시스템 라이브러리, Bash, systemd 사용자 서비스, SSH, tar, coreutils, flock이 필요하다. 서비스 계정은 게임 전용으로 만들고, 로그아웃 후에도 서비스가 실행되도록 관리자가 해당 계정에 linger를 활성화한다. UGS Authentication·Lobby·Relay에 대한 외부 통신이 가능해야 한다.

서비스 계정으로 다음을 한 번 실행한다. 서비스 파일은 저장소의 Ops 디렉터리에서 복사한다.

```bash
install -Dm644 Ops/divinewar-server@.service ~/.config/systemd/user/divinewar-server@.service
systemctl --user daemon-reload
systemctl --user enable divinewar-server@development.service
```

운영 호스트는 마지막 명령의 환경명을 `production`으로 지정한다. 두 환경은 서로 다른 호스트 또는 서비스 계정 사용을 권장한다. 첫 기동은 배포가 수행한다.

## 배포와 참가 코드

1. GitHub Environments의 대상 환경에 변수 `DIVINEWAR_SSH_HOST`, `DIVINEWAR_SSH_USER`를 설정한다. Secrets에는 전용 배포 개인 키 `DIVINEWAR_SSH_KEY`와 관리자가 확인한 서버 공개 호스트 키 한 줄 `DIVINEWAR_KNOWN_HOSTS`를 등록한다. 해당 공개 배포 키는 서비스 계정에서 사용할 수 있어야 한다.
2. `main`의 **Multiplayer** 빌드가 성공하면 같은 커밋에서 **Deploy dedicated server**를 수동 실행한다. 환경과 빌드 실행 ID를 입력한다. 운영 배포는 운영 서버 아티팩트와 GitHub 환경 승인이 필요하다. 재시작 때 진행 중인 경기는 종료되므로 빈 방에서 배포한다.
3. 워크플로는 서버 아카이브를 전송하고 체크섬을 확인한 다음 사용자 서비스를 재시작한다. 새 프로세스가 방 코드를 발급하면 배포에 성공한다. 2분 안에 발급되지 않으면 이전 릴리스를 복구한다.
4. 운영자가 호스트에서 다음 명령으로 새 코드 발급을 확인하고 참가자 두 명에게 현재 코드를 전달한다. 코드는 경기마다 바뀐다.

```bash
journalctl --user -u divinewar-server@development.service -f -o cat | grep --line-buffered -F '[DivineWar] JOIN_CODE='
```

빌드, 익명 인증 설정, 플랫폼별 테스트 절차는 [멀티플레이 운영 문서](../Docs/Multiplayer.md)를 참조한다.
