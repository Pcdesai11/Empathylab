# Interview prep

Study sheet for a Software Engineer conversation about this repo. The numbers below that describe CI are blanks on purpose. Quote a pass count only after you have a GitHub Actions run in front of you, or after you have just run `dotnet test` and can say it was local.

## Six things to say

- EmpathyLab is a stationary VR conversation. The player looks at a response, the other person's tension moves, and a debrief scores empathy, clarity, and de-escalation. The scenes are a late bakery order and a neighbor at the door. No weapons, no violence, no enforcement imagery.
- The rules live in `EmpathyLab.Core`, an assembly with no Unity references. MonoBehaviours only load JSON, draw world-space text, and forward a choice. That split is why the mood, graph, and score tests run under `dotnet test` and under the Unity Test Framework.
- Scenes are data. `ScenarioLoader` reads JSON from StreamingAssets. The validator checks every file for dead ends, missing speaker text, broken links, and unreachable nodes. `BuildScript.Build` runs that check before `BuildPipeline.BuildPlayer` and returns exit code 1 on errors. The workflow checks the JSON again.
- The no-headset path is intentional. XRI 3.7 deprecated `XRDeviceSimulator` and still ships it as a legacy sample that the package says will be removed. The supported tool is the XR Interaction Simulator sample. CI never needs that sample: PlayMode calls `ChooseFirst` on the presenter. The gaze ray and the 1/2/3 keys are there so the scene plays before the sample is imported.
- CI is GameCI v4, which the docs currently label as the current major: `unity-test-runner` and `unity-builder`, Unity 6000.3.29f1, EditMode and PlayMode, then StandaloneLinux64 on Mono. `Library` is cached per target. Coverage is off because the test-runner docs name it as a Unity 6 crash source. Personal license activation is a `.ulf` file in `UNITY_LICENSE`, plus email and password. I do not claim a pass rate until a run has written one.
- The dashboard is a static page. Each run appends commit SHA, test counts, the Unity test duration, and validator warnings. Missing files stay null. The README table says pending until that file has rows.

## Five questions

### Why is the domain code not in a MonoBehaviour?

A MonoBehaviour is hard to construct in a test and it drags in the scene. Tension, scores, and the graph are pure functions of data. Keeping them in `EmpathyLab.Core` means an invalid node id, an empty choice, and a score past 100 are ordinary NUnit cases. The presenter can be wrong about layout without being wrong about the score.

### Why both a gaze ray and the XR Interaction Simulator?

The simulator is a sample prefab. It is not available until someone imports it, and CI will not import it. The gaze ray parented to the camera, plus world-space buttons, is the path that plays on a laptop and the path PlayMode drives. The simulator is the path I turn on in the editor when I want to show tracked controllers. I use the Interaction Simulator rather than the legacy Device Simulator because XRI 3.5 deprecated the old type and 3.7 says the old sample will be removed.

### How does a bad scenario fail CI if the tests were green yesterday?

The directory test loads every JSON file and fails on broken links or unreachable nodes, so a bad file fails EditMode. Separately, `BuildScript.Build` writes `Logs/scenario-validation.json` and exits 1 when `errorCount` is not zero. The workflow runs `tools/ci/check_validator.py` on that file even if Unity's `-quit` were to hide the exit code. Warnings are reported and do not fail the build.

### Why is OpenXR in the manifest but not enabled?

The plugin has to be there or a device build is a surprise later. An active loader in a headless PlayMode run looks for a runtime that the GameCI container does not have, and the test fails for a reason that is not the scenario. Enable OpenXR on the Standalone tab when the build is for a device. The simulator does not need it.

### What would you measure before you called this production tooling?

I would not quote a duration until the Actions log has one. The first things I would read off a real run are EditMode time, PlayMode time, and whether the Library cache actually restored. I would also watch validator warnings, because a warning trend means authors are shipping single-choice nodes or lines that are too long for VR. The dashboard is shaped for those series. Coverage stays off until a run without it is green, because the GameCI docs call out Unity 6 coverage crashes.

## Explain these without notes

- What `initialTension`, a choice's `tensionDelta`, and the three score fields do, and that scores clamp to 0–100 from a baseline stored in the JSON.
- The difference between an ending (`ending: true`, no choices) and a dead end (no choices, not an ending).
- Why `TryGetNode` returns false for an unknown id, and why `Choose` throws if that id is not on the current node.
- The assembly arrow: Editor and Runtime depend on Core. Core depends on nothing in Unity. PlayMode tests depend on Runtime. EditMode tests depend only on Core.
- How to import the XR Interaction Simulator, the Tab and bracket keys, and why there is no teleport.
- Where the `.ulf` file is on your machine, which three secrets the workflow reads, and why you sign out of the Hub afterward.
- What the summary JSON contains, and which cells in the README you are not allowed to fill by hand.
