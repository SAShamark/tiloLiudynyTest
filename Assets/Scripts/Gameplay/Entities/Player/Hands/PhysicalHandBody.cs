using UnityEngine;

namespace Gameplay.Entities.Player.Hands
{
    [DisallowMultipleComponent]
    public sealed class PhysicalHandBody : MonoBehaviour
    {
        public static bool IsHand(Collider collider)
        {
            var body = collider != null ? collider.attachedRigidbody : null;
            return body != null && body.TryGetComponent<PhysicalHandBody>(out _);
        }
    }
}
