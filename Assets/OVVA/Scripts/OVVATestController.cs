using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using TMPro;
using UnityEngine.UI;

namespace ChaosIkaros.OVVA
{
    public class OVVATestController : MonoBehaviour
    {
        public bool ShowDebugPanel = false;
        public GameObject DebugPanel;
        public InstructionManager InstructionManager;
        public RawImage ResultPanel;

        public GameObject LeftEyeMask;
        public GameObject RightEyeMask;

        public OVVARecorder OVVARecorder;
        public OVVAGenerator OVVAGenerator;
        public OVVAInputManager OVVAInputManager;
        public OVVADatasetBatchProcessing OVVADatasetBatchProcessing;

        public GameObject StartButton;
        public OVVAStage OVVAStage = OVVAStage.CentralVVA;

        public TMP_Text SubTaskInfo;
        public TMP_Text HMDInfo;
        public TMP_Dropdown ParticipantID;
        public TMP_Dropdown EyeCondition;
        public string HMDName;
        public Vector2Int RenderResolution;
        public float LogMAR;
        public bool Stage1HasMistake = false;
        private List<float> _accuracyList = new List<float> { };
        private int _repeatCounter = 0;
        private int _subTaskScore = 0;
        private float _currentDistance = 0f;
        private float _halfFOV = OVVAGenerator.HalfFOVStart;
        private float _timer = 0;
        private float _lastDistanceA = 0f;
        private float _lastDistanceB = 0f;
        private float _lastDistanceC = 0;
        private float _startTime = 0;
        private float _accuracy = 0;
        private float _progress = 0;
        private string _currentPositions = "";
        private string _currentAnswer = "";
        private int _wrongCounter = 0;
        private bool _startedSubTask = false;
        private bool _passTask = false;

        // Start is called before the first frame update
        void Start()
        {
            DebugPanel.SetActive(ShowDebugPanel);
            List<string> options = new List<string> { };
            for (int i = 1; i < 65; i++)
            {
                options.Add(i.ToString());
            }

            LeftEyeMask.transform.parent.SetParent(OVVAGenerator.CameraTransform);
            LeftEyeMask.transform.parent.localPosition = new Vector3(0, 0, 0.03f);

            ParticipantID.ClearOptions();
            ParticipantID.AddOptions(options);
            EyeCondition.ClearOptions();
            EyeCondition.AddOptions(new List<string> { "Both", "L", "R" });

            OVVAGenerator.GenerateOVVA();
            OVVAStage = OVVAStage.CentralVVA;

            StartCoroutine(InitDeviceInfo());
        }

        public IEnumerator InitDeviceInfo()
        {
            yield return new WaitUntil(() => InputDevices.GetDeviceAtXRNode(XRNode.Head) != null);
            RenderResolution = new Vector2Int(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight);
            HMDName = OVVAUtility.GetXRDeviceName();
            HMDInfo.text = "HMD: " + HMDName + "\r\nRender Resolution: " + RenderResolution.x + "x" +
                           RenderResolution.y;
        }

        public IEnumerator EyeMaskTest()
        {
            while (true)
            {
                LeftEyeMask.SetActive(true);
                RightEyeMask.SetActive(true);
                yield return new WaitForSeconds(2.0f);
                LeftEyeMask.SetActive(false);
                RightEyeMask.SetActive(true);
                yield return new WaitForSeconds(2.0f);
                LeftEyeMask.SetActive(true);
                RightEyeMask.SetActive(false);
                yield return new WaitForSeconds(2.0f);
                LeftEyeMask.SetActive(false);
                RightEyeMask.SetActive(false);
                yield return new WaitForSeconds(2.0f);
            }
        }

        public void StartTest()
        {
            StartButton.SetActive(false);
            StartCoroutine(OVVATestLoop());
        }

        private void SetEyeMask()
        {
            string currentMode = EyeCondition.options[EyeCondition.value].text;
            if (LeftEyeMask != null && RightEyeMask != null)
            {
                if (currentMode == "Both")
                {
                    LeftEyeMask.SetActive(false);
                    RightEyeMask.SetActive(false);
                }
                else if (currentMode == "R")
                {
                    LeftEyeMask.SetActive(true);
                    RightEyeMask.SetActive(false);
                }
                else if (currentMode == "L")
                {
                    LeftEyeMask.SetActive(false);
                    RightEyeMask.SetActive(true);
                }
            }
        }

