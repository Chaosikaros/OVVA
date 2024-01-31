using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR;
using TMPro;

namespace ChaosIkaros.OVVA
{
    public class OVVARecorder : MonoBehaviour
    {
        public TMP_Text RecorderDebug;
        public List<float> CentralAreaAccuracyList = new List<float> { };
        public List<string> OutputData = new List<string>();

        private string _recordingTypes =
            "time stamp, ID, time interval, distance, FOV, testStage, logMAR, userInput, currentPosA, currentPosB, diff, resolutionX, resolutionY, trialCondition, lastTaskAccuracy, taskSteps, allPositions, allAnswers \r\n";
        private string _dirpath = "";

        private void Awake()
        {
            _dirpath = Application.dataPath + "/" + OVVAUtility.OVVADatasetPath + "/";
            if (!Directory.Exists(_dirpath))
            {
                Directory.CreateDirectory(_dirpath);
            }
        }

        public void RecordData(float logMAR, float c, float a, float b, float validFov, float timer, float accuracy, float progress,
            string currentPositions, string currentAnswer, OVVAStage OVVAStage, UserInput userInput,
            string participantTrial)
        {
            string inputFrameTemp = GetTimeForFileName() + ",";
            inputFrameTemp += OutputData.Count + "," + timer + ",";
            inputFrameTemp += c + "," + validFov + "," + OVVAStage.ToString() + "," + logMAR + "," +
                              userInput.ToString() + ",";
            inputFrameTemp += a + "," + b + "," + Mathf.Abs((a - b)) + ",";
            inputFrameTemp += XRSettings.eyeTextureWidth + "," + XRSettings.eyeTextureHeight + "," +
                              participantTrial + ",";
            inputFrameTemp += accuracy + "," + progress + "," + currentPositions + "," + currentAnswer;
            inputFrameTemp += "\r\n";
            OutputData.Add(inputFrameTemp);
            RecorderDebug.text = "Current distance: " + c.ToString("F3")
                                                           + "\r\nDifference: " + Mathf.Abs((a - b)).ToString("F3") +
                                                           "; Valid FOV: " + validFov
                                                           + "\r\nCurrent logMAR (vision loss): " +
                                                           logMAR.ToString("F4") +
                                                           "; AvgAccuracy: " +
                                                           (CentralAreaAccuracyList.Sum() /
                                                            CentralAreaAccuracyList.Count).ToString("F2");
        }

        public static string GetFileName()
        {
            return GetTimeForFileName() + ".csv";
        }

        public static string GetTimeForFileName()
        {
            return System.DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss_ffffff");
        }

        public void OutputCSV(string fileName)
        {
            string fullPath = _dirpath + fileName;
            if (!File.Exists(fullPath))
                File.Create(fullPath).Dispose();
            Stream stream = File.OpenWrite(fullPath);
            BufferedStream bfs = new BufferedStream(stream);
            bfs.Seek(0, SeekOrigin.Begin);
            bfs.SetLength(0);
            byte[] buffType = new UTF8Encoding().GetBytes(_recordingTypes);
            bfs.Write(buffType, 0, buffType.Length);
            for (int i = 0; i < OutputData.Count; i++)
            {
                byte[] buffData = new UTF8Encoding().GetBytes(OutputData[i]);
                bfs.Write(buffData, 0, buffData.Length);
            }

            bfs.Flush();
            bfs.Close();
            stream.Close();
            Debug.Log("Saved file: " + fullPath);
            
        }
    }
}