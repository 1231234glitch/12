# Unity 전환 프로젝트 — 원본 아트 적용 대기

[Unity 프로젝트 ZIP 다운로드](https://github.com/1231234glitch/12/raw/refs/heads/main/downloads/Project_HINA_Unity_Project_Draft.zip)

Unity 2022.3.62f1용 C# 프로젝트 초안입니다. 스프라이트 시트 9개, 애니메이션 매핑 49개, 에셋 요구사항 CSV를 포함합니다. 게임 코어 검사 57개 통과. **현재 그림은 대체 리소스이며 블루 아카이브 원본 아트 적용은 미완료입니다. Unity 에디터 실행·API 컴파일·EXE/WebGL 빌드는 이 환경에서 미검증입니다.** ZIP의 README.md와 VALIDATION.json을 확인하세요.

아래는 이전 Godot 빌드입니다.

---

# Project HINA 통합 다운로드

[통합 ZIP 다운로드](https://github.com/1231234glitch/12/raw/refs/heads/main/downloads/Project_HINA_All_In_One.zip)

이 ZIP 하나에 Windows 실행 파일, Web 빌드, Godot 4.6.3 전체 소스를 포함했습니다.

- Windows: 압축 해제 후 `Windows/Project_HINA.exe` 실행
- Web: `Web` 폴더에서 `python3 -m http.server 8000`으로 실행 후 브라우저에서 접속
- 소스: `Source/Project_HINA/project.godot`을 Godot 4.6.3에서 열기

조작: 방향키 이동·조준, J 사격·근접 공격, K 점프, L 필살기, 1 코인, Esc 일시정지.

5개 스테이지와 보스전. 첨부된 Project_HINA_v0.9 코드를 복원하고 누락된 이미지·음원을 직접 생성한 대체 리소스로 구현했습니다. 기능 검사 218개와 브라우저 키 입력·음향 출력 검증 통과. Windows 실행 자체는 Linux 호스트에서 미검증입니다.

파일 무결성은 `downloads/SHA256SUMS.txt`에서 확인할 수 있습니다. ZIP을 해제하면 `START_HERE.txt`와 각 폴더의 README가 있습니다. 소스에 포함된 Noto 글꼴은 한국어·라틴 문자 부분집합이며 원래 라이선스를 포함합니다.
