# EmpathyLab VR

You stand still in a small room and talk to someone who is already upset. Avery is a bakery customer whose dad's cake is late. Rio is a neighbor whose child just fell asleep. You look at a response and select it. Their mood moves. The scene ends on a debrief that scores empathy, clarity, and de-escalation, and explains why each line helped or hurt.

There are no weapons, no violence, and no law-enforcement imagery.

## Play it without a headset

The scene is meant to run on a laptop. You do not walk, and you do not need a headset for the demo.

1. Install Unity **6000.3.29f1** and open this folder.
2. Open `Assets/Scenes/EmpathyLab.unity` and press Play.
3. Look at a response and click it, or press **1**, **2**, or **3**.
4. **Reset view** puts you back at eye height, facing the counter.
5. To play the neighbor scene, select the `Scenario` object and set `scenarioFileName` to `shared-wall.json`.

For tracked controllers, import the **XR Interaction Simulator** sample from the XR Interaction Toolkit, then run **EmpathyLab > Add XR Interaction Simulator To Scene**. **Tab** switches view mode. **[** and **]** choose a hand. You still do not walk. Remove that object before a headset build.

OpenXR is installed and left off until you build for a device.

## What to record

Capture these after you have played the scene, and add them under `docs/images/`:

1. The opening, with Avery upset and three responses visible.
2. The same view after a calmer line, with the mood label changed.
3. The debrief, with the three scores and the reasons.
4. A short GIF of one full run.

## Add a scene

Copy `Assets/StreamingAssets/Scenarios/front-counter.json`, change the people and the lines, and point `scenarioFileName` at the new file. No C# change is required. **EmpathyLab > Scenario Validator** checks that every line leads somewhere.

The test, validator, and CI setup is in [docs/engineering.md](docs/engineering.md).
