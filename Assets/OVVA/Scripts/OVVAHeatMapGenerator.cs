using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using TMPro;

namespace ChaosIkaros.OVVA
{
    public class OVVAHeatMapGenerator : MonoBehaviour
    {
        public const int HeatMapTextureSize = 1800;
        public CanvasGroup CanvasGroup;
        public HeatMapType HeatMapType = HeatMapType.ErrorDensity;
        public const float PlaceHolder = -1;
        public List<OVVADataFrame> DataFrames = new List<OVVADataFrame> { };
        public List<OVVADataFrame> FilteredDataFrames = new List<OVVADataFrame> { };
        public Gradient HeatMapGradient;
        public Texture2D OutputImage;
        public Texture2D RatioImage;
        public Texture2D GradientImage;
        public RawImage HeatMapDisplayUI;
        public RawImage GradientDisplayUI;
        public Slider CentralVVASlider;
        public TMP_Text DatasetInfo;
        public string TargetDevice;
        public string TargetResolution;
        public string TargetEye;
        public string TargetCentralVVA;

        void Awake()
        {
            DrawGradientOnTexture();
        }

        public void DrawGradientOnTexture()
        {
            GradientImage = new Texture2D(256, 1);
            for (int i = 0; i < GradientImage.width; ++i)
            {
                Color gradientCol = HeatMapGradient.Evaluate(i / (float)GradientImage.width);
                GradientImage.SetPixel(i, 0, gradientCol);
            }

            GradientImage.Apply();
            GradientDisplayUI.texture = GradientImage;
            //OVVAUtility.Texture2DToPng(GradientImage, Application.dataPath + "/OVVA/Textures/gradient.png");
        }

        public void UpdateDatasetInfo()
        {
            CentralVVASlider.value = float.Parse(TargetCentralVVA);
            DatasetInfo.text = "LogMAR:" + TargetCentralVVA
                                          + "\r\nHMD:" + TargetDevice
                                          + "\r\nRenderResolution:" + TargetResolution
                                          + "\r\nEye:" + TargetEye;
        }

        public void HeapMapPreprocessing(string targetDevice, string targetResolution, string targetEye,
            string exportPath = "")
        {
            TargetDevice = targetDevice;
            TargetResolution = targetResolution;
            TargetEye = targetEye;
            InitializeOutputImage();
            Dictionary<Vector3, Vector2> optotypeDictionary = GetOptotypeDictionary();
            TargetCentralVVA = FilteredDataFrames.Average(x => x.LogMAR).ToString("F4");
            UpdateOutputAndRatioImages(optotypeDictionary);
            ApplyErrorRateIfRequired();
            SaveOutputImage(exportPath);
        }

        public void InitializeOutputImage()
        {
            OutputImage = new Texture2D(HeatMapTextureSize, HeatMapTextureSize, TextureFormat.RFloat, false);
            OutputImage.filterMode = FilterMode.Point;
            for (int x = 0; x < OutputImage.height; x++)
            {
                for (int y = 0; y < OutputImage.width; y++)
                {
                    OutputImage.SetPixel(x, y, new Color(PlaceHolder, 0, 0, 0));
                }
            }

            OutputImage.Apply();
        }

        private Dictionary<Vector3, Vector2> GetOptotypeDictionary()
        {
            FilteredDataFrames.Clear();
            Dictionary<Vector3, Vector2> optotypeDictionary = new Dictionary<Vector3, Vector2> { };
            foreach (var frame in DataFrames)
            {
                if (!ShouldProcessDataFrame(frame, TargetDevice, TargetResolution, TargetEye)) continue;
                FilteredDataFrames.Add(frame);
                UpdateOptotypeDictionary(optotypeDictionary, frame);
            }

            return optotypeDictionary;
        }

        private bool ShouldProcessDataFrame(OVVADataFrame frame, string targetDevice, string targetResolution,
            string targetEye)
        {
            if (frame.DeviceName != targetDevice) return false;
            if (targetResolution != "All" && targetEye != "All")
            {
                return frame.Eye == targetEye && frame.Condition == targetResolution;
            }

            if (targetEye != "All")
            {
                return frame.Eye == targetEye;
            }

            if (targetResolution != "All")
            {
                return frame.Condition == targetResolution;
            }

            return true;
        }

        private void UpdateOptotypeDictionary(Dictionary<Vector3, Vector2> optotypeDictionary, OVVADataFrame frame)
        {
            for (int j = 0; j < frame.OptotypePositions.Count; j++)
            {
                Vector3 tempPos = frame.OptotypeSphericalPositions[j];
                if (!optotypeDictionary.ContainsKey(tempPos))
                {
                    optotypeDictionary.Add(tempPos, Vector2.zero);
                }

                Vector2 currentInfo = optotypeDictionary[tempPos];
                if (frame.OptotypeAccuracies[j])
                {
                    currentInfo.x += 1;
                }

                currentInfo.y += 1;
                optotypeDictionary[tempPos] = currentInfo;
            }
        }

        private void UpdateOutputAndRatioImages(Dictionary<Vector3, Vector2> optotypeDictionary)
        {
            OVVAUtility.CopyTexture2DFrom(ref RatioImage, OutputImage, OutputImage.format);

            foreach (KeyValuePair<Vector3, Vector2> entry in optotypeDictionary)
            {
                UpdatePixelsForEntry(entry);
            }
        }

