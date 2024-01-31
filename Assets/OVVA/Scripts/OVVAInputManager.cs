using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaosIkaros.OVVA
{
    public abstract class InputDeviceManager : MonoBehaviour
    {
        public abstract UserInput GetUserInput();
    }
    
    public class OVVAInputManager : MonoBehaviour
    {
        public InputDeviceManager InputDeviceManager;
        public UserInput UserInput = UserInput.Up;
        public string LastAnswer = "";
        public string LastInput = "";
        public bool ReceivedInput;

        public bool CanRecognizeCurrentOptotype(float rotationAngle)
        {
            if (!OVVAUtility.AngleNameMapping.ContainsKey(rotationAngle) || !OVVAUtility.UserInputNameMapping.ContainsKey(UserInput))
            {
                LastInput = "None";
                return false;
            }
            LastAnswer = OVVAUtility.AngleNameMapping[rotationAngle];
            LastInput = OVVAUtility.UserInputNameMapping[UserInput];
            return LastAnswer == LastInput;
        }


        // Update is called once per frame
        void Update()
        { 
            UserInput = InputDeviceManager.GetUserInput();
            if(UserInput != UserInput.None)
                ReceivedInput = true;
        }
    }
}