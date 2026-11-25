# Engineering guide

This is the tooling, test, and CI write-up. The repository home page is the player guide in `README.md`.

EmpathyLab VR is a small, stationary VR conversation. You stand at a counter or a doorway while another person is upset. You look at a response and select it. Their mood moves, and the scene ends on a debrief that scores empathy, clarity, and de-escalation and explains each line.

The people in the two shipped scenes are a bakery customer and a neighbor. There are no weapons, no violence, and no law-enforcement imagery. The point of the repository is the engineering around that scene: plain C# rules, tests, an editor validator, and a GameCI pipeline.

## Versions

Looked up on 24 November 2026. Do not bump these because a newer number exists. Bump them when you have re-read the release notes.

| Piece | Version | Why this one |
| --- | --- | --- |
| Unity Editor | **6000.3.29f1** (changeset `96d2fe17529c`) | Current Unity 6.3 LTS, released 19 November 2026. Standard support runs through December 2027. Unity 6.0 LTS left standard support on 16 October 2026. Unity 6.6 is an update release, not an LTS. |
| XR Interaction Toolkit | **3.7.0** | Latest released toolkit that supports Unity 6000.0 and later. Released 16 October 2026. |
| OpenXR Plugin | **1.19.0** | Latest released OpenXR plugin for Unity 6000.0 and later. Released 16 October 2026. |
| XR Plugin Management | **4.6.1** | The version Unity lists as released for the 6000.3 editor. |
| Input System | **1.20.1** | The version Unity lists as released for the 6000.3 editor. XRI 3.7.0 requires Input System 1.8.1 or newer, uGUI 2.0.0 or newer, and XR Core Utils 2.4.0 or newer. Those last two resolve as transitive packages. |
| Test Framework | embedded with the editor | Since 1.5.1 the Test Framework is a core package fixed to the editor. It is not pinned in `Packages/manifest.json`, so CI uses the copy inside the 6000.3 image. |
| GameCI | `game-ci/unity-test-runner@v4` and `game-ci/unity-builder@v4` | The GameCI docs fetched on 24 November 2026 label v4 as the current major for both actions. `actions/cache@v4`, `actions/checkout@v4`, and `actions/upload-artifact@v4` match those pages. |

Built-in render pipeline, not URP. The room is primitives and text. A render pipeline package would add import time to CI without changing the training loop.

OpenXR is installed and **not** enabled as an active loader. Headless CI and the simulator do not need a headset runtime. Enable it when you build for a device: **Edit > Project Settings > XR Plug-in Management**, Standalone tab, check OpenXR.

Scripting backend for Standalone is Mono. This CI build exists to prove the project compiles and produces a player. IL2CPP is a device-build decision, and it makes the Linux image slower and easier to break.

## What to capture

There are no screenshots in the repo. Add yours under `docs/images/` after you have run the scene, and link them here. Capture these, in this order:

1. Game view at 1280×720. Avery's opening line, the mood label reading upset, and three response buttons. Include the gaze reticle.
2. The same view after the line that names the missed time. The mood label should have moved.
3. The debrief, with empathy, clarity, and de-escalation visible, plus at least two "why this helped or hurt" lines.
4. A GIF of one full run, about 8–12 seconds: look at a response, select it, land on the debrief. If the XR Interaction Simulator overlay is on screen, leave it in the frame so a reviewer can see you were not in a headset.
5. **EmpathyLab > Scenario Validator** after a clean validate, then one deliberate broken file if you want a before/after.
6. After secrets are in place: the GitHub Actions run, and this dashboard once a `main` push has published it.

Do not draw fake pass counts on the images.

## Architecture

```mermaid
flowchart LR
  json["Scenario JSON"] --> loader["ScenarioLoader"]
  loader --> graph["DialogueGraph"]
  graph --> session["ScenarioSession"]
  mood["MoodModel"] --> session
  score["ScoringEngine"] --> session
  session --> presenter["ScenarioPresenter"]
  presenter --> ui["World-space UI"]
  gaze["Gaze ray and simulator"] --> ui
  ui --> session
  session --> debrief["Debrief"]
  validator["ScenarioValidator"] --> json
  validator --> window["Validator window"]
  validator --> build["BuildScript"]
  build --> player["StandaloneLinux64"]
  tests["EditMode and PlayMode tests"] --> session
  ci["GitHub Actions"] --> tests
  ci --> build
  ci --> pages["Pages dashboard"]
```