        public void InitializeOVVATest()
        {
            SetEyeMask();
            OVVAGenerator.StartRadius = 6f;
            OVVAGenerator.UpdateNonCentralArea(OVVAGenerator.StartRadius);
            _currentDistance = 0f;
            _halfFOV = OVVAGenerator.HalfFOVStart;
            _timer = 0;
            _lastDistanceA = _currentDistance;
            _lastDistanceB = OVVAGenerator.StartRadius;
            _lastDistanceC = 0;
            _startTime = 0;
            _accuracy = 0;
            _progress = 0;
            _currentPositions = "";
            _currentAnswer = "";
            _accuracyList = new List<float> { };
            OVVARecorder.OutputData.Clear();
            OVVAInputManager.UserInput = UserInput.SubTaskEnd;
        }

        public IEnumerator OVVATestLoop()
        {
            yield return StartCoroutine(InstructionManager.WaitForHMDAdjustment());
            InitializeOVVATest();
            while (OVVAInputManager.UserInput != UserInput.End)
            {
                yield return StartCoroutine(HandleSubTaskEnd());
                _startTime = Time.time;
                yield return new WaitUntil(() => OVVAInputManager.ReceivedInput);
                _startedSubTask = false;
                _passTask = false;
                yield return new WaitUntil(() => OVVAInputManager.UserInput == UserInput.SubTaskStart);
                yield return StartCoroutine(HandleSubTaskStart());
                PostSubTaskProcessing();
                if (OVVAStage == OVVAStage.Degradation && !_startedSubTask)
                    OVVAInputManager.UserInput = UserInput.SubTaskSkip;
                else
                    OVVAInputManager.UserInput = UserInput.SubTaskEnd;
            }

            OVVAGenerator.ResetOVVA();
        }

        private IEnumerator HandleSubTaskEnd()
        {
            if (OVVAInputManager.UserInput == UserInput.SubTaskEnd)
            {
                if (OVVAStage == OVVAStage.CentralVVA)
                {
                    Stage1HasMistake = false;
                    if (_startTime != 0)
                        yield return StartCoroutine(InstructionManager.EndCheckForStage1());
                    UpdateCentralVVATest();
                }
                else
                {
                    _halfFOV += OVVAGenerator.HalfFOVInc;
                    OVVAGenerator.UpdateCentralArea(_lastDistanceC, _halfFOV);
                    if(_halfFOV > OVVAGenerator.HalfFOVStart + OVVAGenerator.HalfFOVInc)
                        yield return StartCoroutine(InstructionManager.EndCheckForStage2());
                }
            }
        }

        private IEnumerator HandleSubTaskStart()
        {
            _startedSubTask = true;
            _repeatCounter = OVVAGenerator.ActiveOptotypeList.Count;
            _subTaskScore = 0;
            _currentAnswer = "";
            _currentPositions = "";
            _wrongCounter = 0;
            while (_repeatCounter > 0)
            {
                yield return StartCoroutine(HandleSubTaskRound());
                if (_wrongCounter != 0 && OVVAStage == OVVAStage.CentralVVA)
                {
                    SubTaskInfo.text += "; Early Stop";
                    break;
                }
            }
        }

        private IEnumerator HandleSubTaskRound()
        {
            OVVAInputManager.UserInput = UserInput.RoundStart;
            _repeatCounter--;
            OVVAGenerator.ShowSingleOptotype(_repeatCounter);
            while (!(OVVAInputManager.UserInput == UserInput.Up || OVVAInputManager.UserInput == UserInput.Down
                                                                || OVVAInputManager.UserInput == UserInput.Left ||
                                                                OVVAInputManager.UserInput == UserInput.Right))
            {
                yield return new WaitUntil(() => OVVAInputManager.ReceivedInput);
            }

            RecordRoundData();
        }

