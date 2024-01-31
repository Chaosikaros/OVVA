using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

namespace ChaosIkaros.OVVA
{
    public class OVVADatasetBatchProcessing : MonoBehaviour
    {
        public OVVAHeatMapGenerator OVVAHeatMapGenerator;
        public DatasetScope DatasetScope = DatasetScope.All;
        public bool EnableProcessingOnStarted = false;
        public bool PostProcessingOnly = false;
        public bool SingleFileProcessing = false;
        public List<FileInfo> FileList = new List<FileInfo>();
        public Dictionary<string, List<string>> DeviceToRecordingTypes = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> DeviceToAllEyeOrAllResolution = new Dictionary<string, List<string>>();

        private Dictionary<string, HashSet<string>> _deviceToResolutionConditions =
            new Dictionary<string, HashSet<string>>();

        private Dictionary<string, HashSet<string>> _deviceToEyeConditions = new Dictionary<string, HashSet<string>>();
        private string _directoryPath = "";
        private string _directoryPathForTemp = "";

        void Start()
        {
            _directoryPath = Application.dataPath + "/" + OVVAUtility.OVVADatasetPath + "/";
            _directoryPathForTemp = Application.dataPath + "/" + OVVAUtility.OVVADatasetPath + "/TempFiles/";
            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }

            if (!Directory.Exists(_directoryPathForTemp))
            {
                Directory.CreateDirectory(_directoryPathForTemp);
            }

            if (EnableProcessingOnStarted)
            {
                if (!SingleFileProcessing)
                {
                    GetAllFiles();
                    StartCoroutine(PostProcessing());
                }
                else
                {
                    LoadAndProcessSingleFile(
                        "OVVA_ID_1_Device_HeadTracking-OpenXR_RenderResolution_2064x2096_Eye_Both_2024_01_30_01_19_51_814150.csv");
                }
            }
        }

        public void LoadAndProcessSingleFile(string fileName)
        {
            OVVAHeatMapGenerator.HeatMapType = HeatMapType.ErrorDensity;
            FileInfo file = new FileInfo(_directoryPath + fileName);
            if (file.Exists && file.Name.StartsWith("OVVA") && !file.Name.Contains(".meta") &&
                file.Name.Contains(".csv"))
            {
                LoadDataset(file.Name);
                Debug.Log("Converted file: " + file.Name);
                var splitName = fileName.Split('_');
                var device = splitName[4];
                var recordingType = splitName[6] + "_" + splitName[8];
                OVVAHeatMapGenerator.HeapMapPreprocessing(device, splitName[6], splitName[8]);
                string TargetCentralVVA = OVVAHeatMapGenerator.TargetCentralVVA;
                OVVAHeatMapGenerator.HeatMapPostprocessing(ref OVVAHeatMapGenerator.RatioImage,
                    ref OVVAHeatMapGenerator.OutputImage, 0, DatasetScope.PerTexture, device, splitName[6],
                    splitName[8], TargetCentralVVA);
                OVVAHeatMapGenerator.UpdateHeatMap(_directoryPath);
            }
            else
            {
                Debug.Log("File does not exist or does not meet the requirements: " + file.Name);
            }
        }

        public FileInfo GetTargetFileName(string target)
        {
            DirectoryInfo folder = new DirectoryInfo(_directoryPathForTemp);
            FileInfo[] files = folder.GetFiles();
            List<FileInfo> fileList = new List<FileInfo>();
            for (int i = 0; i < files.Length; i++)
            {
                if (!files[i].Name.Contains(".meta") &&
                    files[i].Name.Contains(".tex2D"))
                {
                    fileList.Add(files[i]);
                }
            }

            return fileList.First(x => x.Name.Contains(target));
        }

