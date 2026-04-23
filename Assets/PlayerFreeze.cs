using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    // ============================================================
    //  PLAYER FREEZE
    //  Attach to any GameObject. Enable it to freeze, disable to unfreeze.
    //  Works whether the player is walking, jumping, or skating.
    // ============================================================
    public class PlayerFreeze : MonoBehaviour
    {
        [Tooltip("Drag the Player root GameObject here (the one with FirstPersonController)")]
        public GameObject playerRoot;

        private FirstPersonController _fpc;
        private SlopeSlideController _slide;
        private CharacterController _cc;
        private StarterAssetsInputs _inputs;
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        private bool _wasFrozen = false;

        private void Awake()
        {
            ResolveRefs();
        }

        private void OnEnable()
        {
            ResolveRefs();
            Freeze();
        }

        private void OnDisable()
        {
            Unfreeze();
        }

        private void ResolveRefs()
        {
            if (playerRoot == null)
                playerRoot = GameObject.FindGameObjectWithTag("Player");
            if (playerRoot == null) return;

            _fpc = playerRoot.GetComponent<FirstPersonController>();
            _slide = playerRoot.GetComponent<SlopeSlideController>();
            _cc = playerRoot.GetComponent<CharacterController>();
            _inputs = playerRoot.GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = playerRoot.GetComponent<PlayerInput>();
#endif
        }

        public void Freeze()
        {
            if (_wasFrozen) return;
            _wasFrozen = true;

            // 1. Kill all slope momentum & state
            if (_slide != null)
                _slide.ForceResetState();

            // 2. Disable FPC so Update/LateUpdate stop firing
            if (_fpc != null)
                _fpc.enabled = false;

            // 3. Disable CharacterController so it cannot be moved at all
            if (_cc != null)
                _cc.enabled = false;

            // 4. Zero every input axis so buffered input can't fire on unfreeze
            if (_inputs != null)
                ClearInputs();

#if ENABLE_INPUT_SYSTEM
            // 5. Disable PlayerInput so hardware events are swallowed
            if (_playerInput != null)
                _playerInput.enabled = false;
#endif
        }

        public void Unfreeze()
        {
            if (!_wasFrozen) return;
            _wasFrozen = false;

#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null)
                _playerInput.enabled = true;
#endif

            if (_inputs != null)
                ClearInputs();

            // Re-enable CC first, then the controller
            if (_cc != null)
                _cc.enabled = true;

            if (_fpc != null)
                _fpc.enabled = true;
        }

        private void ClearInputs()
        {
            _inputs.move = Vector2.zero;
            _inputs.look = Vector2.zero;
            _inputs.jump = false;
            _inputs.sprint = false;
            _inputs.crouch = false;
        }
    }
}