        private void RecordRoundData()
        {
            _currentPositions +=
                OVVAUtility.Vector3ToString(OVVAGenerator.ActiveOptotypeList[_repeatCounter].transform.localPosition) +
                ";";
            if (OVVAInputManager.CanRecognizeCurrentOptotype(OVVAGenerator.ActiveOptotypeList[_repeatCounter].transform.GetChild(0).localRotation.eulerAngles.z))
            {
                _subTaskScore++;
                _currentAnswer += "1;";
            }
            else
            {
                _wrongCounter++;
                _currentAnswer += "0;";
            }

            _accuracy = (float)_subTaskScore / (float)OVVAGenerator.ActiveOptotypeList.Count;
            _progress = (float)(OVVAGenerator.ActiveOptotypeList.Count - _repeatCounter) /
                        (float)OVVAGenerator.ActiveOptotypeList.Count;
            SubTaskInfo.text = "Loop: " + _repeatCounter + "; accuracy: " + _accuracy
                               + "; progress: " + _progress + "\r\nLastAnswer: " + OVVAInputManager.LastAnswer +
                               "; lastInput: " + OVVAInputManager.LastInput;
        }

        private void PostSubTaskProcessing()
        {
            _accuracy = (float)_subTaskScore / (float)OVVAGenerator.ActiveOptotypeList.Count;
            _progress = (float)(OVVAGenerator.ActiveOptotypeList.Count - _repeatCounter) /
                        (float)OVVAGenerator.ActiveOptotypeList.Count;
            if (_accuracy >= OVVAGenerator.CentralAreaAccuracyThreshold)
                _passTask = true;
            _timer = (Time.time - _startTime);
            float angle = 0;
            OVVAUtility.VisualAngleConversion(ref angle, 0.0008726646f, _lastDistanceC);
            LogMAR = OVVAUtility.DegreeToLogMAR(angle);
            OVVARecorder.RecordData(LogMAR, _lastDistanceC, _lastDistanceA, _lastDistanceB, _halfFOV * 2, _timer,
                _accuracy,
                _progress, _currentPositions,
                _currentAnswer, OVVAStage, OVVAInputManager.UserInput, EyeCondition.options[EyeCondition.value].text);
            UpdateStage();
        }

        private void UpdateStage()
        {
            if (OVVAStage == OVVAStage.CentralVVA)
            {
                _accuracyList.Add(_accuracy);
                if (!_passTask)
                {
                    _lastDistanceB = _lastDistanceC;
                }
                else
                {
                    _lastDistanceA = _lastDistanceC;
                }

                if (Mathf.Abs(_lastDistanceA - _lastDistanceB) < OVVAGenerator.OptotypeDif)
                {
                    OVVAStage = OVVAStage.Degradation;
                    _halfFOV = OVVAGenerator.HalfFOVStart;
                    OVVARecorder.CentralAreaAccuracyList = new List<float> { };
                }
            }
            else
            {
                OVVARecorder.CentralAreaAccuracyList.Add(_accuracy);
            }
        }

        private void UpdateCentralVVATest()
        {
            if (_lastDistanceC != (_lastDistanceA + _lastDistanceB) * 0.5f)
            {
                if (!Stage1HasMistake)
                    _lastDistanceC = (_lastDistanceA + _lastDistanceB) * 0.5f;
                OVVAGenerator.UpdateNonCentralArea(_lastDistanceC);
            }
        }

        public void SaveData()
        {
            string fileName = "OVVA_ID_" + ParticipantID.options[ParticipantID.value].text +
                              "_Device_" + HMDName +
                              "_RenderResolution_" + RenderResolution.x + "x" + RenderResolution.y +
                              "_Eye_" + EyeCondition.options[EyeCondition.value].text +
                              "_" + OVVARecorder.GetFileName();
            OVVARecorder.OutputCSV(fileName);
            OVVAGenerator.ResetOVVA();
            LeftEyeMask.SetActive(false);
            RightEyeMask.SetActive(false);
            InstructionManager.OVVAUI.SetActive(true);
            StartCoroutine(DelayedLoadAndProcessSingleFile(fileName));
        }

        public IEnumerator DelayedLoadAndProcessSingleFile(string fileName)
        {
            yield return new WaitForSeconds(1.0f);
            InstructionManager.OVVAUI.SetActive(false);
            OVVADatasetBatchProcessing.LoadAndProcessSingleFile(fileName);
            OVVADatasetBatchProcessing.gameObject.SetActive(false);
            InstructionManager.OVVAUI.SetActive(true);
            ResultPanel.gameObject.SetActive(true);
            ResultPanel.texture = OVVADatasetBatchProcessing.OVVAHeatMapGenerator.OutputImage;
        }

        private void OnApplicationQuit()
        {
            // if (OVVAInputManager.UserInput != UserInput.End && OVVAStage == OVVAStage.Degradation)
            //     SaveData();
        }
    }
}