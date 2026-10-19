# Project HINA — Unity 2D 전환 프로젝트

**상태: Unity 프로젝트 초안 / 원본 아트 적용 대기 / Unity 에디터 실행·빌드 미검증.**

이 프로젝트는 Godot로 실행하지 않습니다. C# 게임 로직과 Unity SpriteRenderer, TextureImporter, Resources, AudioSource로 다시 작성했습니다. 게임 코드가 원본과 기능·연출까지 완전히 동일한 포트는 아닙니다.

## 현재 구현

5개 스테이지 데이터, 좌우 이동, 점프, 방향 사격, 공중 하향 사격, 웅크리기, 전방 자동 근접 공격, 필살기, 피격·잔기·부활·코인 컨티뉴, 전차 HP, 적 웨이브, 장애물·발판·탄환 충돌, 드롭·점수, 사전 경고 보스 공격, 호드 기둥 방어, 헤세드 4웨이브/코어 개방, 메뉴·일시정지·음량·전체 화면 UI를 작성했습니다.

원본 Godot의 폴리곤 보스 변형, 고즈의 실제/분신 공략, 모든 오디오 전환과 사용자 키 재설정은 동일하게 이식하지 않았습니다. 현재 고즈는 예고된 폭탄 구역 공격을 사용하는 임시 로직입니다.

## Unity로 열기

1. Unity Hub에서 **Unity 2022.3.62f1**과 Windows Build Support 또는 WebGL Build Support를 설치하고 Unity 라이선스를 활성화합니다.
2. **Add project from disk**에서 이 패키지의 `ProjectHINA` 폴더를 선택합니다.
3. 첫 임포트가 끝나면 `HINA > Open Demo Scene`을 선택합니다. 첫 실행에서는 부트스트랩 씬을 만들도록 구성했습니다.
4. Play를 누릅니다. `HINA > Reimport Sprite Sheets`로 시트를 다시 분할할 수 있습니다.
5. Windows/WebGL 출력은 `HINA > Build Windows x64` 또는 `HINA > Build WebGL`. 에디터와 대상 모듈이 실제 설치된 환경에서만 가능합니다.

이 클라우드 머신에는 Unity 에디터·활성화 라이선스가 없으므로 위 에디터 절차와 렌더링, 실제 Unity EXE/WebGL 빌드는 실행 검증하지 못했습니다. 포함된 ZIP은 완성 EXE가 아닌 프로젝트 소스입니다.

조작: A/D 또는 방향키 이동, W/↑ 상단·대각 조준, S/↓ 웅크리기·공중 하단 조준, J 사격·근접, K 점프, L 필살기, 1 코인, Esc 일시정지, Enter 시작.

## 스프라이트 시트와 원본 요구사항

`ProjectHINA/Assets/Hina/Resources/Hina/Sheets/`에 9개의 PNG 시트가 있으며, `atlas_manifest.json`이 셀 크기·피벗·이름·애니메이션 매핑을 정의합니다. `SheetImporter.cs`가 PNG를 다중 스프라이트로 자동 분할하고 런타임이 상태별 프레임을 선택합니다.

**현재 모든 그림은 기존 빌드의 생성형 대체 그림입니다. 블루 아카이브 공식 원본 리소스를 적용했다고 주장하지 않습니다.** 게임 내 오브젝트에 필요한 원본 이미지가 첨부 파일에 포함되지 않았습니다. `ASSET_REQUIREMENTS.csv`의 `original_asset_status`가 `MISSING`인 이유입니다. 기존 파일명의 해시만으로 실제 그림을 복원할 수는 없습니다.

원본 적용 대상은 히나, 헬멧단·스케반·자동기계·전차, 비나·페로로지라·호드·고즈·헤세드, 소품·발판·잔해·배경·탄환·드롭·효과입니다. 맵에 배치한 모든 대상의 원본 이미지 또는 사용할 공식 참고 이미지/시트가 필요합니다. 스케반 저격병·레일건 전차·소품 세부 디자인의 원본 대응도 확인해야 합니다. 검토용 `SPRITE_REVIEW.png`와 CSV 명세를 함께 제공합니다.

원본 시트 교체 방식:

- 현재 시트와 같은 크기·셀 순서의 PNG를 `player.png`, `enemies.png`, `bosses.png`, `props.png`, `backgrounds.png`, `ground.png`, `title.png`, `effects.png`, `items.png`로 준비합니다.
- 에디터의 Resources/Hina/Sheets에서 PNG를 교체하고 `HINA > Reimport Sprite Sheets`를 선택합니다.
- 크기·프레임 수가 다르면 먼저 `atlas_manifest.json`의 해당 셀 정의와 애니메이션 프레임명을 수정해야 합니다. 원본 이미지에서 새 시트를 제작하는 작업은 실제 원본을 받은 후 진행해야 합니다.
- 이 저장소 안에서 재생성하려면 `Tools/prepare_unity_assets.py --original-dir /path/to/sheets`를 사용합니다. 단독 ZIP에서는 기존 시트를 교체할 수 있습니다. 대체 그림을 처음부터 다시 생성하는 모드는 이전 프로젝트의 Godot 대체 리소스를 입력으로 사용합니다. `original_sources.json`에 각 atlas ID의 `source` 출처를 기록해야 교체를 허용합니다. 도구는 제공된 리소스를 공식 원본으로 자동 인증하지 않습니다.

폰트는 Noto Sans CJK이며 OFL 라이선스를 포함했습니다. 음악·효과음은 직접 합성한 대체 음원입니다.

## 검증

`Tools/run_core_tests.py`는 Unity에 의존하지 않는 **실제 게임 로직**을 .NET 8로 컴파일하고 테스트합니다. 57개 검사 통과. 결과: `core_test_results.json`. Unity API 컴파일·스프라이트 임포트·렌더링 검증과는 별개입니다.

```sh
python3 Tools/run_core_tests.py
# 또는 일반 개발 머신에서
cd Tests && dotnet run
```

`DOTNET_ROOT` 또는 PATH의 .NET 8 SDK를 사용합니다. 이 머신에서만 사용하는 기본 SDK 경로도 실행 스크립트에 fallback으로 있습니다. .NET은 게임 로직 검사용이며 Unity 게임 실행을 대신하지 않습니다.

Unity Test Runner용 `Assets/Hina/Tests/Editor/CoreTests.cs`도 포함했습니다. 해당 NUnit 4개 검사는 Unity 에디터가 없어 미실행 상태입니다. 실행 결과를 만들어 넣지 않았습니다.

## 핵심 파일

- `Scripts/Core/GameModel.cs`: 엔진 독립 게임 시뮬레이션
- `Scripts/Runtime/HinaGame.cs`: Unity 입력·월드 렌더링·UI·음향
- `Scripts/Runtime/AtlasLibrary.cs`: 시트/상태 애니메이션 조회
- `Editor/SheetImporter.cs`: 시트 자동 분할
- `Editor/ProjectSetup.cs`: 씬 생성·빌드 메뉴
- `Resources/Hina/stages.json`: 5개 스테이지
- `Resources/Hina/atlas_manifest.json`: 시트·애니메이션 매핑/출처 상태