        public void HeatMapPostprocessing(string name, string device, float max = 0)
        {
            OVVAHeatMapGenerator.InitializeOutputImage();

            OVVAUtility.AssetToTexture2D(ref OVVAHeatMapGenerator.OutputImage,
                _directoryPathForTemp + GetTargetFileName(device + "_" + name).Name);

            string[] parameters = GetTargetFileName(device + "_" + name).Name.Replace(".tex2D", "").Split("_");
            OVVAHeatMapGenerator.HeatMapPostprocessing(ref OVVAHeatMapGenerator.RatioImage,
                ref OVVAHeatMapGenerator.OutputImage, max, DatasetScope, device,
                parameters[1], parameters[2], parameters[3]);
            string fileName = "";
            if (OVVAHeatMapGenerator.HeatMapType == HeatMapType.ErrorDensity)
                fileName += "ROIErrorDensity_" + DatasetScope.ToString();
            else
                fileName += "ROIErrorRate_" + DatasetScope.ToString();
            string tempPath = _directoryPathForTemp + fileName + "\\" + device + "\\";
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            OVVAUtility.Texture2DToPng(OVVAHeatMapGenerator.RatioImage,
                tempPath + name + "_" + device + "_" + fileName + ".png");
            OVVAUtility.Texture2DToPng(OVVAHeatMapGenerator.RatioImage,
                _directoryPathForTemp + "r_" + device + "_" + name + ".png");

            OVVAHeatMapGenerator.UpdateHeatMap(_directoryPath);
        }

