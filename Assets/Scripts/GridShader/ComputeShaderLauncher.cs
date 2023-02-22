using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class ComputeShaderLauncher : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture renderTexture;

    public Mesh mesh;
    public Material material;
    public int repetitions;
    public int count;

    private List<GameObject> objects;
    private Cube[] data;
    
    public struct Cube
    {
        public Vector3 position;
        public Color color;
    }
    
    private void Start()
    {
       
    }
    
    private void Update()
    {
        
    }

    public void createCubes()
    {
        objects = new List<GameObject>();
        data = new Cube[count * count];
        for(int x = 0; x < count; x++)
            for(int y = 0; y < count; y++)
                createCube(x, y);
    }

    private void createCube(int x, int y)
    {
        GameObject cube = new ("Cube " + x * count + y, typeof(MeshFilter), typeof(MeshRenderer));
        cube.GetComponent<MeshFilter>().mesh = mesh;
        cube.GetComponent<MeshRenderer>().material = new Material(material);
        cube.transform.position = new Vector3(x, y, Random.Range(-0.1f, 0.1f));

        Color color = Random.ColorHSV();
        cube.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
        objects.Add(cube);

        Cube cubeData = new ()
        {
            position = cube.transform.position,
            color = color
        };
        data[x * count + y] = cubeData;
    }
    
    /*private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!renderTexture)
        {
            renderTexture = new RenderTexture(256, 256, 24) { enableRandomWrite = true };
            renderTexture.Create();
        }
        
        computeShader.SetFloat("resolution", renderTexture.width);
        computeShader.SetTexture(0, "result", renderTexture);
        
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
        
        Graphics.Blit(renderTexture, dest);
    }*/

    public void randomizeCPU()
    {
        for (int i = 0; i < repetitions; i++)
        {
            foreach (GameObject obj in objects)
            {
                obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y,
                    Random.Range(-0.1f, 0.1f));
                obj.GetComponent<MeshRenderer>().material.SetColor("_Color", Random.ColorHSV());
            }
        }
    }

    public void randomizeGPU()
    {
        const int Size = sizeof(float) * 4 + sizeof(float) * 3;

        int colorSize = sizeof(float) * 4;
        int vectorSize = sizeof(float) * 3;
        int totalSize = colorSize + vectorSize;
        
        ComputeBuffer computeBuffer = new (data.Length, totalSize);
        computeBuffer.SetData(data);
        
        computeShader.SetBuffer(0, "cubes", computeBuffer);
        computeShader.SetFloat("resolution", data.Length);
        computeShader.SetFloat("repetitions", repetitions);
        computeShader.Dispatch(0, data.Length / 10, 1, 1);
   
        computeBuffer.GetData(data);
        
        for (int i = 0; i < objects.Count; i++)
        {
            float z = Mathf.Lerp(-0.1f, 0.1f, data[i].position.z);
            data[i].position = new Vector3(data[i].position.x, data[i].position.y, z);
            
            objects[i].transform.position = data[i].position;
            objects[i].GetComponent<MeshRenderer>().material.SetColor("_Color", data[i].color);
        }
        
        computeBuffer.Dispose();
    }

    private void OnGUI()
    {
        if (objects == null)
        {
            if(GUI.Button(new Rect(0, 0, 100, 50), "Create"))
                createCubes();
        }
        else
        {
            if(GUI.Button(new Rect(0, 0, 100, 50), "Random CPU"))
                randomizeCPU();
            if(GUI.Button(new Rect(100, 0, 100, 50), "Random GPU"))
                randomizeGPU();
        }
    }
}