Dependency direction, enforced with assembly definitions:

```text
EmpathyLab.EditorTools  -->  EmpathyLab.Core
EmpathyLab.Runtime      -->  EmpathyLab.Core
EmpathyLab.Tests.EditMode  -->  EmpathyLab.Core
EmpathyLab.Tests.PlayMode  -->  EmpathyLab.Runtime --> EmpathyLab.Core
```

`EmpathyLab.Core` sets `noEngineReferences`. The dialogue graph, mood model, and scoring engine do not know about scenes, canvases, or XR. That is why the same files run under `dotnet test` and under the Unity Test Framework.

## Run it without a headset

This is a design choice, not a temporary shortcut. The scene has to be demonstrable on a laptop, and CI has no headset.

XRI 3.7.0 renamed the old tool. The package still contains a sample named **XR Device Simulator**, and its own description says it is the older classic simulator and will be removed. The supported sample is the **XR Interaction Simulator**. `XRDeviceSimulator` was marked deprecated in XRI 3.5.0 (16 March 2026). This project standardizes on the Interaction Simulator. Same job: drive the headset and the controllers from a keyboard and mouse.

The scene also runs before that sample is imported. A gaze ray sits on the camera, choices are world-space buttons, and the keys 1, 2, and 3 select them. CI uses that path. The simulator is what you turn on when you want the interview demo to show tracked controllers.

### First open

1. Install Unity **6000.3.29f1** with the **Linux Build Support** module if you want a local player, plus **Mac** or **Windows** build support for your machine.
2. Open this folder in Unity Hub. Wait until package resolution finishes.
3. Open `Assets/Scenes/EmpathyLab.unity`.
4. Press Play. You are standing still, looking at Avery. There is no stick locomotion, on purpose.
5. Look at a response and click it, or press 1, 2, or 3. **Reset view** puts the camera back to eye height, facing the counter.
6. To switch scenes, select the `Scenario` object and change `scenarioFileName` to `shared-wall.json`.

### XR Interaction Simulator

From the [XRI 3.7 simulator overview](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.7/manual/xr-interaction-simulator-overview.html):

1. **Window > Package Manager**, select **XR Interaction Toolkit**, and import the **XR Interaction Simulator** sample.
2. Run **EmpathyLab > Add XR Interaction Simulator To Scene**. The menu looks for that prefab, and only falls back to the legacy XR Device Simulator prefab if the new one is not imported.
3. Or enable **Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit > Use XR Interaction Simulator in scenes**. Leave **Instantiate In Editor Only** on, so a headset build does not ship the simulator.
4. Press Play. **Tab** switches between FPS mode and device mode. **[** and **]** activate the left and right controllers. Press a bracket key twice to switch that hand between controller and hand mode. The on-screen card lists the rest.
5. You still do not walk. Look at a response and select it with the controller trigger. Remove the simulator object before a device build.

Active Input Handling is **Input System Package**. XRI needs it. Keyboard shortcuts in the presenter use `Keyboard.current`, so they keep working with that setting.

Comfort rules baked into the scene: no artificial locomotion, world-space text at a size you can read from the counter, large buttons, and a reset that only recenters the view.

## Tests

### On this machine, without the editor

The EditMode tests do not reference `UnityEngine`. From the repo root:

```bash
dotnet test tools/dotnet/EmpathyLab.Core.Tests.csproj
```

That runs the dialogue graph, mood model, scoring engine, session, and validator, including invalid node ids, empty choices, score clamps, broken links, dead ends, missing speaker text, and unreachable nodes. One test loads every file in `Assets/StreamingAssets/Scenarios` and fails if any link is broken or any node cannot be reached.

### In the Unity Editor

**Window > General > Test Runner.**

- EditMode: the same cases as `dotnet test`.
- PlayMode: the scene loads, the object named `XR Origin` has a camera, and choosing through the presenter reaches the debrief.

PlayMode does not require the simulator sample. It calls `ScenarioPresenter.ChooseFirst`.

### In CI

