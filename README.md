# Eclipse Hollow Sky — Unity Build Runner

This repository is the shared Unity build machine for our projects. It owns the Unity license secrets and runs Android builds from source held in separate game repositories.

## Build a project

1. Open **Actions > Build Unity Project Android APK > Run workflow** in this repository.
2. Enter the target project's `owner/repository`, branch, Unity version, and static build method. The default values are ready for [the first building-inspection project](https://github.com/tpylman90-ctrl/Unity-project-).
3. Start the run. This workflow checks out that project, builds an Android ARM64 APK with this repository's Unity secrets, and stores the APK here as a workflow artifact. The optional prerelease is enabled by default and is also published here.

The game source stays in the game repository. It does not need its own Unity license secrets.

## Unity license and repository access

The build uses `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` from **this repository's** Actions secrets. These are already configured for this runner. Do not copy them into game repositories or commit them to source.

Public project repositories can be checked out directly. For a private project repository, add a read-only `PROJECT_REPO_TOKEN` secret here so the runner can check it out.

## Build inputs

| Input | Purpose |
| --- | --- |
| `project-repository` | GitHub `owner/repository` containing the Unity project |
| `project-branch` | Branch to build |
| `project-path` | Unity project root within that repository |
| `unity-version` | Exact Unity Editor version, including suffix |
| `build-method` | Static editor method that prepares and builds the project |
| `apk-path` | APK path produced by that method |
| `artifact-name` | Workflow artifact name |
| `publish-prerelease` | Attach the APK to a test prerelease in this build repository |

For another project, set its build method and APK path to match the code in that project. The optional private-repository token needs only read access.

## What stays here

- `.github/workflows/unity-android.yml`: central manual build and APK publishing workflow.
- `.github/workflows/source-checks.yml`: checks that this repository contains only shared build infrastructure.
- The Unity license secrets and optional private-project checkout token.

Game code, Blender source files, and generated assets belong in their respective game repositories.
