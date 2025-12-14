using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CryingCirriModule : CorruptionModule
{
    public static bool[][] DisplayedState =
    {
        new bool[] { true, true, false, false, false },
        new bool[] { true, false, true, false, false },
        new bool[] { true, false, false, true, false },
        new bool[] { true, false, false, false, true },
        new bool[] { false, true, true, false, false },
        new bool[] { false, true, false, true, false },
        new bool[] { false, true, false, false, true },
        new bool[] { false, false, true, true, false },
        new bool[] { false, false, true, false, true },
        new bool[] { false, false, false, true, true }
    };

    public static int[][] Table =
    {
        new int[] { 3, 6, 4, 2, 0, 5, 7, 8, 9, 1 },
        new int[] { 7, 0, 2, 8, 1, 3, 9, 5, 6, 4 },
        new int[] { 5, 6, 7, 8, 9, 4, 0, 2, 3, 1 },
        new int[] { 0, 4, 7, 1, 9, 5, 8, 6, 3, 2 },
        new int[] { 8, 5, 3, 9, 7, 4, 0, 6, 1, 2 }
    };

    private Stack<int> _stages = new Stack<int>();
    private List<bool[]> _solution;

    private List<bool[]> _enteredSolution;

    private float[] _vibrateSpeeds;
    private bool[] _fastVibration;
    private int _currentCycler = 0;

    public CryingCirriModule(CorruptionScript module) : base(module)
    {
    }

    protected override IEnumerator Generate()
    {
        GeneratePhysical();

        GenerateStage();

        yield break;
    }

    protected override float PointHeight(float x, float z)
    {
        return 0.015f;
    }

    public override string ModuleName()
    {
        return "Crying Cirri";
    }

    private void GeneratePhysical()
    {
        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.CryingCirriAssets;

        main.ExposeResources();

        _vibrateSpeeds = new float[5];
        _fastVibration = new bool[5];

        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        moduleSelectable.ChildRowLength = 0;
        moduleSelectable.Children = new KMSelectable[_vibrateSpeeds.Length];

        for (int i = 0; i < _vibrateSpeeds.Length; i++)
        {
            _vibrateSpeeds[i] = 0.25f;

            GameObject obj = new GameObject();
            obj.transform.SetParent(Module.transform);
            obj.transform.localPosition = new Vector3(Mathf.Sin(Mathf.PI * 2 * i / _vibrateSpeeds.Length), 0, Mathf.Cos(Mathf.PI * 2 * i / _vibrateSpeeds.Length)) * RandomFloat(0.05f, 0.075f) + new Vector3(0, 0.015f, 0);
            obj.transform.localScale = new Vector3(0.0625f, 1, 1);
            obj.transform.localEulerAngles = RandomFloat(Mathf.PI / 24f, Mathf.PI / 8f) * new Vector3(0, 0, 180 / Mathf.PI) + RandomFloat(0, Mathf.PI * 2f) * new Vector3(0, 180 / Mathf.PI, 0);

            GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
            newPiece.transform.localPosition = Vector3.zero;
            newPiece.transform.localScale = Vector3.one;
            Module.StartCoroutine(Vibrate(i, newPiece));


            GameObject obj2 = new GameObject();
            obj2.transform.SetParent(Module.transform);
            obj2.transform.localPosition = obj.transform.localPosition + new Vector3(0, 0.005f, 0);
            obj2.transform.localScale = Vector3.one;
            obj2.transform.localEulerAngles = Vector3.zero;

            KMSelectable newSelectable = GameObject.Instantiate(assets[1], obj2.transform).GetComponent<KMSelectable>();
            newSelectable.transform.localPosition = Vector3.zero;
            newSelectable.transform.localScale = Vector3.one;
            newSelectable.transform.localEulerAngles = new Vector3(-90, 0, 0);
            newSelectable.Parent = moduleSelectable;

            newSelectable.enabled = true;
            CorruptionReflectionTools.UpdateSelectable(newSelectable);
            moduleSelectable.Children[i] = newSelectable;

            int i2 = i;
            newSelectable.OnInteract += () =>
            {
                if (!newSelectable.enabled)
                    return false;

                newSelectable.AddInteractionPunch(0.5f);

                if (_fastVibration[i2])
                    Module.Log("Tendril {0} has been interacted with while it was signaling. Strike!", i2 + 1);
                else
                {
                    Module.StartCoroutine(Accelerate(i2, 16));

                    Module.PlaySound("Tendril", obj2.transform);

                    if (_enteredSolution.Last()[i2])
                        Module.Log("Tendril {0} has been interacted with, but was already submitted. Strike!", i2 + 1);
                    else if (!_solution[_enteredSolution.Count - 1][i2])
                        Module.Log("Tendril {0} has been interacted with, when it shouldn't have been. Strike!", i2 + 1);
                    else if (_solution[_enteredSolution.Count - 1].Count(x => x) != _enteredSolution.Last().Count(x => x) + 1)
                    {
                        _enteredSolution.Last()[i2] = true;
                        Module.Log("Tendril {0} has been interacted with successfully.", i2 + 1);
                        return false;
                    }
                    else if (_solution.Count != _enteredSolution.Count)
                    {
                        Module.Log("Tendril {0} has been interacted with successfully, completing a stage segment.", i2 + 1);
                        _enteredSolution.Add(new bool[_enteredSolution.Last().Length]);
                        return false;
                    }
                    else
                    {
                        Module.Log("Tendril {0} has been interacted with successfully, completing a stage.", i2 + 1);

                        if (_stages.Count >= 5)
                        {
                            foreach (KMSelectable selectable in moduleSelectable.Children)
                                selectable.enabled = false;
                            Module.Log("Module has been solved!");
                            Module.Pass();
                            return false;
                        }

                        GenerateStage();

                        return false;
                    }
                }

                _enteredSolution = new List<bool[]> { new bool[_vibrateSpeeds.Length] };
                Module.Strike();
                Module.StartCoroutine(CycleStage());





                return false;
            };
        }

        moduleSelectable.UpdateChildren();

        main.CloseResources();
    }

    private IEnumerator CycleStage()
    {
        int currentCycler = ++_currentCycler;
        while (!_enteredSolution.First().Any(x => x) && currentCycler == _currentCycler)
        {
            yield return new WaitForSeconds(1f);

            if (_enteredSolution.First().Any(x => x) || currentCycler != _currentCycler)
                break;

            foreach (int stage in _stages)
            {
                for (int i = 0; i < DisplayedState[stage].Length; i++)
                    if (DisplayedState[stage][i])
                        Module.StartCoroutine(Accelerate(i, 16));

                yield return new WaitForSeconds(1f);

                if (_enteredSolution.First().Any(x => x) || currentCycler != _currentCycler)
                    break;
            }
        }
    }

    private IEnumerator Accelerate(int index, float speed)
    {
        float totalTime = 1f;
        float currentTime = 0f;
        float origSpeed = _vibrateSpeeds[index];
        _fastVibration[index] = true;

        while (currentTime < totalTime)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;

            _vibrateSpeeds[index] = Mathf.Lerp(origSpeed, speed, 1 - Mathf.Abs(2 * currentTime / totalTime - 1));
        }

        _fastVibration[index] = false;
    }

    private IEnumerator Vibrate(int index, GameObject tendril)
    {
        float currentAngle = RandomFloat(0, Mathf.PI * 2);
        while (true)
        {
            yield return null;
            currentAngle += Time.deltaTime * _vibrateSpeeds[index] * Mathf.PI * 2;

            tendril.transform.localEulerAngles = new Vector3(-90, 0, 0) - currentAngle * new Vector3(0, 180 / Mathf.PI, 0);
        }
    }

    private void GenerateStage()
    {
        bool[] currentState = new bool[_vibrateSpeeds.Length];

        _solution = new List<bool[]>();

        _stages.Push(RandomInt(0, 10));

        for (int i = 0; i < _stages.Count; i++)
        {
            int[] newState = Enumerable.Range(0, currentState.Length).Select(x => (currentState[x] ? 1 : 0) + (DisplayedState[_stages.ElementAt(i)][x] ? 1 : 0)).ToArray();

            while (true)
            {
                int index = newState.IndexOf(x => x > 1);
                if (index == -1)
                    break;

                newState[(index + 1) % currentState.Length] += newState[index] / 2;
                newState[index] %= 2;
            }

            currentState = newState.Select(x => x > 0).ToArray();

            int transform = Table[i][_stages.ElementAt(i)];
            switch (transform)
            {
                case 0:
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                    currentState = Enumerable.Range(0, currentState.Length).Select(x => currentState[(x + (currentState.Length - transform)) % currentState.Length]).ToArray();
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    currentState = Enumerable.Range(0, currentState.Length).Select(x => currentState[(2 * transform - x) % currentState.Length]).ToArray();
                    break;
                default:
                    throw new Exception("Undefined stage transform");
            }

            _solution.Add(currentState);
        }

        Module.Log("The stage prepended is: [{0}].", Enumerable.Range(0, currentState.Length).Where(x => DisplayedState[_stages.Peek()][x]).Select(x => x + 1).Join("+"));
        Module.Log("The expected responses are: {0}.", _solution.Select(x => "[" + Enumerable.Range(0, currentState.Length).Where(y => x[y]).Select(y => y + 1).Join("+") + "]").Join(", "));

        _enteredSolution = new List<bool[]> { new bool[currentState.Length] };

        Module.StartCoroutine(CycleStage());
    }
}

