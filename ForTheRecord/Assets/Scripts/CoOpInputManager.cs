using UnityEngine;
using UnityEngine.InputSystem;

namespace CameraCoop
{
    public class CoOpInputManager : MonoBehaviour
    {
        public static CoOpInputManager Instance { get; private set; }

        [Header("Debug Status")]
        [SerializeField] private bool _gamepad1Active;
        [SerializeField] private bool _gamepad2Active;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            var gamepads = Gamepad.all;
            _gamepad1Active = gamepads.Count > 0 && gamepads[0] != null;
            _gamepad2Active = gamepads.Count > 1 && gamepads[1] != null;
        }

        public Vector2 GetP1Movement()
        {
            Vector2 move = Vector2.zero;
            var gamepads = Gamepad.all;

            // Gamepad 1
            if (gamepads.Count > 0 && gamepads[0] != null)
            {
                move = gamepads[0].leftStick.ReadValue();
                if (move.sqrMagnitude < 0.04f)
                {
                    // Check D-Pad
                    move = gamepads[0].dpad.ReadValue();
                }
            }

            // Keyboard fallback for P1: WASD
            if (Keyboard.current != null)
            {
                Vector2 kb = Vector2.zero;
                if (Keyboard.current.wKey.isPressed) kb.y += 1f;
                if (Keyboard.current.sKey.isPressed) kb.y -= 1f;
                if (Keyboard.current.aKey.isPressed) kb.x -= 1f;
                if (Keyboard.current.dKey.isPressed) kb.x += 1f;

                if (kb.sqrMagnitude > 0.01f)
                {
                    move = kb.normalized;
                }
            }

            return Vector2.ClampMagnitude(move, 1f);
        }

        public Vector2 GetP2Movement()
        {
            Vector2 move = Vector2.zero;
            var gamepads = Gamepad.all;

            // Gamepad 2
            if (gamepads.Count > 1 && gamepads[1] != null)
            {
                move = gamepads[1].leftStick.ReadValue();
                if (move.sqrMagnitude < 0.04f)
                {
                    move = gamepads[1].dpad.ReadValue();
                }
            }

            // Keyboard fallback for P2: Arrow keys or IJKL
            if (Keyboard.current != null)
            {
                Vector2 kb = Vector2.zero;
                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.iKey.isPressed) kb.y += 1f;
                if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.kKey.isPressed) kb.y -= 1f;
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.jKey.isPressed) kb.x -= 1f;
                if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.lKey.isPressed) kb.x += 1f;

                if (kb.sqrMagnitude > 0.01f)
                {
                    move = kb.normalized;
                }
            }

            return Vector2.ClampMagnitude(move, 1f);
        }

        public bool WasP1ActionPressed()
        {
            var gamepads = Gamepad.all;
            if (gamepads.Count > 0 && gamepads[0] != null)
            {
                if (gamepads[0].buttonSouth.wasPressedThisFrame) return true;
                if (gamepads[0].rightTrigger.wasPressedThisFrame) return true;
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;
                if (Keyboard.current.fKey.wasPressedThisFrame) return true;
            }

            return false;
        }

        public bool WasP2ActionPressed()
        {
            var gamepads = Gamepad.all;
            if (gamepads.Count > 1 && gamepads[1] != null)
            {
                if (gamepads[1].buttonSouth.wasPressedThisFrame) return true;
                if (gamepads[1].rightTrigger.wasPressedThisFrame) return true;
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.enterKey.wasPressedThisFrame) return true;
                if (Keyboard.current.numpadEnterKey.wasPressedThisFrame) return true;
                if (Keyboard.current.rightShiftKey.wasPressedThisFrame) return true;
                if (Keyboard.current.rightCtrlKey.wasPressedThisFrame) return true;
                if (Keyboard.current.slashKey.wasPressedThisFrame) return true;
            }

            return false;
        }

        public bool WasAnyActionPressed()
        {
            return WasP1ActionPressed() || WasP2ActionPressed();
        }

        public bool IsGamepad1Connected => _gamepad1Active;
        public bool IsGamepad2Connected => _gamepad2Active;
    }
}
