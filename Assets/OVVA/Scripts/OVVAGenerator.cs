using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChaosIkaros.OVVA
{
    public class OVVAGenerator : MonoBehaviour
    {
        public float StartRadius = 3f;
        public Vector2 CenterPos = new Vector2(90, 0);
        public const float HalfFOVStart = 2.5f;
        public const float HalfFOVInc = 5f;
        public const float CentralAreaAccuracyThreshold = 1.0f;
        public const float OptotypeDif = 0.01f;
        private List<GameObject> _optotypeList = new List<GameObject> { };
        public List<GameObject> ActiveOptotypeList = new List<GameObject> { };
        public GameObject OptotypePrefab;
        public Transform CameraTransform;

        // Start is called before the first frame update
        void Start()
        {
            OVVAUtility.SetCameraBackground(CameraTransform.GetComponent<Camera>());
        }

        public void ResetOVVA()
        {
            foreach (GameObject go in _optotypeList)
            {
                Destroy(go);
            }

            _optotypeList.Clear();
        }
        
        public void GenerateOVVA()
        {
            ResetOVVA();
            StartRadius = 0.75f;
            int maxRing = 18;
            int counter = 0;
            for (int j = 0; j < maxRing; j++)
            {
                float radius = HalfFOVStart + HalfFOVInc * j;
                for (int x = 0; x < 360; x += 360 / 8)
                {
                    float vertical = Mathf.Sin(x * Mathf.Deg2Rad);
                    float horizontal = Mathf.Cos(x * Mathf.Deg2Rad);
                    Vector2 spawnDir = new Vector2(horizontal, vertical);
                    Vector2 spawnPos = CenterPos + spawnDir * radius;
                    DrawELetter(counter, StartRadius, spawnPos);
                    counter++;
                }
            }
        }
        
        public void ShowSingleOptotype(int repeatCounter)
        {
            if (ActiveOptotypeList.Count == 0)
                return;
            for (int i = 0; i < ActiveOptotypeList.Count; i++)
                ActiveOptotypeList[i].SetActive(false);
            ActiveOptotypeList[repeatCounter].SetActive(true);
        }

        public void UpdateNonCentralArea(float distance)
        {
            List<Vector2> focusPos = new List<Vector2> { };
            float radius = HalfFOVStart;
            for (int x = 0; x < 360; x += 360 / 8)
            {
                float vertical = Mathf.Sin(x * Mathf.Deg2Rad);
                float horizontal = Mathf.Cos(x * Mathf.Deg2Rad);
                Vector2 spawnDir = new Vector2(horizontal, vertical);
                Vector2 spawnPos = CenterPos + spawnDir * radius;
                focusPos.Add(spawnPos);
            }
            
            for (int i = 0; i < _optotypeList.Count; i++)
            {
                _optotypeList[i].SetActive(false);
            }
            
            for (int i = 0; i < focusPos.Count; i++)
            {
                DrawELetter(i, distance, focusPos[i]);
            }

            ActiveOptotypeList.Clear();
            for (int i = 0; i < _optotypeList.Count; i++)
            {
                if (_optotypeList[i].activeSelf == true)
                    ActiveOptotypeList.Add(_optotypeList[i]);
            }

            OVVAUtility.Shuffle(ref ActiveOptotypeList);
        }

        public void UpdateCentralArea(float distance, float radiusFOV)
        {
            List<Vector2> peripheralPos = new List<Vector2> { };
            float radius = radiusFOV;
            for (int x = 0; x < 360; x += 360 / 8)
            {
                float vertical = Mathf.Sin(x * Mathf.Deg2Rad);
                float horizontal = Mathf.Cos(x * Mathf.Deg2Rad);
                Vector2 spawnDir = new Vector2(horizontal, vertical);
                Vector2 spawnPos = CenterPos + spawnDir * radius;
                peripheralPos.Add(spawnPos);
            }

            for (int i = 0; i < _optotypeList.Count; i++)
            {
                _optotypeList[i].SetActive(false);
            }
            
            for (int i = 0; i < peripheralPos.Count; i++)
            {
                DrawELetter(i, distance, peripheralPos[i]);
            }

            ActiveOptotypeList.Clear();
            for (int i = 0; i < _optotypeList.Count; i++)
            {
                if (_optotypeList[i].activeSelf == true)
                    ActiveOptotypeList.Add(_optotypeList[i]);
            }

            OVVAUtility.Shuffle(ref ActiveOptotypeList);
        }

        public void DrawELetter(int i, float distance, Vector2 pos)
        {
            if (i < _optotypeList.Count)
            {
                _optotypeList[i].SetActive(true);
                _optotypeList[i].transform.GetChild(0).localRotation =
                    Quaternion.Euler(0, -180, OVVAUtility.OptotypeAngles[UnityEngine.Random.Range(0, 4)]);
                _optotypeList[i].transform.localPosition = OVVAUtility.SphericalToCartesian(distance, pos.x, pos.y);
                _optotypeList[i].transform.LookAt(CameraTransform);
            }
            else
            {
                GameObject go = GameObject.Instantiate(OptotypePrefab);
                go.transform.SetParent(CameraTransform);
                go.transform.GetChild(0).localRotation =
                    Quaternion.Euler(0, -180, OVVAUtility.OptotypeAngles[UnityEngine.Random.Range(0, 4)]);
                go.transform.localPosition = OVVAUtility.SphericalToCartesian(distance, pos.x, pos.y);
                go.transform.LookAt(CameraTransform);
                _optotypeList.Add(go);
            }
        }
    }
}