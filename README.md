# Unity Android Build Foundation

This repository contains shared GitHub Actions automation for Unity Android projects. Game source, Blender assets, and Unity project settings belong in each game's own repository.

## Use it from a game repository

Copy `examples/unity-android-caller.yml` into the game's `.github/workflows/unity-android.yml`. Set the Unity version, build method, output APK path, and artifact name to match that project. The reusable workflow checks out the **calling game's repository**, not this build repository.

The example starts only when manually triggered from the game's **Actions** tab. The build output is uploaded as a 14-day workflow artifact. Optional prerelease publishing is off by default; turn it on in the caller only when you want a downloadable test APK attached to a release in the game repo.

## One-time license setup per game repo

For a Unity Personal license, add these repository Actions secrets:

- `UNITY_LICENSE`: contents of the activated `.ulf` license file
- `UNITY_EMAIL`: Unity account email
- `UNITY_PASSWORD`: Unity account password

Follow [GameCI's current activation guide](https://game.ci/docs/github/activation/). Keep all three values in the **game repository's** Actions secrets. The caller maps them explicitly to the reusable workflow; they are not stored in this repository or in source files.

A Personal license may require the manual CI activation process described by GameCI. The workflow checks that the secrets are present before spending time on Unity startup.

## Shared build behavior

- Unity version is an explicit input and must include the full revision suffix.
- Android APK builds use the project's own static Unity Editor build method.
- The caller chooses the output path and artifact name.
- Unity's generated `Library` directory is cached separately per calling repository and Unity version.
- The optional prerelease job requires the caller to grant `contents: write`. It publishes to the game repository, not this workflow repository.

## Repository layout

- `.github/workflows/unity-android.yml`: reusable Android build workflow.
- `examples/unity-android-caller.yml`: minimal workflow to copy into a game repository.
- `.github/workflows/source-checks.yml`: checks the workflow foundation without launching Unity.

## Current project split

- **Build foundation:** this repository.
- **Eclipse Hollow Sky prototype:** [Unity-project-](https://github.com/tpylman90-ctrl/Unity-project-).

The prototype's Unity scripts, Blender generators, assets, and project settings have been moved to the game repo. They remain in this repository's earlier commit history as well.

## Versioning

Callers currently reference `@main` so they can use the latest shared workflow. Once the workflow is settled, pin callers to a reviewed tag or commit SHA for stable builds.