        private void UpdatePixelsForEntry(KeyValuePair<Vector3, Vector2> entry)
        {
            Vector2 centerPos = new Vector2(entry.Key.y * 10, (entry.Key.z + 90) * 10);
            float radius = 20;
            for (int x = (int)(centerPos.x - radius); x <= centerPos.x + radius; x++)
            {
                for (int y = (int)(centerPos.y - radius); y <= centerPos.y + radius; y++)
                {
                    Vector2 currentPos = new Vector2(x, y);
                    if (Vector2.Distance(currentPos, centerPos) < 20)
                    {
                        UpdatePixelsForPosition(x, y, entry);
                    }
                }
            }
        }

        private void UpdatePixelsForPosition(int xRaw, int yRaw, KeyValuePair<Vector3, Vector2> entry)
        {
            int x = OutputImage.width - xRaw - 1; // mirrored X
            int y = yRaw;
            if (HeatMapType == HeatMapType.ErrorDensity)
            {
                if (OutputImage.GetPixel(x, y).r == PlaceHolder)
                    OutputImage.SetPixel(x, y, new Color(0, 0, 0, 0));
                OutputImage.SetPixel(x, y,
                    new Color(OutputImage.GetPixel(x, y).r + entry.Value.y - entry.Value.x, 0, 0, 0));
                //entry.Value.y - entry.Value.x = all - correct = wrong
            }
            else
            {
                if (OutputImage.GetPixel(x, y).r == PlaceHolder)
                    OutputImage.SetPixel(x, y, new Color(0, 0, 0, 0));
                if (RatioImage.GetPixel(x, y).r == PlaceHolder)
                    RatioImage.SetPixel(x, y, new Color(0, 0, 0, 0));
                OutputImage.SetPixel(x, y,
                    new Color(OutputImage.GetPixel(x, y).r + entry.Value.y - entry.Value.x, 0, 0, 0));
                //entry.Value.y - entry.Value.x = all - correct = wrong
                RatioImage.SetPixel(x, y,
                    new Color(RatioImage.GetPixel(x, y).r + entry.Value.y, 0, 0, 0));
            }
        }

        private void ApplyErrorRateIfRequired()
        {
            if (HeatMapType == HeatMapType.ErrorRate)
            {
                for (int x = 0; x < OutputImage.height; x++)
                {
                    for (int y = 0; y < OutputImage.width; y++)
                    {
                        if (RatioImage.GetPixel(x, y).r != PlaceHolder)
                        {
                            OutputImage.SetPixel(x, y,
                                new Color(OutputImage.GetPixel(x, y).r / RatioImage.GetPixel(x, y).r, 0, 0, 0));
                        }
                    }
                }
            }

            OutputImage.Apply();
        }

        private void SaveOutputImage(string exportPath)
        {
            if (Directory.Exists(exportPath))
            {
                string fileName = TargetDevice + "_" + TargetResolution + "_" + TargetEye + "_" + TargetCentralVVA;
                OVVAUtility.Texture2DToPng(OutputImage, exportPath + fileName + ".png");
                OVVAUtility.Texture2DToAsset(OutputImage, exportPath + fileName + ".tex2D");
            }
        }

        public void HeatMapPostprocessing(ref Texture2D ratioTexture2D, ref Texture2D output, float maxAll,
            DatasetScope datasetScope, string targetDevice, string targetResolution, string targetEye,
            string targetCentralVVA)
        {
            TargetDevice = targetDevice;
            TargetResolution = targetResolution;
            TargetEye = targetEye;
            TargetCentralVVA = targetCentralVVA;
            OVVAUtility.CopyTexture2DFrom(ref ratioTexture2D, output, TextureFormat.RGBA32);
            float max = maxAll;
            if (maxAll == 0 || datasetScope == DatasetScope.PerTexture)
            {
                List<float> allPixels = OVVAUtility.DumpPixelsToList(output);
                max = allPixels.Max();
            }

            for (int x = 0; x < ratioTexture2D.height; x++)
            {
                for (int y = 0; y < ratioTexture2D.width; y++)
                {
                    if (HeatMapType == HeatMapType.ErrorDensity)
                    {
                        if (output.GetPixel(x, y).r != PlaceHolder)
                            ratioTexture2D.SetPixel(x, y, HeatMapGradient.Evaluate(output.GetPixel(x, y).r / max));
                        else
                            ratioTexture2D.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        if (output.GetPixel(x, y).r != PlaceHolder)
                            ratioTexture2D.SetPixel(x, y, HeatMapGradient.Evaluate(output.GetPixel(x, y).r));
                        else
                            ratioTexture2D.SetPixel(x, y, Color.black);
                    }
                }
            }

            ratioTexture2D.Apply();
        }

        public void UpdateHeatMap(string exportDirectoryPath = "")
        {
            CanvasGroup.alpha = 1;
            HeatMapDisplayUI.texture = RatioImage;
            if (!Directory.Exists(exportDirectoryPath))
                return;
            UpdateDatasetInfo();
            OVVAUtility.CanvasToTexture2D(ref OutputImage, HeatMapDisplayUI.canvas, 1920, 1080);
            OVVAUtility.Texture2DToPng(OutputImage,
                exportDirectoryPath + OVVAUtility.GetValidFileName(DatasetInfo.text) + ".png");
        }
    }
}