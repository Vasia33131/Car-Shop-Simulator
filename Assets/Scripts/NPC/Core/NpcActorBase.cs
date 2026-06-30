using System.Collections.Generic;
using UnityEngine;

namespace StoreSim.NPC
{
    public abstract class NpcActorBase : MonoBehaviour
    {
        private static readonly List<NpcActorBase> _active = new();
        public static IReadOnlyList<NpcActorBase> Active => _active;

        protected virtual void OnEnable()
        {
            if (!_active.Contains(this))
                _active.Add(this);
        }

        protected virtual void OnDisable() => _active.Remove(this);
    }
}