        public IEnumerator PostProcessing()
        {
            if (!PostProcessingOnly)
                yield return StartCoroutine(BatchProcessing());
            List<float> allMaxList = new List<float> { };
            if (DatasetScope == DatasetScope.All)
            {
                allMaxList = new List<float> { };

                foreach (var device in DeviceToAllEyeOrAllResolution)
                {
                    foreach (var recordingType in device.Value)
                    {
                        OVVAUtility.AssetToTexture2D(ref OVVAHeatMapGenerator.OutputImage,
                            _directoryPathForTemp + GetTargetFileName(device.Key + "_" + recordingType).Name);
                        OVVAUtility.CopyTexture2DFrom(ref OVVAHeatMapGenerator.RatioImage,
                            OVVAHeatMapGenerator.OutputImage,
                            TextureFormat.RGBA32);
                        List<float> temp = OVVAUtility.DumpPixelsToList(OVVAHeatMapGenerator.OutputImage);
                        allMaxList.Add(temp.Max());
                        yield return new WaitForEndOfFrame();
                    }
                }

                Debug.Log("Max all: " + allMaxList.Max());
            }

            foreach (var device in DeviceToAllEyeOrAllResolution)
            {
                if (DatasetScope == DatasetScope.PerDevice)
                {
                    OVVAHeatMapGenerator.InitializeOutputImage();
                    allMaxList = new List<float> { };
                    foreach (var recordingType in device.Value)
                    {
                        OVVAUtility.AssetToTexture2D(ref OVVAHeatMapGenerator.OutputImage,
                            _directoryPathForTemp + GetTargetFileName(device.Key + "_" + recordingType).Name);
                        OVVAUtility.CopyTexture2DFrom(ref OVVAHeatMapGenerator.RatioImage,
                            OVVAHeatMapGenerator.OutputImage,
                            TextureFormat.RGBA32);
                        List<float> temp = OVVAUtility.DumpPixelsToList(OVVAHeatMapGenerator.OutputImage);
                        allMaxList.Add(temp.Max());
                        yield return new WaitForEndOfFrame();
                    }

                    Debug.Log("Max all: " + allMaxList.Max());
                }

                foreach (var recordingType in device.Value)
                {
                    if (DatasetScope == DatasetScope.PerTexture)
                        HeatMapPostprocessing(recordingType, device.Key, 0);
                    else
                        HeatMapPostprocessing(recordingType, device.Key, allMaxList.Max());
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        public void InitializeDeviceToRecordingTypes(List<FileInfo> fileList)
        {
            DeviceToRecordingTypes = new Dictionary<string, List<string>>();
            foreach (var file in fileList)
            {
                var fileName = file.Name;
                var splitName = fileName.Split('_');
                var device = splitName[4];
                var recordingType = splitName[6] + "_" + splitName[8];
                if (DeviceToRecordingTypes.ContainsKey(device))
                {
                    if (!DeviceToRecordingTypes[device].Contains(recordingType))
                    {
                        DeviceToRecordingTypes[device].Add(recordingType);
                    }
                }
                else
                {
                    DeviceToRecordingTypes[device] = new List<string> { recordingType };
                }
            }

            _deviceToResolutionConditions = new Dictionary<string, HashSet<string>>();
            _deviceToEyeConditions = new Dictionary<string, HashSet<string>>();
            foreach (var device in DeviceToRecordingTypes)
            {
                _deviceToResolutionConditions[device.Key] = new HashSet<string>();
                _deviceToEyeConditions[device.Key] = new HashSet<string>();
                foreach (var recordingType in device.Value)
                {
                    string[] parameters = recordingType.Split('_');
                    _deviceToResolutionConditions[device.Key].Add(parameters[0]);
                    _deviceToEyeConditions[device.Key].Add(parameters[1]);
                }
            }

            DeviceToAllEyeOrAllResolution = new Dictionary<string, List<string>>();
            foreach (var device in DeviceToRecordingTypes.Keys)
            {
                foreach (var resolutionCondition in _deviceToResolutionConditions[device])
                {
                    string combination1 = resolutionCondition + "_All";
                    // Add the combination to the new dictionary
                    if (!DeviceToAllEyeOrAllResolution.ContainsKey(device))
                    {
                        DeviceToAllEyeOrAllResolution[device] = new List<string>();
                    }

                    DeviceToAllEyeOrAllResolution[device].Add(combination1);
                }

                foreach (var eyeCondition in _deviceToEyeConditions[device])
                {
                    string combination2 = "All_" + eyeCondition;
                    // Add the combination to the new dictionary
                    if (!DeviceToAllEyeOrAllResolution.ContainsKey(device))
                    {
                        DeviceToAllEyeOrAllResolution[device] = new List<string>();
                    }

                    DeviceToAllEyeOrAllResolution[device].Add(combination2);
                }
            }
        }

        public void GetAllFiles()
        {
            DirectoryInfo folder = new DirectoryInfo(_directoryPath);
            FileInfo[] files = folder.GetFiles();
            FileList.Clear();
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Name.StartsWith("OVVA") && !files[i].Name.Contains(".meta") &&
                    files[i].Name.Contains(".csv"))
                {
                    FileList.Add(files[i]);
                }
            }

            InitializeDeviceToRecordingTypes(FileList);
        }

        public IEnumerator BatchProcessing()
        {
            OVVAUtility.CleanDirectory(_directoryPathForTemp);
            OVVAHeatMapGenerator.DataFrames = new List<OVVADataFrame> { };
            for (int i = 0; i < FileList.Count; i++)
            {
                LoadDataset(FileList[i].Name);
                Debug.Log("Converted file: " + FileList[i].Name);
            }

            foreach (var device in DeviceToRecordingTypes.Keys)
            {
                foreach (var resolutionCondition in _deviceToResolutionConditions[device])
                {
                    OVVAHeatMapGenerator.HeapMapPreprocessing(device, resolutionCondition, "All",
                        _directoryPathForTemp);
                    yield return new WaitForEndOfFrame();
                }

                foreach (var eyeCondition in _deviceToEyeConditions[device])
                {
                    OVVAHeatMapGenerator.HeapMapPreprocessing(device, "All", eyeCondition, _directoryPathForTemp);
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        public void LoadDataset(string fileName)
        {
            string path = _directoryPath + fileName;
            int counter = 0;

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamReader sr = new StreamReader(bs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (counter > 0)
                    {
                        string[] rawDataClips = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (rawDataClips[5] == "Degradation")
                        {
                            OVVADataFrame temp = OVVADataFrame.StringToDataFrame(fileName, 
                                rawDataClips[1], rawDataClips[3], rawDataClips[4],
                                rawDataClips[6], rawDataClips[11], rawDataClips[12],
                                rawDataClips[14], rawDataClips[16], rawDataClips[17]);
                            OVVAHeatMapGenerator.DataFrames.Add(temp);
                        }
                    }

                    counter++;
                }
            }
        }
    }
}