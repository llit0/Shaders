using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using static UnityEngine.Mathf;
using Random = UnityEngine.Random;

public static class GraphFunctionLibrary
{
   public delegate Vector3 Function(float u, float v, float t);
   
   public static int FunctionCount => Functions.Length;
   
   private static Function[] Functions = { Wave, MultiWave, Ripple, Sphere, Torus };
   public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }

   public static Function GetFunction(FunctionName name) => Functions[(int)name];

   
   public static Vector3 Wave(float u, float v, float t)
   {
      return new Vector3(u, Sin(PI * (u + v + t)), v);
   }
   
   public static Vector3 MultiWave (float u, float v, float t)
   {
      Vector3 p = new (u, Sin(PI * (u + 0.5f * t)), v);
      p.y += 0.5f * Sin(2f * PI * (v + t));
      p.y += Sin(PI * (u + v + 0.25f * t));
      p.y *= 1f / 2.5f;
      return p;
   }
   
   public static Vector3 Ripple (float u, float v, float t) 
   {
      float d = Sqrt(u * u + v * v);
      float y = Sin(PI * (4f * d - t)) / (1f + 10f * d);
      return new Vector3(u, y, v);
   }

   public static Vector3 Sphere(float u, float v, float t)
   {
      float r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
      float s = r * Cos(0.5f * PI * v);
      
      return new Vector3(s * Sin(PI * u), r * Sin(PI * 0.5f * v), s * Cos(PI * u));
   }
   
   public static Vector3 Torus (float u, float v, float t)
   {
      float r1 = 0.7f + 0.1f * Sin(PI * (4f * u + 0.8f * t));
      float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 2f * v + 2.5f * t));
      float s = r1 + r2 * Cos(PI * v);
      return new Vector3(s * Sin(PI * u), r2 * Sin(PI * v), s * Cos(PI * u));
   }
   
   public static Vector3 Morph (float u, float v, float t, Function from, Function to, float progress) 
   {
      return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
   }
   
   public static FunctionName GetRandomFunctionNameOtherThan(FunctionName currentName)
   {
      FunctionName choice = (FunctionName)Random.Range(1, Functions.Length);
      return choice == currentName ? 0 : choice;
   }
}
