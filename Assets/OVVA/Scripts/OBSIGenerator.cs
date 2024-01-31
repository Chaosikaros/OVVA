using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChaosIkaros.OVVA
{
    public class OBSIGenerator : MonoBehaviour
    {
        public const int OptotypeInterval = 1;
        public float StartRadius = 0.75f;
        public GameObject OptotypePrefab;
        public Transform CameraTransform;
        private List<GameObject> _optotypeList = new List<GameObject> { };

        // Start is called before the first frame update
        void Start()
        {
            OVVAUtility.SetCameraBackground(CameraTransform.GetComponent<Camera>());
            GenerateOBSI();
        }

        public void ResetDistance(string input)
        {
            StartRadius = float.Parse(input);
        }

        public void StartAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(MapAnimation());
        }

        public IEnumerator MapAnimation()
        {
            StartRadius = 0;
            for (int i = 0; i < 12; i++)
            {
                StartRadius += 0.5f;
                Debug.Log(StartRadius);
                yield return new WaitForSeconds(1.0f);
                foreach (GameObject go in _optotypeList)
                {
                    go.SetActive(false);
                }

                UpdateOBSI(StartRadius, CameraTransform);

                foreach (GameObject go in _optotypeList)
                {
                    go.SetActive(true);
                }
            }
        }

        public void ResetOBSI()
        {
            foreach (GameObject go in _optotypeList)
            {
                Destroy(go);
            }

            _optotypeList.Clear();
        }

        public void GenerateOBSI()
        {
            ResetOBSI();
            for (int x = 0; x < 180; x += OptotypeInterval)
            {
                for (int y = -90; y < 90; y += OptotypeInterval)
                {
                    _optotypeList.Add(GameObject.Instantiate(OptotypePrefab));
                }
            }
            UpdateOBSI(StartRadius, CameraTransform);
        }

        public void UpdateOBSI(float radius, Transform cameraTrans)
        {
            int counter = 0;
            for (int x = 0; x < 180; x += OptotypeInterval)
            {
                for (int y = -90; y < 90; y += OptotypeInterval)
                {
                    GameObject go = _optotypeList[counter];
                    go.transform.SetParent(cameraTrans);
                    go.transform.GetChild(0).localRotation = Quaternion.Euler(0, -180, OVVAUtility.OptotypeAngles[counter % 4]);
                    go.transform.localPosition = OVVAUtility.SphericalToCartesian(radius, x, y);
                    go.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    counter++;
                }
            }
        }
    }
}