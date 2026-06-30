using System;
using UnityEngine;

namespace StoreSim.NPC
{
    [Serializable]
    public struct NpcMovementSettings
    {
        public float MoveSpeed;
        public float RotationSpeed;
        public float StoppingDistance;
        public LayerMask GroundMask;
        public float GroundCheckHeight;
        public float GroundCheckDistance;
        public float GroundOffset;

        public static NpcMovementSettings Default => new()
        {
            MoveSpeed = 2f,
            RotationSpeed = 10f,
            StoppingDistance = 0.1f,
            GroundCheckHeight = 2f,
            GroundCheckDistance = 10f
        };
    }
}
