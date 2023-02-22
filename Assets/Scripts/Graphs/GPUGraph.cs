using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GraphFunctionLibrary;

public class GPUGraph : MonoBehaviour
{
    [Header("Essential Creation Parameters")]
    [SerializeField]
    private ComputeShader computeShader;

    private const int MaxResoltion = 1000;
    
    [SerializeField, Range(10, MaxResoltion)]
    private int resolution = 10;

    [SerializeField] 
    private Transform parent;
    
    [SerializeField]
    private Material material;

    [SerializeField]
    private Mesh mesh;
    
    [Header("Current Function")]
    [SerializeField]
    private FunctionName function;

    [Header("Function Switch Parameters")]
    public bool switchFunctions;
    public float transitionDuration = 1f;
    public float functionDuration = 1f;
    
    private bool transitioning;
    private float functionCurrentDuration;
    private FunctionName previousFunction;
    
    private ComputeBuffer positionsBuffer;

    private static readonly int PositionsId = Shader.PropertyToID("_Positions");
    private static readonly int ResolutionId = Shader.PropertyToID("_Resolution");
    private static readonly int StepId = Shader.PropertyToID("_Step");
    private static readonly int TimeId = Shader.PropertyToID("_Time");
    private static readonly int TransitionProgressId = Shader.PropertyToID("_TransitionProgress");
    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(MaxResoltion * MaxResoltion, sizeof(float) * 3);
    }

    private void OnDisable()
    {
        positionsBuffer.Dispose();
        positionsBuffer = null;
    }
    
    private void Update()
    {
        updateTransitioningState();
        updateFunctionOnGPU();
    }

    private void updateFunctionOnGPU () 
    {
        float step = 2f / resolution;
        computeShader.SetInt(ResolutionId, resolution);
        computeShader.SetFloat(StepId, step);
        computeShader.SetFloat(TimeId, Time.time);
        
        if (transitioning)
            computeShader.SetFloat(TransitionProgressId, Mathf.SmoothStep(0f, 1f, functionCurrentDuration / transitionDuration));

        int kernelIndex = (int)function + (int)(transitioning ? previousFunction : function) * FunctionCount;
        
        computeShader.SetBuffer(kernelIndex, PositionsId, positionsBuffer);
        
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);
            
        material.SetBuffer(PositionsId, positionsBuffer);
        material.SetFloat(StepId, step);
        Bounds bounds = new (Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }

    private void updateTransitioningState()
    {
        if (switchFunctions)
        {
            functionCurrentDuration += Time.deltaTime;
            if (!transitioning && functionCurrentDuration >= functionDuration)
            {
                functionCurrentDuration = 0;
                transitioning = true;
                previousFunction = function;
                function = GetRandomFunctionNameOtherThan(function);
            }
            else if (transitioning && functionCurrentDuration >= transitionDuration)
            {
                transitioning = false;
                functionCurrentDuration = 0;
            }
        }
    }
}
