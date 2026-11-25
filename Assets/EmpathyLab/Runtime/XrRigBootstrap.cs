#nullable enable
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace EmpathyLab.Runtime
{
    /// <summary>
    /// Builds a stationary XR Origin at runtime and aims a gaze ray at the world-space choices.
    /// There is no teleport or continuous move. The player stays at the counter, which is the comfort rule for this scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XrRigBootstrap : MonoBehaviour
    {
        /// <summary>Name PlayMode tests and the presenter look for.</summary>
        public const string OriginObjectName = "XR Origin";

        const float EyeHeightMeters = 1.6f;

        /// <summary>Puts the camera back to eye height, looking at the counter.</summary>
        public void ResetView()
        {
            var camera = GetComponentInChildren<Camera>();
            if (camera == null)
                return;

            camera.transform.localPosition = new Vector3(0f, EyeHeightMeters, 0f);
            camera.transform.localRotation = Quaternion.identity;
        }

        void Awake()
        {
            var camera = EnsureCamera();
            EnsureOrigin(camera);
            EnsureInteractionManager();
            EnsureEventSystem();
            EnsureGazeInteractor(camera);
            EnsureReticle(camera);
        }

        Camera EnsureCamera()
        {
            var camera = GetComponentInChildren<Camera>();
            if (camera != null)
                return camera;

            var offset = transform.Find("Camera Offset");
            if (offset == null)
            {
                var offsetObject = new GameObject("Camera Offset");
                offsetObject.transform.SetParent(transform, false);
                offset = offsetObject.transform;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(offset, false);
            cameraObject.transform.localPosition = new Vector3(0f, EyeHeightMeters, 0f);
            camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.82f, 0.86f, 0.88f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 40f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        void EnsureOrigin(Camera camera)
        {
            var origin = GetComponent<XROrigin>();
            if (origin == null)
                origin = gameObject.AddComponent<XROrigin>();

            var offset = camera.transform.parent != null ? camera.transform.parent.gameObject : gameObject;
            origin.CameraFloorOffsetObject = offset;
            origin.Camera = camera;
            origin.CameraYOffset = EyeHeightMeters;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        }

        void EnsureInteractionManager()
        {
            if (FindAnyObjectByType<XRInteractionManager>() != null)
                return;

            var manager = new GameObject("Interaction Manager");
            manager.transform.SetParent(transform, false);
            manager.AddComponent<XRInteractionManager>();
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            var module = eventSystem.AddComponent<XRUIInputModule>();
            module.enableXRInput = true;
            module.enableMouseInput = true;
            module.enableBuiltinActionsAsFallback = true;
        }

        static void EnsureGazeInteractor(Camera camera)
        {
            if (camera.GetComponentInChildren<XRRayInteractor>(true) != null)
                return;

            var gaze = new GameObject("Gaze Interactor");
            gaze.transform.SetParent(camera.transform, false);
            var ray = gaze.AddComponent<XRRayInteractor>();
            ray.enableUIInteraction = true;
        }

        static void EnsureReticle(Camera camera)
        {
            if (camera.transform.Find("Gaze Reticle") != null)
                return;

            var reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = "Gaze Reticle";
            Destroy(reticle.GetComponent<Collider>());
            reticle.layer = 2;
            reticle.transform.SetParent(camera.transform, false);
            reticle.transform.localPosition = new Vector3(0f, 0f, 0.85f);
            reticle.transform.localScale = Vector3.one * 0.012f;
            RoomDressing.Paint(reticle, new Color(0.12f, 0.12f, 0.1f, 1f));
        }
    }
}
