using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrintBed : MonoBehaviour
{
    public Material BedMaterial; 
    public float xSize = 400;
    public float ySize = 400;

    // Start is called before the first frame update
    void Start()
    {
        GameObject plane = new GameObject("Plane");
        
        MeshFilter meshFilter = (MeshFilter)plane.AddComponent(typeof(MeshFilter));
        meshFilter.mesh = CreateMesh();
        
        MeshRenderer renderer = plane.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        renderer.material = BedMaterial;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.green);
        tex.Apply();
        renderer.material.mainTexture = tex;
    }

    Mesh CreateMesh()
    {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        m.vertices = new Vector3[] {
         new Vector3(0, 0, 0),
         new Vector3(0, 0, ySize),
         new Vector3(xSize, 0, ySize),
         new Vector3(xSize, 0, 0)
     };
        m.uv = new Vector2[] {
         new Vector2 (0, 0),
         new Vector2 (0, 1),
         new Vector2(1, 1),
         new Vector2 (1, 0)
     };
        m.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateNormals();

        return m;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
