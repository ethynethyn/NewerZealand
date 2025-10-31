using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Ashsvp.InputManager_SVP;

namespace Ashsvp
{
    public class InputManager_SVP : MonoBehaviour
    {
        public SimcadeVehicleController SimcadeVehicleController;

        [Serializable]
        public class KeyboardInput
        {
            public KeyCode steerLeft = KeyCode.A;
            public KeyCode steerRight = KeyCode.D;
            public KeyCode accelerate = KeyCode.W;
            public KeyCode decelerate = KeyCode.S;
            public KeyCode handBrake = KeyCode.Space;
        }

        public KeyboardInput keyboardInput = new KeyboardInput();

        [Serializable]
        public class MobileInput
        {
            public UiButton_SVP steerLeft;
            public UiButton_SVP steerRight;
            public UiButton_SVP accelerate;
            public UiButton_SVP decelerate;
            public UiButton_SVP handBrake;
        }

        public bool useMobileInput = false;
        public MobileInput mobileInput = new MobileInput();

        public float SteerInput { get; private set; }
        public float AccelerationInput { get; private set; }
        public float HandbrakeInput { get; private set; }

        private void Start()
        {
            SimcadeVehicleController = GetComponent<SimcadeVehicleController>();
        }

        private void Update()
        {
            float tempSteerInput = GetKeyboardSteerInput();
            float tempAccelerationInput = GetKeyboardAccelerationInput();
            float tempHandbrakeInput = GetKeyboardHandbrakeInput();

            if (useMobileInput)
            {
                tempSteerInput = GetMobileSteerInput();
                tempAccelerationInput = GetMobileAccelerationInput();
                tempHandbrakeInput = GetMobileHandbrakeInput();
            }

            AccelerationInput = Mathf.Abs(tempAccelerationInput) > 0 ? Mathf.Lerp(AccelerationInput, tempAccelerationInput, 15 * Time.deltaTime) : 0;
            SteerInput = Mathf.Abs(tempSteerInput) > 0 ? Mathf.Lerp(SteerInput, tempSteerInput, 15 * Time.deltaTime)
                : Mathf.Lerp(SteerInput, tempSteerInput, 25 * Time.deltaTime);
            HandbrakeInput = tempHandbrakeInput;

            // provide input to vehicle controller
            SimcadeVehicleController.ProvideInputs(AccelerationInput, SteerInput, HandbrakeInput);
        }

        private float GetKeyboardSteerInput()
        {
            float steerInput = 0f;
            if (Input.GetKey(keyboardInput.steerLeft))
                steerInput -= 1f;
            if (Input.GetKey(keyboardInput.steerRight))
                steerInput += 1f;
            return steerInput;
        }

        private float GetKeyboardAccelerationInput()
        {
            float accelInput = 0f;
            if (Input.GetKey(keyboardInput.accelerate))
                accelInput += 1f;
            if (Input.GetKey(keyboardInput.decelerate))
                accelInput -= 1f;
            return accelInput;
        }

        private float GetKeyboardHandbrakeInput()
        {
            return Input.GetKey(keyboardInput.handBrake) ? 1f : 0f;
        }

        private float GetMobileSteerInput()
        {
            float steerInput = 0f;
            if (mobileInput.steerLeft.isPressed)
                steerInput -= 1f;
            if (mobileInput.steerRight.isPressed)
                steerInput += 1f;
            return steerInput;
        }

        private float GetMobileAccelerationInput()
        {
            float accelInput = 0f;
            if (mobileInput.accelerate.isPressed)
                accelInput += 1f;
            if (mobileInput.decelerate.isPressed)
                accelInput -= 1f;
            return accelInput;
        }

        private float GetMobileHandbrakeInput()
        {
            return mobileInput.handBrake.isPressed ? 1f : 0f;
        }

        // Added reset method
        public void ResetInputs()
        {
            SteerInput = 0f;
            AccelerationInput = 0f;
            HandbrakeInput = 0f;

            if (SimcadeVehicleController != null)
            {
                // Immediately apply zeroed input so the car stops
                SimcadeVehicleController.ProvideInputs(0f, 0f, 0f);
            }
        }
    }
}
