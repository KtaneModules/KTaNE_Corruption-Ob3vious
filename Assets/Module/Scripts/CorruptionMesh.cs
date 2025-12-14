using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DelaunatorSharp;
using System;

//this script is going to be awesome as fuck
public class CorruptionMesh : MonoBehaviour
{

    public MeshFilter testFilter;
    void Start()
    {
    }

    public void SetTerrain(Func<float, float, float> terrain)
    {
        Mesh mesh = GenerateModuleTerrain(terrain);
        testFilter.mesh = mesh;
        testFilter.sharedMesh = mesh;
    }

    public Mesh GenerateModuleTerrain(Func<float, float, float> terrain)
    {
        Mesh mesh = new Mesh();
        List<Vector2> points = new List<Vector2> {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        }; //contains vertices along the boundary and randomly scattered points inside the boundary

        const int borderResolution = 5; //how many times edges along the border are subdivided
        float segmentLength = 1 / borderResolution; //the length of an individual segment of the edge
        for (int i = 0; i < borderResolution; i++)
        {
            float segmentLengthMultiple = i * segmentLength;
            points.Add(new Vector2(0, segmentLengthMultiple));
            points.Add(new Vector2(1, segmentLengthMultiple));
            points.Add(new Vector2(segmentLengthMultiple, 0));
            points.Add(new Vector2(segmentLengthMultiple, 1));
        }

        const int scatteredPointsAmount = 4000; //how many points will be randomly placed inside the square
        const float epsilon = 0f;
        while (points.Count < (4 + scatteredPointsAmount)) //need points to be amt of points we want plus 4 for the 4 corner points (will update later if needed)
        {
            Vector2 randomPoint = new Vector2(UnityEngine.Random.Range(epsilon, 1 - epsilon), UnityEngine.Random.Range(epsilon, 1 - epsilon)); //generate a random point inside the square

            /*//check if the point is colinear with two other points
            bool colinear = false;
            for(int firstPointIndex = 0; firstPointIndex < points.Count; firstPointIndex++) //get indices of points for every pair of points
            {
                Vector2 firstPoint = points[firstPointIndex];
                for (int secondPointIndex = firstPointIndex + 1; secondPointIndex < points.Count; secondPointIndex++) //we can start at the index after the first index because these are unordered pairs. we don't need to check points 2 and 0 if we already checked points 0 and 2, and we don't want to use the same point twice for this check
                {
                    Vector2 secondPoint = points[secondPointIndex];

                    //could cause problems if x values are equal, making slope infinity. circumvented by preventing points from generating on the boundary, because then we know they would be colinear anyway
                    float firstSlope = (firstPoint.y - randomPoint.y) / (firstPoint.x - randomPoint.x);
                    float secondSlope = (secondPoint.y - secondPoint.y) / (secondPoint.x - secondPoint.x);
                    if (firstSlope == secondSlope) //if the slopes are equal, all three points are colinear and this random point cannot be here
                        colinear = true; //idk how to continue the while loop from here so i'm just doing this
                }
                if (colinear)
                    break;
            }
            if (!colinear) //if there are no pairs of points that are colinear this can be added*/
            points.Add(randomPoint);
            //Debug.LogFormat("Placed point at {0}, {1}", randomPoint.x, randomPoint.y);
        }

        int[] triangles = new Delaunator(ToPoints(points)).Triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            //Debug.LogFormat("Triangle {0}: {1} > {2} > {3}", i / 3, triangles[i], triangles[i + 1], triangles[i + 2]);
        }

        Vector3[] pointsWithHeight = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            int dummy = i;
            Vector2 point = points[dummy];
            pointsWithHeight[i] = new Vector3(point.x, terrain(point.x, point.y), point.y);
        }

        //set up mesh with new points and triangles
        mesh.Clear();
        mesh.vertices = pointsWithHeight;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); //makes the shading not look fucky

        return mesh;
    }

    //copied from extensions, this is the only one we need
    public IPoint[] ToPoints(IEnumerable<Vector2> vertices)
    {
        return vertices.Select(vertex => new Point(vertex.x, vertex.y)).OfType<IPoint>().ToArray();
    }

    public static Func<float, float, float> GenerateWaveNoise()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float waveLength = Mathf.Pow(2f, UnityEngine.Random.Range(-9f, -3f));
        float amplitude = Mathf.Sqrt(waveLength) / 512f * UnityEngine.Random.Range(0.5f, 1f);
        float offset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        float xFrequency = Mathf.Cos(angle) / waveLength;
        float zFrequency = Mathf.Sin(angle) / waveLength;

        return (x, z) => Mathf.Sin(offset + x * xFrequency + z * zFrequency) * amplitude;
    }

    public static Func<float, float, float> GenerateWaveBundle()
    {
        List<Func<float, float, float>> waves = new List<Func<float, float, float>>();
        for (int i = 0; i < 32; i++)
            waves.Add(GenerateWaveNoise());

        return (x, z) => waves.Sum(f => f(x, z));
    }

    public static Func<float, float, float> GenerateBorderBevel(float bevelAmount, float heightMult)
    {
        return (x, z) =>
            Mathf.Min(heightMult + x * (1 - heightMult) / bevelAmount, 1) * Mathf.Min(heightMult + (1 - x) * (1 - heightMult) / bevelAmount, 1) *
            Mathf.Min(heightMult + z * (1 - heightMult) / bevelAmount, 1) * Mathf.Min(heightMult + (1 - z) * (1 - heightMult) / bevelAmount, 1);
    }

    public static Func<float, float, float> GenerateModifiedFunction(Func<float, float, float> function)
    {
        Func<float, float, float> noise = GenerateWaveBundle();
        Func<float, float, float> bevel = GenerateBorderBevel(1 / 32f, 0f);

        return (x, z) => (function((x - 0.5f) / 5f, (z - 0.5f) / 5f) + noise(x, z)) * bevel(x, z);
    }
}