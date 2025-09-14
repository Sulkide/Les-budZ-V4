using UnityEngine;
using System;
public class Waves : MonoBehaviour
{
    
    public int Dimension = 10;
    public Octave[] Octaves;
    public float uvScale;
    protected MeshFilter meshFilters;
    protected Mesh mesh;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = new Mesh();
        mesh.name = gameObject.name;

        mesh.vertices = GenerateVerts();
        mesh.triangles = GenerateTries();
        mesh.uv = GenerateUVs();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        meshFilters = gameObject.AddComponent<MeshFilter>();
        meshFilters.mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        var verts = mesh.vertices;
        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                var y = 0f;
                for (int o = 0; o < Octaves.Length; o++)
                {
                    if (Octaves[o].alternate)
                    {
                        var perl = Mathf.PerlinNoise((x * Octaves[o].scale.x) / Dimension,
                            (z * Octaves[o].scale.y) / Dimension) * Mathf.PI * 2f;
                        y += Mathf.Cos(perl + Octaves[o].speed.magnitude * Time.time) * Octaves[o].height;
                    }
                    else
                    {
                        var perl = Mathf.PerlinNoise(
                            (x * Octaves[o].scale.x) + Time.time * Octaves[o].speed.x / Dimension,
                            (z * Octaves[o].scale.y) + Time.time * Octaves[o].speed.y / Dimension) - 0.5f;
                        y += perl * Octaves[o].height;
                    }
                }
                
                verts[index(x, z)] = new Vector3(x, y, z);
            }
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }

    private Vector3[] GenerateVerts()
    {
        var verts = new Vector3[(Dimension + 1) * (Dimension + 1)];

        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                verts[index(x, z)] = new Vector3(x, 0, z);
            }
        }
        
        return verts;
    }

    private int index(int x, int z)
    {
        return x * (Dimension + 1) + z;
    }

    private int[] GenerateTries()
    {
        var tries = new int[mesh.vertices.Length * 6];

        for (int x = 0; x < Dimension; x++)
        {
            for (int z = 0; z < Dimension; z++)
            {
                tries[index(x,z) * 6 + 0] = index(x,z);
                tries[index(x,z) * 6 + 1] = index(x+1,z + 1);
                tries[index(x,z) * 6 + 2] = index(x+1,z);
                tries[index(x,z) * 6 + 3] = index(x,z);
                tries[index(x,z) * 6 + 4] = index(x,z+1);
                tries[index(x,z) * 6 + 5] = index(x+1,z+1);
            }
        }
        return tries;
    }

    private Vector2[] GenerateUVs()
    {
        var uvs = new Vector2[mesh.vertices.Length];
        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                var vec = new Vector2((x / uvScale) % 2, (z / uvScale) % 2);
                uvs[index(x,z)] = new Vector2(vec.x <= 1 ? vec.x : 2 - vec.x, vec.y <= 1 ? vec.y : 2 - vec.y);
            }
        }
        
        return uvs;
    }



    [Serializable]
    public struct Octave
    {
        public Vector2 speed;
        public Vector2 scale;
        public float height;
        public bool alternate;
    }
}
