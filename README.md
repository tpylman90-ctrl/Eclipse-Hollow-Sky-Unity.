# Unity Android Build Foundation

This repository is the shared GitHub Actions runner for Unity Android projects. Unity source, Blender authoring files, and game assets live in each project's own repository. The runner checks out a selected project, builds an Android ARM64 APK with Unity 6.3, and stores it as a workflow artifact.

## Build the first inspection project

The current project is [Flight-unity](https://github.com/tpylman90-ctrl/Flight-unity).

1. Keep `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` in **this runner repository's** Settings > Secrets and variables > Actions.
2. Open **Actions > Build Selected Unity Project > Run workflow**.
3. Use the defaults to build `tpylman90-ctrl/Flight-unity` from `main`.
4. Download the APK from the workflow run's **Artifacts** section.

This central workflow uses the runner repository's existing Unity activation secrets. The project repository does not need a second copy. Public project repositories work with the default checkout token. For a private project, add a read-only GitHub token with access to that project as the `PROJECTS_READ_TOKEN` secret in this runner repository.

## Add future Unity projects

Store each project in its own repository with a Unity `Packages/manifest.json`, a matching `ProjectSettings/ProjectVersion.txt`, and a static editor build method. Run **Build Selected Unity Project** and enter the repository, ref, Unity version, build method, and artifact name. Keep game-specific source and assets out of this runner.

The reusable workflow at `.github/workflows/unity-android.yml` is also available for projects that prefer to call the runner from their own Actions workflow. That option receives the caller repository's secrets; the central manual workflow above is the path that keeps activation secrets here.

## Current project

- **Runner:** this repository; Unity build workflow and Unity activation secrets.
- **First project:** [Flight-unity](https://github.com/tpylman90-ctrl/Flight-unity); one ground-based Blender building and Android inspection viewer.

## Local development

Open a game project with the Unity version in its `ProjectVersion.txt`. Install Android Build Support, SDK/NDK Tools, and OpenJDK through Unity Hub. The GitHub workflow provides repeatable Android builds and a downloadable APK artifact.
