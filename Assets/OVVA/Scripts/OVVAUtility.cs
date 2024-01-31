using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

namespace ChaosIkaros.OVVA
{
    public enum UserInput
    {
        None,
        Up,
        Down,
        Left,
        Right,
        SubTaskStart,
        SubTaskEnd,
        SubTaskSkip,
        RoundStart,
        End
    }

    public enum OVVAStage
    {
        CentralVVA,
        Degradation
    }
    
    public enum DatasetScope
    {
        All,
        PerDevice,
        PerTexture
    }

    public enum HeatMapType
    {
        ErrorDensity,
        ErrorRate
    }

    public class OVVAUtility
    {
        public const string OVVADatasetPath = "OVVADataset";
        
        public static readonly float[] OptotypeAngles = new float[] { 0, 90, 180, 270 }; //right up left down
        
        public static readonly Dictionary<float, string> AngleNameMapping = new Dictionary<float, string>
        {
            {OptotypeAngles[0], "right"},
            {OptotypeAngles[1], "up"},
            {OptotypeAngles[2], "left"},
            {OptotypeAngles[3], "down"}
        };
        
        public static readonly Dictionary<UserInput, string> UserInputNameMapping = new Dictionary<UserInput, string>
        {
            {UserInput.Right, "right"},
            {UserInput.Up, "up"},
            {UserInput.Left, "left"},
            {UserInput.Down, "down"}
        };

        public static float VisualAngleConversion(ref float visualAngle, float objectSize, float objectDistance)
        {
            if (visualAngle == 0 && objectSize != 0 && objectDistance != 0)
            {
                visualAngle = (float)(2 * Mathf.Atan(objectSize / (2 * objectDistance)) * (180 / Math.PI));
                return visualAngle;
            }
            else if (visualAngle != 0 && objectSize == 0 && objectDistance != 0)
            {
                return (float)(2 * objectDistance * Mathf.Tan((float)(visualAngle * Math.PI / 360)));
            }
            else if (visualAngle != 0 && objectSize != 0 && objectDistance == 0)
            {
                return (float)((objectSize / 2) / Mathf.Tan((float)(visualAngle * Math.PI / 360)));
            }
            else
            {
                return 0;
            }
        }

        public static string DegreeToVisualAngle(float visualAngle)
        {
            float d = Mathf.Floor(visualAngle);
            float m = Mathf.Floor(60.0f * (visualAngle - d));
            float s = (visualAngle - d - m / 60.0f) * 60.0f;
            return d + "°" + m + "'" + s.ToString("F2") + "''";
        }

        public static float DegreeToVisualAngleFloat(float visualAngle)
        {
            float d = Mathf.Floor(visualAngle);
            float m = Mathf.Floor(60.0f * (visualAngle - d));
            float s = (visualAngle - d - m / 60.0f) * 60.0f;
            return (float)d * 60.0f + (float)m + (float)s * 60.0f / 60.0f;
        }

        public static float DegreeToDigitalVA(float visualAngle)
        {
            return 1.0f / DegreeToVisualAngleFloat(visualAngle);
        }
        
        public static float DegreeToLogMAR(float visualAngle)
        {
            return - Mathf.Log10(DegreeToDigitalVA(visualAngle));
        }

        public static void Shuffle(ref List<GameObject> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }

        public static string Vector3ToString(Vector3 pos)
        {
            return pos.x.ToString("F3") + ";" + pos.y.ToString("F3") + ";" + pos.z.ToString("F3");
        }

        public static Vector3 SphericalToCartesian(float radius, float polarDegree, float elevationDegree)
        {
            Vector3 result = Vector3.zero;
            float a = radius * Mathf.Cos(elevationDegree * Mathf.Deg2Rad);
            result.x = a * Mathf.Cos(polarDegree * Mathf.Deg2Rad);
            result.y = radius * Mathf.Sin(elevationDegree * Mathf.Deg2Rad);
            result.z = a * Mathf.Sin(polarDegree * Mathf.Deg2Rad);
            return result;
        }

        public static Vector3 CartesianToSpherical(Vector3 cartCoords)
        {
            float outRadius, outPolar, outElevation = 0;
            if (cartCoords.x == 0)
                cartCoords.x = Mathf.Epsilon;
            outRadius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                                   + (cartCoords.y * cartCoords.y)
                                   + (cartCoords.z * cartCoords.z));
            outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);
            if (cartCoords.x < 0)
                outPolar += Mathf.PI;
            outElevation = Mathf.Asin(cartCoords.y / outRadius);
            return new Vector3(outRadius, outPolar * Mathf.Rad2Deg, outElevation * Mathf.Rad2Deg);
        }
        
        public static void SetCameraBackground(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
        }
        
        public static string GetXRDeviceName()
        {
            InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (headDevice.name == null)
                return "None";
            return headDevice.name.Replace("_", "").Replace(" ", "");
        }
        
        public static void Texture2DToPng(Texture2D texture2D, string path)
        {
            var png = texture2D.EncodeToPNG();
            File.WriteAllBytes(path, png);
        }

        public static void Texture2DToAsset(Texture2D texture2D, string path)
        {
            File.WriteAllBytes(path, texture2D.GetRawTextureData());
        }
        
        public static List<float> DumpPixelsToList(Texture2D texture2D)
        {
            List<float> pixels = new List<float>();
            for (int x = 0; x < texture2D.height; x++)
            {
                for (int y = 0; y < texture2D.width; y++)
                {
                    float temp = texture2D.GetPixel(x, y).r;
                    pixels.Add(temp);
                }
            }
            pixels.Sort();
            return pixels;
        }
        
        public static void CopyTexture2DFrom(ref Texture2D result, Texture2D texture2D, TextureFormat textureFormat)
        {
            result = new Texture2D(texture2D.width, texture2D.height, textureFormat, false);
            result.SetPixels(texture2D.GetPixels());
            result.Apply();
        }

        public static void AssetToTexture2D(ref Texture2D texture2D, string path)
        {
            if (texture2D == null)
                texture2D = new Texture2D(180, 180, TextureFormat.RFloat, false);
            texture2D.LoadRawTextureData(File.ReadAllBytes(path));
            texture2D.Apply();
        }
        
        public static void CanvasToTexture2D(ref Texture2D texture2D, Canvas canvasToCapture, int imageWidth, int imageHeight)
        {
            RenderTexture tempRT = new RenderTexture(imageWidth, imageHeight, 24);
            tempRT.antiAliasing = 8;
            GameObject tempCameraGO = new GameObject("TempCamera");
            Camera tempCamera = tempCameraGO.AddComponent<Camera>();
            Camera mainCamera = Camera.main;
            tempCamera.CopyFrom(mainCamera);
            
            tempCamera.targetTexture = tempRT;
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.backgroundColor = Color.clear;
            RenderMode tempRenderMode = canvasToCapture.renderMode;
            canvasToCapture.renderMode = RenderMode.ScreenSpaceCamera;
            canvasToCapture.worldCamera = tempCamera;
            tempCamera.Render();
            
            texture2D = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
            RenderTexture.active = tempRT;
            texture2D.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            
            GameObject.DestroyImmediate(tempCameraGO);
            GameObject.DestroyImmediate(tempRT);
            canvasToCapture.renderMode = tempRenderMode;
            canvasToCapture.worldCamera = null;
        }

        public static string GetValidFileName(string input)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return System.Text.RegularExpressions.Regex.Replace(input, invalidRegStr, " ").Replace(" ", "_");
        }
        
        public static void CleanDirectory(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                CleanDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException) 
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(path);
        }
    }
}