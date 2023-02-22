using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static GraphFunctionLibrary;
using Random = UnityEngine.Random;

public class Graph : MonoBehaviour
{
    [Header("Essential Creation Parameters")]
    [SerializeField]
    private Transform pointPrefab;
    
    [SerializeField, Range(10, 100)]
    private int resolution = 10;

    [SerializeField] 
    private Transform parent;

    [Header("Current Function")]
    [SerializeField]
    private FunctionName function;

    [Header("Function Switch Parameters")]
    public bool switchFunctions;
    public float transitionDuration = 1f;
    public float functionDuration = 1f;
    
    private bool transitioning;

    private float functionCurrentDuration;
    
    private void OnGUI()
    {
        if(GUI.Button(new Rect(0, 0, 100, 50), "Build Graph"))
            buildGraph();
    }

    private void Update()
    {
        if (transitioning)
            return;
        
        updateGraph();
        if (switchFunctions)
        {
            functionCurrentDuration += Time.deltaTime;
            if (functionCurrentDuration >= functionDuration)
            {
                functionCurrentDuration = 0;
                transitioning = true;
                StartCoroutine(updateGraphWithTransition(function));
            }
        }
        else
            functionCurrentDuration = 0f;
    }
    
    private void buildGraph()
    {
        float step = 2f / resolution;
        Vector3 scale = Vector3.one * step;

        for (int i = 0; i < resolution * resolution; i++) 
        {
            Transform point = Instantiate(pointPrefab, parent, false);
            point.localScale = scale;
        }
    }

    private void updateGraph()
    {
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < parent.childCount; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }
            float u = (x + 0.5f) * step - 1f;
            
            parent.GetChild(i).localPosition = GetFunction(function)(u, v, Time.time);
        }
    }
    
    private IEnumerator updateGraphWithTransition(FunctionName previousFunction)
    {
        function = GetRandomFunctionNameOtherThan(function);
        
        Function from = GetFunction(previousFunction);
        Function to = GetFunction(function);

        float progress = 0;
        while (progress < transitionDuration)
        {
            float step = 2f / resolution;
            float v = 0.5f * step - 1f;
            for (int i = 0, x = 0, z = 0; i < parent.childCount; i++, x++)
            {
                if (x == resolution)
                {
                    x = 0;
                    z += 1;
                    v = (z + 0.5f) * step - 1f;
                }

                float u = (x + 0.5f) * step - 1f;

                parent.GetChild(i).localPosition = Morph(u, v, Time.time, from, to, progress);
            }

            progress += Time.deltaTime;
            yield return null;
        }

        transitioning = false;
    }
}
