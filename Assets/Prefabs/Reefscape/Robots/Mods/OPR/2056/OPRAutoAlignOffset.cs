using UnityEngine;

namespace Games.Reefscape.Components
{
    [CreateAssetMenu(fileName = "OPRAutoAlignOffset", menuName = "Robot/OPR Auto Align Offset", order = 0)]
    public class OPRAutoAlignOffset : ScriptableObject
    {
        [Header("Offsets")]
        [Tooltip("The offset in the X direction from the reef's center for the robot to target (Always positive)")]
        public float x = 1.3f;
        
        [Tooltip("The offset in the Y direction from the reef's center for the robot to target (Always positive)")]
        public float y = 0.17f;
    }
}

