using UnityEngine;

namespace StarterAssets
{
    // ============================================================
    //  PLAYER TELEPORTER
    //  Attach to any GameObject. Set the destination Transform.
    //  Enable the GameObject to teleport; it auto-disables itself after.
    // ============================================================
    public class PlayerTeleporter : MonoBehaviour
    {
        [Tooltip("Drag the Player root GameObject here (the one with FirstPersonController)")]
        public GameObject playerRoot;

        [Tooltip("Where to teleport the player")]
        public Transform destination;

        [Tooltip("Also inherit the destination's Y rotation (yaw) so the player faces the right way")]
        public bool inheritYaw = true;

        private CharacterController _cc;
        private FirstPersonController _fpc;
        private SlopeSlideController _slide;

        private void Awake()
        {
            ResolveRefs();
        }

        private void OnEnable()
        {
            ResolveRefs();
            Teleport();
            // Self-disable so re-enabling next time will fire again
            gameObject.SetActive(false);
        }

        private void ResolveRefs()
        {
            if (playerRoot == null)
                playerRoot = GameObject.FindGameObjectWithTag("Player");
            if (playerRoot == null) return;

            _cc = playerRoot.GetComponent<CharacterController>();
            _fpc = playerRoot.GetComponent<FirstPersonController>();
            _slide = playerRoot.GetComponent<SlopeSlideController>();
        }

        public void Teleport()
        {
            if (playerRoot == null || destination == null)
            {
                Debug.LogWarning("[PlayerTeleporter] playerRoot or destination is not assigned.");
                return;
            }

            // 1. Kill momentum before the move
            if (_slide != null)
                _slide.ForceResetState();

            // 2. Disable CC — required to move a CC without it fighting the transform
            bool ccWasEnabled = _cc != null && _cc.enabled;
            if (_cc != null) _cc.enabled = false;

            // 3. Move the transform
            playerRoot.transform.position = destination.position;

            // 4. Optionally rotate to face destination direction
            if (inheritYaw)
            {
                Vector3 euler = playerRoot.transform.eulerAngles;
                euler.y = destination.eulerAngles.y;
                playerRoot.transform.eulerAngles = euler;
            }

            // 5. Re-enable CC
            if (_cc != null && ccWasEnabled)
                _cc.enabled = true;
        }
    }
}