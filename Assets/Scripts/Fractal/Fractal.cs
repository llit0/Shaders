using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class Fractal : MonoBehaviour
{
    [SerializeField, Range(3, 8)]
    private int depth = 4;
    
    [SerializeField]
    private Mesh mesh;

    [SerializeField] 
    private Mesh leafMesh;
    
    [SerializeField]
    private Material material;

    private static quaternion[] Rotations =
    {
        quaternion.identity, quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };
    
    private struct FractalPart 
    {
        public Vector3 worldPosition;
        public Quaternion rotation;
        public Quaternion worldRotation;
        public float spinAngle;
        public float maxSagAngle;
        public float spinVelocity;
    }
    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    private struct UpdateFractalLevelJob : IJobFor
    {
        public float scale;
        public float deltaTime;
        
        [ReadOnly]
        public NativeArray<FractalPart> parents;
        public NativeArray<FractalPart> parts;
        
        [WriteOnly]
        public NativeArray<float3x4> matrices;

        public UpdateFractalLevelJob(
            float scale, float deltaTime, 
            NativeArray<FractalPart> parents, NativeArray<FractalPart> parts, NativeArray<float3x4> matrices)
        {
            this.scale = scale;
            this.parents = parents;
            this.parts = parts;
            this.matrices = matrices;
            this.deltaTime = deltaTime;
        }
        
        public void Execute(int index)
        {
            FractalPart parent = parents[index / 5];
            FractalPart part = parts[index];
            part.spinAngle += part.spinVelocity * deltaTime;
            
            float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
            
            float3 sagAxis = cross(up(), upAxis);

            float sagMagnitude = length(sagAxis);
            quaternion baseRotation;
            if (sagMagnitude > 0f)
            {
                sagAxis /= sagMagnitude;
                quaternion sagRotation = quaternion.AxisAngle(sagAxis, part.maxSagAngle * sagMagnitude);
                baseRotation = mul(sagRotation, parent.worldRotation);
            }
            else 
                baseRotation = parent.worldRotation;
            
            part.worldPosition = (float3)parent.worldPosition + mul(part.worldRotation, float3(0f, 1.5f * scale, 0f));
            part.worldRotation = mul(baseRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[index] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
                
            parts[index] = part;
        }
    }
    
    private NativeArray<FractalPart>[] parts;
    private NativeArray<float3x4>[] matrices;
    
    private ComputeBuffer[] matricesBuffers;
    private MaterialPropertyBlock fractalProperties;
    
    [SerializeField] 
    private Gradient gradient1;

    [SerializeField] 
    private Gradient gradient2;
    
    [SerializeField]
    private Color leafColor1;
    [SerializeField]
    private Color leafColor2;
    
    [SerializeField, Range(0f, 90f)] 
    private float maxSagAngleA = 15f, maxSagAngleB = 25f;

    [SerializeField, Range(0f, 90f)] 
    private float spinSpeed1 = 20f; 
    [SerializeField, Range(0, 90f)]
    private float spinSpeed2 = 25f;
    [SerializeField, Range(0f, 1f)]
    private float reverseSpinChance = 0.25f;
    
    private static readonly int MatricesID = Shader.PropertyToID("_Matrices");
    private static readonly int SequenceFactorsID = Shader.PropertyToID("_SequenceFactors");
    private static readonly int Color1ID = Shader.PropertyToID("_Color1");
    private static readonly int Color2ID = Shader.PropertyToID("_Color2");
    
    private Vector4[] sequenceFactors;
    private void OnEnable()
    {
        sequenceFactors = new Vector4[depth];
        fractalProperties ??= new MaterialPropertyBlock();
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];
        matricesBuffers = new ComputeBuffer[depth];
        for (int i = 0, length = 1; i < depth; i++, length *= 5)
        {
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, 12 * 4);
            sequenceFactors[i] = new Vector4(Random.value, Random.value);
        }
        
        parts[0][0] = createPart(0);
        for (int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
        {
            NativeArray<FractalPart> levelParts = parts[levelIndex];
            for (int fractalPartIndex = 0; fractalPartIndex < levelParts.Length; fractalPartIndex += 5)
                for (int childIndex = 0; childIndex < 5; childIndex++)
                    levelParts[fractalPartIndex + childIndex] = createPart(childIndex);
        }
    }
    
    private void OnDisable ()
    {
        for (int i = 0; i < depth; i++)
        {
            matricesBuffers[i].Release();
            parts[i].Dispose();
            matrices[i].Dispose();
        }

        sequenceFactors = null;
        parts = null;
        matrices = null;
        matricesBuffers = null; 
    }

    private void OnValidate()
    {
        if (parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update () 
    {
        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += rootPart.spinVelocity * Time.deltaTime;
        rootPart.worldRotation = mul(transform.rotation,
            mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
        rootPart.worldPosition = transform.position;
        parts[0][0] = rootPart;
        
        float objectScale = transform.lossyScale.x;
        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);
        
        float scale = objectScale;

        JobHandle handle = default;
        for (int levelIndex = 1; levelIndex < parts.Length; levelIndex++)
        {
            scale *= 0.5f;
            handle = new UpdateFractalLevelJob(scale, Time.deltaTime, 
                parts[levelIndex - 1], parts[levelIndex], matrices[levelIndex])
                .ScheduleParallel(parts[levelIndex].Length, 5, handle);
            
        }

        handle.Complete();
        
        Bounds bounds = new (parts[0][0].worldPosition, 3f * objectScale * Vector3.one);
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            Color color1, color2;
            Mesh instanceMesh;
            if (i == matricesBuffers.Length - 1)
            {
                color1 = leafColor1;
                color2 = leafColor2;
                instanceMesh = leafMesh;
            }
            else
            {
                instanceMesh = mesh;
                float gradientInterpolatorValue = i / (matricesBuffers.Length - 2f);
                color1 = gradient1.Evaluate(gradientInterpolatorValue);
                color2 = gradient2.Evaluate(gradientInterpolatorValue);
            }
            matricesBuffers[i].SetData(matrices[i]);
            fractalProperties.SetBuffer(MatricesID,  matricesBuffers[i]);

            fractalProperties.SetColor(Color1ID, color1);
            fractalProperties.SetColor(Color2ID, color2);
            fractalProperties.SetVector(SequenceFactorsID, sequenceFactors[i]);
            Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds,
                matricesBuffers[i].count, fractalProperties);
        }
    }

    private FractalPart createPart (int childIndex) => new ()
    {
        rotation = Rotations[childIndex],
        maxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
        spinVelocity = (Random.value < reverseSpinChance ? -1f : 1f) * radians(Random.Range(spinSpeed1, spinSpeed2))
    };

}
