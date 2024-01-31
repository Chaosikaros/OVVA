using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChaosIkaros.OVVA
{
    public class KeyboardInputDevice : InputDeviceManager
    {
        public override UserInput GetUserInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                return UserInput.SubTaskStart;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                return UserInput.Up;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                return UserInput.Down;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                return UserInput.Left;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                return UserInput.Right;
            }
            return UserInput.None;
        }
    }
}
