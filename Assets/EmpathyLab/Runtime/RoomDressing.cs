#nullable enable
using UnityEngine;

namespace EmpathyLab.Runtime
{
    /// <summary>
    /// Colors the primitive room at play time so the scene file does not need material assets.
    /// </summary>
    public sealed class RoomDressing : MonoBehaviour
    {
        void Start()
        {
            PaintNamed("Floor", new Color(0.45f, 0.32f, 0.22f));
            PaintNamed("Ceiling", new Color(0.93f, 0.90f, 0.84f));
            PaintNamed("WallNorth", new Color(0.86f, 0.82f, 0.74f));
            PaintNamed("WallSouth", new Color(0.84f, 0.80f, 0.73f));
            PaintNamed("WallEast", new Color(0.80f, 0.76f, 0.68f));
            PaintNamed("WallWest", new Color(0.80f, 0.76f, 0.68f));
            PaintNamed("Counter", new Color(0.62f, 0.45f, 0.30f));
            PaintNamed("Avery", new Color(0.72f, 0.45f, 0.34f));
        }

        /// <summary>Assigns an unlit-looking color on a primitive. Shared by the rig reticle.</summary>
        /// <param name="target">Object to color.</param>
        /// <param name="color">Color to apply.</param>
        public static void Paint(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var shader = Shader.Find("Standard")
                ?? Shader.Find("Diffuse")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return;

            var material = new Material(shader);
            material.color = color;
            renderer.material = material;
        }

        static void PaintNamed(string objectName, Color color)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
                Paint(target, color);
        }
    }
}
