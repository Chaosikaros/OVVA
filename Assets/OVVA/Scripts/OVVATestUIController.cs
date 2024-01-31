using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ChaosIkaros.OVVA
{
    public class OVVATestUIController : MonoBehaviour
    {
        public Selectable CurrentSelection;
        public InstructionManager InstructionManager;
        public OVVAInputManager OVVAInputManager;

        public IEnumerator WaitForClickOnButton()
        {
            OVVAInputManager.ReceivedInput = false;
            yield return new WaitUntil(() =>
                OVVAInputManager.ReceivedInput && OVVAInputManager.UserInput == UserInput.SubTaskStart);
            if (CurrentSelection == null)
            {
                CurrentSelection = InstructionManager.GridLayoutGroup.transform.GetChild(0)
                    .GetComponent<Selectable>();
                CurrentSelection.Select();
            }

            if (CurrentSelection != null && CurrentSelection.gameObject.GetComponent<Button>() != null)
            {
                CurrentSelection.gameObject.GetComponent<Button>().onClick.Invoke();
            }
            OVVAInputManager.ReceivedInput = false;
        }

        void Update()
        {
            if (OVVAInputManager.ReceivedInput && InstructionManager.OVVAUI.activeSelf)
            {
                if (CurrentSelection == null && InstructionManager.GridLayoutGroup.transform.childCount != 0)
                {
                    CurrentSelection = InstructionManager.GridLayoutGroup.transform.GetChild(0)
                        .GetComponent<Selectable>();
                    CurrentSelection.Select();
                }

                if (OVVAInputManager.UserInput == UserInput.Up)
                {
                    Selectable nextSelection = CurrentSelection.FindSelectableOnUp();
                    if (nextSelection != null)
                    {
                        CurrentSelection = nextSelection;
                        CurrentSelection.Select();
                    }
                }
                else if (OVVAInputManager.UserInput == UserInput.Down)
                {
                    Selectable nextSelection = CurrentSelection.FindSelectableOnDown();
                    if (nextSelection != null)
                    {
                        CurrentSelection = nextSelection;
                        CurrentSelection.Select();
                    }
                }
                else if (OVVAInputManager.UserInput == UserInput.Left)
                {
                    Selectable nextSelection = CurrentSelection.FindSelectableOnLeft();
                    if (nextSelection != null)
                    {
                        CurrentSelection = nextSelection;
                        CurrentSelection.Select();
                    }
                }
                else if (OVVAInputManager.UserInput == UserInput.Right)
                {
                    Selectable nextSelection = CurrentSelection.FindSelectableOnRight();
                    if (nextSelection != null)
                    {
                        CurrentSelection = nextSelection;
                        CurrentSelection.Select();
                    }
                }
            }
        }
    }
}