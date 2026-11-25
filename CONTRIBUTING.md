# Contributing to EmpathyLab VR

This is the first-week guide. Read it once, then keep the README open for versions and the simulator steps.

## What you are walking into

EmpathyLab is a stationary VR conversation used as a portfolio for tooling work: testable C#, an editor check, and a GameCI pipeline. The player stands still. They pick a line. The other person's mood and three scores move. The scene ends on a debrief.

You will not find locomotion, an inventory, or a backend. If a change needs those, it is out of scope until someone writes down why.

## Day one

1. Install Unity **6000.3.29f1** from the Hub. The exact version is `ProjectSettings/ProjectVersion.txt`.
2. Clone the repo and open the folder. The Unity project is the repository root, not a nested directory.
3. Let the Package Manager finish. The first import creates `Library/`. That folder is gitignored. Do not commit it.
4. Open `Assets/Scenes/EmpathyLab.unity` and press Play. You should see Avery, three responses, and a **Reset view** button.
5. Run the logic tests from a terminal at the repo root:

   ```bash
   dotnet test tools/dotnet/EmpathyLab.Core.Tests.csproj
   ```

6. In the editor, open **Window > General > Test Runner** and run EditMode, then PlayMode.

If PlayMode cannot find the scene, check that `Assets/Scenes/EmpathyLab.unity` is enabled in **File > Build Settings**. It is already listed in `ProjectSettings/EditorBuildSettings.asset`.

## Where code goes

| You want to... | Touch this | Do not touch |
| --- | --- | --- |
| Change how mood or scores work | `Assets/EmpathyLab/Core` | A MonoBehaviour |
| Change the words of a scene | `Assets/StreamingAssets/Scenarios/*.json` | A C# switch on the scenario id |
| Change the panel, the rig, or the room colors | `Assets/EmpathyLab/Runtime` | The scoring math |
| Change the validator window or the player build | `Assets/EmpathyLab/Editor` | A copy of the rules. Call `ScenarioValidator`. |
| Add a regression | `Assets/EmpathyLab/Tests/EditMode` for rules, `PlayMode` for the scene | A test that needs a headset |

`EmpathyLab.Core` cannot reference `UnityEngine`. If a test only needs the rules, put it in EditMode so `dotnet test` still runs it. PlayMode is for "the scene came up and the debrief appeared."

Public types need a short XML doc. The next person should be able to hover a method and know what it guarantees.

## Add a scenario

Copy `front-counter.json`. Keep `initialTension` inside 0–1 and `scoreBaseline` inside 0–100. Every non-ending node needs at least one choice that leads somewhere, and every node must be reachable from `startNodeId`. Endings set `"ending": true` and use an empty `choices` array.

Then run **EmpathyLab > Scenario Validator** or `dotnet test`. A broken link fails the test and, in CI, fails the build. Warnings (a very long line, a single choice) are printed and do not fail the build.

Point the `Scenario` object at the new file name when you want to play it. You do not register the file in code. The validator reads the whole folder.

## What not to commit

- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `build/`
- `tools/dotnet/bin/` and `tools/dotnet/obj/`
- The generated `.csproj` and `.sln` at the repo root
- A `.ulf` license file, a Unity password, or a screenshot that shows either
- A hand-edited pass count in the README. CI writes `docs/data/runs.json`.

Do commit `.meta` files. Unity matches scripts to the scene by those GUIDs. A missing meta shows up as a missing script.

After your first successful open, if Unity writes `Packages/packages-lock.json`, commit it so CI resolves the same transitive packages you did.

## CI, in one pass

`.github/workflows/ci.yml` runs on push and pull request:

1. EditMode tests, then PlayMode tests, in parallel. `Library` is cached per mode.
2. If both pass, `BuildScript.Build` validates scenarios and builds StandaloneLinux64.
3. The summary job reads the NUnit XML and the validator JSON. It does not fill in zeros when a file is missing.
4. On `main`, it appends that object to `docs/data/runs.json` and publishes `docs/` to GitHub Pages.

You need `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD` for a Personal license before any of that goes green. The README walks through the `.ulf` file. Do not put those values in the workflow file.

## Review checklist

- The new rule has an EditMode test, including the edge that should fail.
- A scenario change still passes the directory validator.
- You did not add teleport, snap turn, or continuous move.
- Text is still readable at the counter. Buttons stay large.
- You can explain the file you touched without opening it.
