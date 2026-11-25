#nullable enable
using UnityEditor;
using UnityEngine;

namespace EmpathyLab.EditorTools
{
    /// <summary>
    /// Headset-free setup. XRI 3.7 still ships the legacy XR Device Simulator sample and marks it for removal.
    /// The supported sample is the XR Interaction Simulator, which drives the headset and controllers from the keyboard.
    /// </summary>
    public static class ProjectMenus
    {
        const string InteractionSimulator = "XR Interaction Simulator";
        const string LegacyDeviceSimulator = "XR Device Simulator";

        /// <summary>Drops the simulator prefab into the open scene after the sample has been imported.</summary>
        [MenuItem("EmpathyLab/Add XR Interaction Simulator To Scene")]
        public static void AddSimulator()
        {
            var path = FindPrefab(InteractionSimulator) ?? FindPrefab(LegacyDeviceSimulator);
            if (path == null)
            {
                Debug.LogWarning(
                    "No simulator prefab is in the project yet. In Package Manager, select XR Interaction Toolkit 3.7.0 " +
                    "and import the XR Interaction Simulator sample. " +
                    "The legacy XR Device Simulator sample is still listed, and the package says it will be removed, so use the Interaction Simulator. " +
                    "Then run EmpathyLab/Add XR Interaction Simulator To Scene again. " +
                    "You can also enable Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit > Use XR Interaction Simulator in scenes.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("Could not load " + path);
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add XR simulator");
            Selection.activeGameObject = instance;
            Debug.Log(
                "Added " + prefab.name + ". Press Play. Tab switches FPS mode and device mode. " +
                "The bracket keys [ ] activate the left and right controllers. " +
                "This scene does not move the player. Look at a response and select it. " +
                "Remove the simulator prefab before you build for a headset.");
        }

        /// <summary>Prints the editor and the XR package versions so a mismatch is obvious.</summary>
        [MenuItem("EmpathyLab/Log Package Versions")]
        public static void LogPackages()
        {
            Debug.Log("Unity " + Application.unityVersion);
            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (package.name.Contains("xr") || package.name.Contains("inputsystem") || package.name.Contains("test-framework") || package.name == "com.unity.ugui")
                    Debug.Log(package.name + " " + package.version);
            }

            Debug.Log(
                "OpenXR is installed and left inactive so headless CI does not wait on a headset runtime. " +
                "Before a device build: Edit > Project Settings > XR Plug-in Management, and enable OpenXR on the Standalone tab.");
        }

        static string? FindPrefab(string prefabName)
        {
            foreach (var guid in AssetDatabase.FindAssets(prefabName + " t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(prefabName + ".prefab"))
                    return path;
            }

            return null;
        }
    }
}
