using System;
using System.Collections;
using System.Collections.Generic;
using ChaosIkaros.OVVA;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstructionManager : MonoBehaviour
{
    public GameObject OVVAVideo;
    public OVVATestController OVVATestController;
    public OVVATestUIController OVVATestUIController;
    public GameObject OVVAUI;
    public GameObject StartUI;
    public GameObject InstructionUI;
    public TMP_Text Introduction;
    public GridLayoutGroup GridLayoutGroup;
    public Button ButtonPrefab;
    public string CurrentBtnName;
    public string CurrentBtnMsg;

    private void Start()
    {

    }

    public void SwitchVideo()
    {
        OVVAVideo.SetActive(!OVVAVideo.activeSelf);
    }

    public IEnumerator WaitForHMDAdjustment()
    {
        OVVAUI.SetActive(true);
        StartUI.SetActive(false);
        InstructionUI.SetActive(true);
        GenerateButtons("1: Click on next to close UI.\r\n" +
                        "2: Adjust your HMD to the most comfortable position that allow you to see letter clearly.\r\n" +
                        "3: Click on any directional button to start formal test.", 
            new string[]{"Next"}, new string[]{"Next"});
        yield return StartCoroutine(OVVATestUIController.WaitForClickOnButton());
        OVVAUI.SetActive(false);
        yield return new WaitUntil(() => OVVATestController.OVVAInputManager.ReceivedInput);
        OVVAUI.SetActive(true);
        GenerateButtons("1: The recognition task generates all E letters first. You can start it by click on the Confirm button.\r\n" +
                        "2: When the test starts, all E letters will disappear and show up one by one randomly.\r\n" +
                        "3: Please indicate the orientation of the current E letter using directional buttons.\r\n" +
                        "4: Be careful and do not make mistake, the first stage is sensitive to mistakes.", 
            new string[]{"I understand"}, new string[]{"Next"});
        InstructionUI.SetActive(true);
        yield return StartCoroutine(OVVATestUIController.WaitForClickOnButton());
        OVVAUI.SetActive(false);
    }
    
    public IEnumerator EndCheckForStage1()
    {
        OVVAUI.SetActive(true);
        GenerateButtons("Did you make any mistake? For example, clicked on wrong directional button for letter with recognizable orientation.\r\n" +
                        "\r\n1: Click on Yes to restart last task.\r\n" +
                        "2: Click on No to start next task.", 
            new string[]{"No", "Yes"}, new string[]{"No", "Yes"});
        InstructionUI.SetActive(true);
        yield return StartCoroutine(OVVATestUIController.WaitForClickOnButton());
        if (CurrentBtnMsg == "Yes")
        {
            OVVATestController.Stage1HasMistake = true;
        }
        OVVAUI.SetActive(false);
    }
    
    public IEnumerator EndCheckForStage2()
    {
        OVVAUI.SetActive(true);
        GenerateButtons("Did any letters on the ring fall outside your field of vision? The ring should display 8 letters in total.\r\n" +
                        "\r\n1: Click on Yes to end the test.\r\n" +
                        "2: Click on No to start next task.", 
            new string[]{"No", "Yes"}, new string[]{"No", "Yes"});
        InstructionUI.SetActive(true);
        yield return StartCoroutine(OVVATestUIController.WaitForClickOnButton());
        if (CurrentBtnMsg == "Yes")
        {
            GenerateButtons("Click on next button to get the result.", 
                new string[]{"Next"}, new string[]{"Next"});
            yield return StartCoroutine(OVVATestUIController.WaitForClickOnButton());
            OVVATestController.OVVAInputManager.UserInput = UserInput.End;
            OVVATestController.OVVAInputManager.ReceivedInput = true;
            OVVATestController.SaveData();
        }
        else
        {
            OVVAUI.SetActive(false);
        }
    }

    public void GenerateButtons(string introduction, string[] buttonNames, string[] buttonMsgs)
    {
        Introduction.text = introduction;

        foreach (Transform child in GridLayoutGroup.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < buttonNames.Length; i++)
        {
            CreateButton(buttonNames[i], buttonMsgs[i]);
        }
    }

    private void CreateButton(string name, string msg)
    {
        Button newButton = Instantiate(ButtonPrefab, GridLayoutGroup.transform);
        SetButtonName(newButton, name);
        newButton.onClick.AddListener(() => OnClickButton(name, msg));
    }

    public void SetButtonName(Button button, string name)
    {
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = name;
        }
    }

    public void OnClickButton(string name, string msg)
    {
        CurrentBtnName = name;
        CurrentBtnMsg = msg;
        //Debug.Log("Click button: " + name + "; msg: " + msg);
    }
}