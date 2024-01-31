using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Text;

namespace ChaosIkaros.OVVA
{
    [Serializable]
    public class OVVADataFrame
    {
        public int ID;
        public int ValidFOV;
        public float LogMAR;
        public float Radius;
        public float Accuracy;
        public string DeviceName;
        public string Condition;
        public string Eye;
        public Vector2Int RenderResolution;
        public List<Vector3> OptotypePositions;
        public List<bool> OptotypeAccuracies;
        public List<Vector3> OptotypeSphericalPositions;

        public static OVVADataFrame StringToDataFrame(string fileName, string ID, string radius, string fov, string logMAR, 
            string resolutionX, string resolutionY, string accuracy, string positions, string accuracies)
        {
            OVVADataFrame OVVADataFrame = new OVVADataFrame();
            OVVADataFrame.ID = int.Parse(ID);
            OVVADataFrame.LogMAR = float.Parse(logMAR);
            int.TryParse(resolutionX, out var x);
            int.TryParse(resolutionY, out var y);
            OVVADataFrame.RenderResolution = new Vector2Int(x, y);
            OVVADataFrame.Radius = float.Parse(radius);
            OVVADataFrame.ValidFOV = int.Parse(fov);
            OVVADataFrame.Accuracy = float.Parse(accuracy);
            string[] fileNames = fileName.Split('_');
            OVVADataFrame.DeviceName = fileNames[4];
            OVVADataFrame.Condition = fileNames[6];
            OVVADataFrame.Eye = fileNames[8];
            OVVADataFrame.OptotypePositions = new List<Vector3> { };
            string[] rawPos = positions.Split(';', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rawPos.Count(); i += 3)
            {
                OVVADataFrame.OptotypePositions.Add(new Vector3(float.Parse(rawPos[i]),
                    float.Parse(rawPos[i + 1]), float.Parse(rawPos[i + 2])));
            }

            OVVADataFrame.OptotypeSphericalPositions = new List<Vector3> { };
            for (int i = 0; i < OVVADataFrame.OptotypePositions.Count; i++)
            {
                OVVADataFrame.OptotypeSphericalPositions.Add(
                    OVVAUtility.CartesianToSpherical(OVVADataFrame.OptotypePositions[i]));
            }

            OVVADataFrame.OptotypeAccuracies = new List<bool> { };
            string[] rawAcc = accuracies.Split(';', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rawAcc.Count(); i++)
            {
                if (rawAcc[i] == "1")
                    OVVADataFrame.OptotypeAccuracies.Add(true);
                else
                    OVVADataFrame.OptotypeAccuracies.Add(false);
            }

            return OVVADataFrame;
        }
    }
}
