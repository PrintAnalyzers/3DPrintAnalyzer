using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ProceduralMesh : MonoBehaviour
{
    public Vector3 StartPos;
    public Vector3 EndPos;

    Mesh mesh;
    BoxCollider boxCollider;
    Rigidbody rigidBody;
    Vector3[] vertices;
    int[] triangles;
    public float Width;
    public float Height;
    public float Length;

    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        boxCollider = GetComponent<BoxCollider>();
        rigidBody = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    public void GenerateLine(Vector3 start, Vector3 end, float width, float height)
    {
        StartPos = start;
        EndPos = end;
        this.Width = width;
        this.Height = height;
        Reset();
        Length = (end - start).magnitude;
        MakeMeshData();
        CreateMesh();
        UpdateBoxCollider();
    }

    public void Reset()
    {
        transform.position = (StartPos + EndPos) * 0.5f;
        transform.LookAt(EndPos);

        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.isKinematic = true;

        GetComponent<MeshRenderer>().material.SetColor("_Color", Color.white);
    }

    Vector3 frontTopLeft;
    Vector3 frontBottomLeft;
    Vector3 frontTopRight;
    Vector3 frontBottomRight;

    Vector3 backTopLeft;
    Vector3 backBottomLeft;
    Vector3 backTopRight;
    Vector3 backBottomRight;

    // Update is called once per frame
    void Update()
    {
    }

    void MakeMeshData()
    {
        var lineStart = new Vector3(0, 0, -0.5f * Length);
        var lineEnd = new Vector3(0, 0, 0.5f * Length);

        Vector3 dir = lineEnd - lineStart;

        frontTopLeft = lineEnd + Vector3.Cross(dir, Vector3.up).normalized * Width / 2;
        frontBottomLeft = frontTopLeft - new Vector3(0, Height, 0);
        frontTopRight = lineEnd + Vector3.Cross(dir, Vector3.down).normalized * Width / 2;
        frontBottomRight = frontTopRight - new Vector3(0, Height, 0);

        backTopLeft = lineStart + Vector3.Cross(dir, Vector3.up).normalized * Width / 2;
        backBottomLeft = backTopLeft - new Vector3(0, Height, 0);
        backTopRight = lineStart + Vector3.Cross(dir, Vector3.down).normalized * Width / 2;
        backBottomRight = backTopRight - new Vector3(0, Height, 0);

        vertices = new Vector3[]
        {
            backBottomLeft,
            backBottomRight,
            frontBottomLeft,
            frontBottomRight,

            backTopLeft,
            backTopRight,
            frontTopLeft,
            frontTopRight,
        };

        //Build a cube
        triangles = new int[]
        {
            // Top
            4, 6, 5,
            6, 7, 5,
            //Bottom
            1, 2, 0,
            1, 3, 2,
            //Left
            6, 4, 0,
            6, 0, 2,
            //Right
            5, 7, 1,
            1, 7, 3,
            //Front
            0, 4, 1,
            4, 5, 1,
            //Back
            7, 6, 3,
            6, 2, 3,
        };
    }

    void CreateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }

    void UpdateBoxCollider()
    {
        boxCollider.center = new Vector3(0, -0.5f * Height, 0);
        boxCollider.size = new Vector3(Width, Height, Length);
        boxCollider.enabled = false;
    }
}