Every push and pull request runs EditMode, then PlayMode, then `EmpathyLab.EditorTools.BuildScript.Build` for **StandaloneLinux64**. The build method validates scenario JSON first and returns exit code 1 when the report has errors. A later step reads `Logs/scenario-validation.json` and fails the job if that file is missing or `errorCount` is greater than zero, so a swallowed Unity exit code still fails the pipeline. The same job then requires the player file under `build/StandaloneLinux64`. GameCI v4 passes that player path as `-customBuildPath` (`build/StandaloneLinux64/StandaloneLinux64`, no extension on v4). A local menu build, which has no custom path, writes `build/StandaloneLinux64/EmpathyLabVR`.

Coverage is off. The current GameCI test-runner docs say code coverage has been the cause of compile errors and a PlayMode crash on Unity 6. Turn it on only when you have a green run without it.

`Library` is cached per test mode and per build target. GameCI's builder docs say cache entries are immutable and that a shared Library across platforms thrashes, so the key includes the mode and a hash of `Assets`, `Packages`, and `ProjectSettings`.

## Secrets

GameCI does not store your Unity login. You do, as GitHub Actions secrets. These steps match the [activation](https://game.ci/docs/github/activation/), [test runner](https://game.ci/docs/github/test-runner/), and [builder](https://game.ci/docs/github/builder/) pages as fetched on 24 November 2026. Both actions are `@v4`.

Use one license type. Do not mix them.

### Personal license

1. Install Unity Hub and sign in with the account that CI should use.
2. **Unity Hub > Preferences > Licenses > Add > Get a free personal license.** Click through until a `.ulf` file exists. A license row in the Hub is not enough.
3. Find the file:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
4. On GitHub: **Settings > Secrets and variables > Actions**. Create:
   - `UNITY_LICENSE` — the entire contents of the `.ulf` file
   - `UNITY_EMAIL` — the Unity account email
   - `UNITY_PASSWORD` — the Unity account password
5. Sign out of Unity Hub afterward. Hub 3.7.0 and later has no Return license action for a Personal seat. Signing out returns it, so your laptop and CI are not both holding it. If CI says the seat is taken, sign out locally and follow GameCI's "Returning a license" note.
6. Use a password made of letters and numbers. GameCI has seen failures when `UNITY_PASSWORD` contains special characters.

The workflow already passes those three secrets into both actions. You do not add a separate activate job. The v4 actions activate inside the test and build steps.

### Professional license

Return the seat in **Unity Hub > Preferences > Licenses** before the first CI run. Then create `UNITY_SERIAL` (the key that looks like `XX-XXXX-XXXX-XXXX-XXXX-XXXX`), `UNITY_EMAIL`, and `UNITY_PASSWORD`. Do not also set `UNITY_LICENSE`. If you switch, edit `.github/workflows/ci.yml` and replace the `UNITY_LICENSE` lines with `UNITY_SERIAL` as the GameCI professional example shows.

### Floating license server

If your org runs a license server, delete the email, password, and license env vars and set `unityLicensingServer` on both actions to that URL. The docs say the action acquires a seat before the step and returns it after.

### GitHub settings the workflow needs

1. **Settings > Actions > General > Workflow permissions > Read and write.** The test runner uses `GITHUB_TOKEN` to publish a check run, and the summary job pushes `docs/data/runs.json` on `main`. A push made with the default token does not start a second workflow.
2. **Settings > Pages > Build and deployment > Source: GitHub Actions.** The `dashboard` job uses `actions/upload-pages-artifact@v3` and `actions/deploy-pages@v4`.
3. After the repo exists on GitHub, the badge line is:

```markdown
[![CI](https://github.com/OWNER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/OWNER/REPO/actions/workflows/ci.yml)
```

Replace `OWNER/REPO`. Until the first run, GitHub has no status to show. Do not paste a green badge from somewhere else.

If `main` is protected and the Actions token cannot push, the Pages site for that run still updates, and the git history of `docs/data/runs.json` does not. Trends across runs need that push, or a pull request that commits the file.

## Add a scenario

1. Copy `Assets/StreamingAssets/Scenarios/front-counter.json`.
2. Change `id`, `title`, `setting`, `npcName`, `npcRole`, `initialTension` (0–1), and `scoreBaseline` (0–100).
3. Give every node an id, speaker, and line. Mark real endings with `"ending": true` and `"choices": []`. A node with no choices and `ending` false is a dead end.
4. Point every `nextNodeId` at a node that exists. Every node must be reachable from `startNodeId`.
5. Keep lines under 280 characters and choice labels under 140. A single-choice node is a warning, not a failure.
6. **EmpathyLab > Scenario Validator**, or `dotnet test`. The directory test fails the run if the new file has a broken link or an unreachable node.
7. On the `Scenario` object, set `scenarioFileName` to the new file name.

No C# change is required.

## Measured results

These cells are filled from CI. They are empty on purpose. Do not type a number in here until you can point at a run.

| Signal | Latest `main` run |
| --- | --- |
| Commit | _pending_ |
| Passed tests | _pending_ |
| Failed tests | _pending_ |
| Test duration (seconds) | _pending_ |
| Validator warnings | _pending_ |
| Validator errors | _pending_ |

`docs/data/runs.json` starts as `[]`. The summary job appends one object per run: commit SHA, test pass/fail/skip counts, the Unity test-run duration, and validator warning and error counts. Missing XML or a missing report stays `null` and the result is `incomplete` or `failed`. The script does not substitute zeros.

The dashboard is the `docs/` folder, published to GitHub Pages from `main`.

## Project map

| Path | What it is |
| --- | --- |
| `Assets/EmpathyLab/Core/MoodModel.cs` | Tension, 0–1, and the calm / steady / upset bands. |
| `Assets/EmpathyLab/Core/ScoringEngine.cs` | Empathy, clarity, and de-escalation, each clamped to 0–100. |
| `Assets/EmpathyLab/Core/DialogueGraph.cs` | Nodes, choices, and the JSON loader. |
| `Assets/EmpathyLab/Core/ScenarioSession.cs` | Applies a choice, then builds the debrief. |
| `Assets/EmpathyLab/Core/ScenarioValidator.cs` | Dead ends, missing speaker text, broken links, unreachable nodes. Writes the JSON report shape. |
| `Assets/EmpathyLab/Runtime/ScenarioPresenter.cs` | World-space UI. The only thing that talks to a session from the scene. |
| `Assets/EmpathyLab/Runtime/XrRigBootstrap.cs` | Stationary XR Origin, gaze ray, reset. No locomotion. |
| `Assets/EmpathyLab/Runtime/RoomDressing.cs` | Colors the primitives at play time. |
| `Assets/EmpathyLab/Editor/ScenarioValidatorWindow.cs` | **EmpathyLab > Scenario Validator**. |
| `Assets/EmpathyLab/Editor/BuildScript.cs` | Batchmode `Build`. Validate, then `BuildPipeline.BuildPlayer`. |
| `Assets/EmpathyLab/Editor/ProjectMenus.cs` | Simulator prefab and a package-version log. |
| `Assets/StreamingAssets/Scenarios/` | `front-counter.json`, `shared-wall.json`. |
| `Assets/Scenes/EmpathyLab.unity` | The room, Avery, the rig, the dialogue anchor. |
| `tools/dotnet/` | Runs the EditMode sources on the local SDK. |
| `tools/ci/` | Turns NUnit XML and the validator report into one JSON object. |
| `docs/` | The Pages dashboard. |
| `.github/workflows/ci.yml` | Test, build, summary, Pages. |

After the first successful open, commit `Packages/packages-lock.json` if Unity creates it. That locks the transitive packages CI will resolve.

## Lessons learned

- The rules of a conversation are not a MonoBehaviour. Putting them in an assembly with no engine references made the edge cases runnable without a headset, and it made the validator and the build share one implementation.
- The XR Interaction Simulator is a sample prefab, not a class you can add from code. The scene therefore has a gaze-and-click path that CI can drive, and a menu that drops the official prefab in once the sample is imported.
- OpenXR belongs in the manifest before it belongs in the active loader list. An active loader with no runtime makes headless PlayMode fail for a reason that has nothing to do with the scenario.
- A validator that only runs in a window will be skipped. Running it inside the batchmode build method, and checking the JSON again in the workflow, is what makes a broken link fail the pipeline.
- GameCI cache keys have to include the target. The docs are explicit that an entry is saved once per key, and that a Library from one platform is the wrong Library for another.
- Coverage stayed off because the current test-runner docs name it as a source of Unity 6 crashes. A green suite you trust is worth more than a coverage badge you have not measured.
- The README does not contain a pass rate. The dashboard reads the file CI writes. Until that file has rows, the honest number is "not measured yet."
