# Unity 빌드 CI

`unity-build.yml`은 수동 실행 전용 GitHub Actions 템플릿입니다. 저장소 `.github/workflows/unity-build.yml`에 설치하면 Actions에서 Windows64 또는 WebGL을 선택합니다.

Unity 2022.3.62f1과 GameCI v4로 Editor/PlayMode 검사를 실행한 후 빌드 결과를 Actions 아티팩트로 업로드합니다. 테스트 또는 빌드 실패 시 실행파일이 제공되지 않습니다. 이 프로젝트를 제작한 환경에서는 워크플로를 실행하지 않았습니다.

사용 중인 Unity 라이선스에 맞는 GameCI 활성화 방법을 확인하고 저장소 Actions Secrets에 필요한 UNITY_LICENSE, UNITY_EMAIL, UNITY_PASSWORD를 안전하게 설정합니다. 라이선스 조건과 CI 사용 가능 여부는 사용하는 Unity 플랜과 활성화 방식에 따릅니다. 값은 프로젝트 파일이나 채팅에 넣지 않습니다. 참고: https://game.ci/docs/github/activation/

로컬에서는 활성화된 Unity 2022.3.62f1에 Windows/WebGL 모듈을 설치하고 프로젝트의 HINA 빌드 메뉴를 사용할 수 있습니다.
