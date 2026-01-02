using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.ChillMod._1778
{
    [CreateAssetMenu(fileName = "Setpoint", menuName = "Robot/ChillOut Setpoint", order = 0)]
    public class ChillOutSetpoint : ScriptableObject
    {
        [Tooltip("Inches")] public float elevatorHeight;
        [Tooltip("Deg")] public float intakeAngle;
        [Tooltip("Deg")] public float clawAngle;
    }
}

