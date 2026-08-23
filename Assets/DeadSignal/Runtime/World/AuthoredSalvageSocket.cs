using UnityEngine;

namespace DeadSignal.World
{
    /// <summary>
    /// Marks a scene-authored cache location while runtime systems retain collection ownership.
    /// </summary>
    public sealed class AuthoredSalvageSocket : MonoBehaviour
    {
        public Vector3 Position => transform.position;
    }
}
