# Project HINA — Unity 프로젝트

[최신 통합 ZIP 다운로드](https://github.com/1231234glitch/12/raw/refs/heads/main/downloads/Project_HINA_Unity_Game_Project.zip)

블루아카이브의 실제 공개 일러스트를 확인하고 히나의 머리·뿔·헤일로·제복·흰 코트·무기와 적·보스의 원본 형태 및 색상을 반영한 새 게임용 아트를 제작했습니다. 히나, 헬멧단, 스케반, 카이저 병사·드로이드·전차, 보스 5종과 소환체, 소품, 배경, 효과를 Unity에 연결했습니다. 참고 출처와 제작 이미지 해시는 ZIP의 DESIGN_SOURCES.json에 있습니다.

8개 PNG 시트, 144개 프레임, 150개 애니메이션 매핑과 디자인 관리 CSV를 포함합니다. 5개 스테이지, 보스별 기믹, 사격·점프·특수기, 파괴 가능한 엄폐물, 점수와 잠금 해제 저장, 설정 화면을 구현했습니다.

게임 코어 검사 **72개 통과**. 이미지와 매핑 검사 및 Chromium 아트 뷰어의 5개 스테이지 확인도 통과했습니다. **현재 ZIP은 Unity 프로젝트 소스입니다. 이 환경에 Unity 에디터와 활성 라이선스가 없어 Unity API 컴파일·실행·EXE/WebGL 빌드는 미검증입니다.** 아트 뷰어를 Unity 실행 화면으로 표시하지 않습니다.

Unity Hub에서 Unity **2022.3.62f1**로 ZIP 안의 `ProjectHINA` 폴더를 열고 `Assets/Hina/Scenes/Bootstrap.unity`를 실행합니다. `HINA` 메뉴에 로컬 빌드 기능이 있습니다. Unity 전용 검사 9개와 수동 GitHub Actions 빌드 템플릿도 포함했습니다.

[아트 배치 예시](Unity/ArtReview/stage_1.png) · [소스 및 실행 안내](Unity/README.md) · [검증 기록](Unity/VALIDATION.json)

파일 크기: 43,373,421 bytes. SHA-256: `654e769a0cf784c54e0d6635e6d2dc284070ce6d7e1b51812a773c9f15d5d18c`.

이전 ZIP도 downloads 폴더에 보존되어 있습니다. 체크섬 목록은 downloads/SHA256SUMS.txt에 있습니다.
