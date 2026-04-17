# CJUCatch (청주대학교 친구 찾기)

**CJUCatch**는 화면 위에 투명하게 귀여운 캐릭터를 띄워두고, 청주대학교 친구들과 실시간으로 화면상에 모여서 대화할 수 있는 멀티플레이어 바탕화면 상호작용 앱입니다. 타자 및 마우스 활동에 반응하는 다이나믹한 파티클 시스템과 물리 엔진을 탑재하고 있습니다.

## 주요 기능 (Features)
- 🖥️ 바탕화면에 투명하게 띄워두는 귀여운 캐릭터 시스템
- ⌨️ 마우스 클릭 및 키보드 타자 반응 실시간 파티클 엔진
- 🔥 타수에 따른 콤보 뱃지 및 다이나믹 파티클 효과 + 흔들림 기능
- 🌎 보안 인스턴스 코드를 통한 비공개 방 생성 및 접속
- 💬 말풍선 UI를 통한 실시간 라이브 채팅 지원
- ⚙️ 마우스 드래그를 필두로 한 유연한 캐릭터 위치 동기화

## 필요한 환경 (Requirements)
- Windows 10 또는 Windows 11
- (.NET 9.0 빌드 환경 기반)

## 빌드 및 실행 가이드 (Build & Run)

**테스트를 위해 터미널에서 실행할 때:**
```bash
dotnet run --project CJUCatch\CJUCatch.Client.Desktop
```

**깃허브 배포를 위해 최종 압축 파일(exe)을 뽑아낼 때:**
```bash
dotnet publish CJUCatch\CJUCatch.Client.Desktop\CJUCatch.Client.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=embedded -o PublishOutput
```

빌드가 완료되면 생성되는 `PublishOutput/` 폴더를 압축해서 공유하시면 됩니다!
