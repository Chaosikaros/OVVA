using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptotypesComparison : MonoBehaviour
{
    public TMP_Text OptotypeInfoText;
    public Transform ParentTransform;

    public List<GameObject> OptotypeList = new List<GameObject> { };
    public List<GameObject> GeneratedOptotypes = new List<GameObject> { }; // List to store generated optotypes

    public float rotationAngle = 0f; // Exposed rotation parameter

    // Start is called before the first frame update
    void Start()
    {
        OptotypeInfoText.text = "Optotypes: ";
        for (int i = OptotypeList.Count - 1; i >= 0; i--)
        {
            OptotypeInfoText.text += OptotypeList[i].name + ";";
        }

        GenerateOptotypes();
    }

    public void GenerateOptotypes()
    {
        float xInterval = 0.01f;
        float initialScale = 0.0075f;
        float maxScaleFactor = 0.14f;
        float scaleFactor = maxScaleFactor;
        float minScaleFactor = 0.002f;
        float scaleDecrement = 0.002f;
        int loopTime = (int)((maxScaleFactor - minScaleFactor) / scaleDecrement);
        float previousYScale = initialScale;
        Vector3 newScale = Vector3.zero;
        for (int j = 0; j < loopTime; j++)
        {
            for (int i = 0; i < OptotypeList.Count; i++)
            {
                GameObject go = Instantiate(OptotypeList[i],
                    ParentTransform.position + new Vector3(scaleFactor * xInterval * i, previousYScale, 0),
                    Quaternion.identity);
                go.transform.parent = ParentTransform;
                newScale = scaleFactor * initialScale * Vector3.one;
                go.transform.localScale = newScale;

                GeneratedOptotypes.Add(go); // Add the generated optotype to the list
            }

            previousYScale += newScale.y;
            scaleFactor -= scaleDecrement;
            scaleFactor = Mathf.Clamp(scaleFactor, minScaleFactor, maxScaleFactor);
        }
    }

    private void OnValidate()
    {
        foreach (GameObject go in GeneratedOptotypes)
        {
            go.transform.rotation = Quaternion.Euler(0, 0, rotationAngle); // Update the rotation of generated optotypes
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}