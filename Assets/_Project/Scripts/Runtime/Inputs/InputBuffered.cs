using UnityEngine;

namespace Beakstorm.Inputs
{
    public class InputBuffered
    {
        private readonly float _inputGrace;
        public double timeAtLastInput;
        public bool bufferActive;
        public bool checkedThisFrame;

        public InputBuffered(float inputGrace)
        {
            _inputGrace = inputGrace;
            bufferActive = false;
            checkedThisFrame = false;
        }

        /// <summary>
        /// Activates the input and saves the time of this input
        /// </summary>
        public void TriggerInput()
        {
            timeAtLastInput = Time.unscaledTimeAsDouble;
            bufferActive = true;
            checkedThisFrame = false;
        }

        /// <summary>
        /// Cancels the input.
        /// </summary>
        public void CancelInput()
        {
            bufferActive = false;
        }

        /// <summary>
        /// Checks the input buffer. If the time since input falls within the input grace, returns true.
        /// If it has already been checked, returns false.
        /// </summary>
        /// <returns></returns>
        public bool CheckOnce()
        {
            if (!bufferActive) return false;
            if (checkedThisFrame) return false;

            checkedThisFrame = true;
            return Time.unscaledTimeAsDouble - timeAtLastInput < _inputGrace;
        }

        /// <summary>
        /// Checks the input buffer. If the time since input falls within the input grace, returns true.
        /// Can be called without resetting the buffer.
        /// </summary>
        /// <returns></returns>
        public bool Check()
        {
            //if (!bufferActive) return false;

            return Time.unscaledTimeAsDouble - timeAtLastInput < _inputGrace;
        }


        public static implicit operator bool(InputBuffered inputBuffered)
        {
            return inputBuffered.CheckOnce();
        }
    }
}