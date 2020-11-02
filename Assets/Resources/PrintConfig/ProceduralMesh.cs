using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(BoxCollider))]
public class ProceduralMesh : MonoBehaviour
{
    Mesh mesh;
    BoxCollider boxCollider;
    Vector3[] vertices;
    int[] triangles;
    float width;
    float height;
    float length;

    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        boxCollider = GetComponent<BoxCollider>();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    public void GenerateLine(Vector3 start, Vector3 end, float width, float height)
    {
        this.width = width;
        this.height = height;
        transform.position = (start + end) * 0.5f;
        transform.LookAt(end);
        // TODO: - 2*width makes the lines shorter to protect them being printed over each other in corners. Need to correct this
        //length = (end - start).magnitude - 2*width;
        length = (end - start).magnitude;
        MakeMeshData();
        CreateMesh();
        UpdateBoxCollider();
    }

    /*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(frontTopLeft, 0.03f);
        Gizmos.DrawSphere(frontBottomLeft, 0.03f);
        Gizmos.DrawSphere(frontTopRight, 0.03f);
        Gizmos.DrawSphere(frontBottomRight, 0.03f);
        Gizmos.DrawSphere(backTopLeft, 0.03f);
        Gizmos.DrawSphere(backBottomLeft, 0.03f);
        Gizmos.DrawSphere(backTopRight, 0.03f);
        Gizmos.DrawSphere(backBottomRight, 0.03f);
    }
    */

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
        var lineStart = new Vector3(0, 0, -0.5f * length);
        var lineEnd = new Vector3(0, 0, 0.5f * length);

        Vector3 dir = lineEnd - lineStart;

        frontTopLeft = lineEnd + Vector3.Cross(dir, Vector3.up).normalized * width / 2;
        frontBottomLeft = frontTopLeft - new Vector3(0, height, 0);
        frontTopRight = lineEnd + Vector3.Cross(dir, Vector3.down).normalized * width / 2;
        frontBottomRight = frontTopRight - new Vector3(0, height, 0);

        backTopLeft = lineStart + Vector3.Cross(dir, Vector3.up).normalized * width / 2;
        backBottomLeft = backTopLeft - new Vector3(0, height, 0);
        backTopRight = lineStart + Vector3.Cross(dir, Vector3.down).normalized * width / 2;
        backBottomRight = backTopRight - new Vector3(0, height, 0);

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
        boxCollider.center = new Vector3(0, -0.5f * height, 0);
        boxCollider.size = new Vector3(width, height, length);
    }
}
