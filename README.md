# Eclipse Hollow Sky — Unity Build Runner

This repository is the shared Unity build machine for our projects. It owns the Unity license secrets and builds Android APKs from source held in separate game repositories.

## Build the first inspection project

1. Open **Actions > Build Unity Project Android APK > Run workflow** in this repository.
2. Keep the default target `tpylman90-ctrl/Unity-project-`, branch `main`, Unity version, and build method.
3. Start the run. The workflow checks out the project here, generates its Blender asset library, builds an Android ARM64 APK using this repository's Unity license secrets, and uploads the APK here as a workflow artifact. A test prerelease is published in this build repository by default.

The game files remain in the project repository. **No Unity license secrets need to be copied there.** Run future builds the same way, changing the repository and build inputs for each project.

## Unity license and project access

The build reads `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` from this repository's Actions secrets. These are already configured for this runner. Do not copy them into game repositories or commit them to source.

Public project repositories can be checked out directly. For a private project repository, add a read-only `PROJECT_REPO_TOKEN` secret here.

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

The workflow only installs Blender when the selected project includes `Blender/ViewingProject/generate_assets.py`. That keeps Blender-authored source and generated Unity assets in the game repo; the runner creates the OBJ/MTL and facade atlas before the Unity build.

## Repository contents

- `.github/workflows/unity-android.yml`: central manual build and APK publishing workflow.
- `.github/workflows/source-checks.yml`: lightweight checks for this build runner.
- Unity license secrets and, optionally, a read-only private-project token.

Game code, Blender source files, and generated assets stay in their respective game repositories.
