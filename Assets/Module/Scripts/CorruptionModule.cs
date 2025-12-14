using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public abstract class CorruptionModule
{
    protected CorruptionScript Module { get; private set; }
    protected Func<float, float, float> HeightMap { get; private set; }

    public CorruptionModule(CorruptionScript module)
    {
        Module = module;
    }

    public void StartModule()
    {
        Module.StartCoroutine(ModuleSetup());
    }

    private IEnumerator ModuleSetup()
    {
        Module.Terrain.GetComponent<MeshRenderer>().enabled = false;
        Dummify();
        Random();
        yield return Generate();
        HeightMap = CorruptionMesh.GenerateModifiedFunction(PointHeight);
        Module.Terrain.SetTerrain(HeightMap);
        Module.Terrain.GetComponent<MeshRenderer>().enabled = true;
        Module.PlaySound("InfectionSpread", Module.transform);
    }

    protected abstract IEnumerator Generate();

    protected abstract float PointHeight(float x, float z);

    public abstract string ModuleName();

    public void Dummify()
    {
        if (Module.Cluster == null)
            return;

        KMSelectable modSelectable = Module.GetComponent<KMSelectable>();
        if (Module.DummySelectable == null)
        {
            Module.DummySelectable = GameObject.Instantiate(Module.Cluster.Infected.First().DummySelectable, Module.transform);
            Module.DummySelectable.transform.localPosition = Vector3.zero;
            Module.DummySelectable.transform.localScale = Vector3.one;
            Module.DummySelectable.transform.localEulerAngles = Vector3.zero;
            Module.DummySelectable.Parent = modSelectable;
            CorruptionReflectionTools.UpdateSelectable(Module.DummySelectable);
        }
        modSelectable.Children = new KMSelectable[] { Module.DummySelectable };
        modSelectable.UpdateChildren();
    }

    private static bool _threading = false;
    protected IEnumerator RunThread(Action thread)
    {
        yield return new WaitWhile(() => _threading);
        _threading = true;
        bool complete = false;
        new Thread(() =>
        {
            thread();
            complete = true;
            _threading = false;
        }).Start();
        yield return new WaitUntil(() => complete);
    }

    private System.Random _random = null;
    protected System.Random Random()
    {
        if (_random == null)
        {
            int seed = UnityEngine.Random.Range(0, int.MaxValue);
            Module.Log("Random seed initialised as {0}.", seed);
            _random = new System.Random(seed);
        }
            
        return _random;
    }

    public float RandomFloat(float min, float max)
    {
        return (float)(Random().NextDouble() * (max - min) + min);
    }
    public int RandomInt(int min, int max)
    {
        return Random().Next(min, max);
    }
    public List<T> Shuffle<T>(List<T> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            int swapPos = RandomInt(i, values.Count);
            T swapSlot = values[i];
            values[i] = values[swapPos];
            values[swapPos] = swapSlot;
        }
        return values;
    }
    public T PickRandom<T>(List<T> values)
    {
        return values[RandomInt(0, values.Count)];
    }
}
