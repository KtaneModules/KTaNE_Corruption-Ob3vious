using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ClottedCapillariesModule : CorruptionModule
{
    public static int Dimension = 4;
    public static int[] EulerDiagram = { -1, -1, 1, 5, 2, 5, 6, 3, 0, 6, 4, 2, 4, 1, 3, 0 };

    private int[,,] _wireStates;
    private List<int> _solutionWires;
    private List<int> _cutWires;

    private float _flowAngle;


    public ClottedCapillariesModule(CorruptionScript module) : base(module)
    {
    }

    protected override IEnumerator Generate()
    {
        GeneratePuzzle();

        GeneratePhysical();

        yield break;
    }

    protected override float PointHeight(float x, float z)
    {
        //2 hills and an elliptic hole inbetween
        float hillX = -Mathf.Sin(_flowAngle) * 0.01f * (2 * Dimension - 2);
        float hillZ = Mathf.Cos(_flowAngle) * 0.01f * (2 * Dimension - 2);
        float dist1 = Mathf.Sqrt((x - hillX) * (x - hillX) + (z - hillZ) * (z - hillZ));
        float dist2 = Mathf.Sqrt((x + hillX) * (x + hillX) + (z + hillZ) * (z + hillZ));
        float hill1 = (Math.Max(0.025f - dist1, 0) / 0.025f) * (Math.Max(0.025f - dist1, 0) / 0.025f) * (3 - 2 * (Math.Max(0.025f - dist1, 0) / 0.025f)) * 0.01f;
        float hill2 = (Math.Max(0.025f - dist2, 0) / 0.025f) * (Math.Max(0.025f - dist2, 0) / 0.025f) * (3 - 2 * (Math.Max(0.025f - dist2, 0) / 0.025f)) * 0.01f;
        float pit = (Math.Max(0.15f - dist1 - dist2, 0) / 0.15f) * (Math.Max(0.15f - dist1 - dist2, 0) / 0.15f) * (3 - 2 * (Math.Max(0.15f - dist1 - dist2, 0) / 0.15f)) * 0.075f;

        return 0.015f + hill1 + hill2 - pit;
    }

    public override string ModuleName()
    {
        return "Clotted Capillaries";
    }

    private void GeneratePhysical()
    {
        _cutWires = new List<int>();

        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.ClottedCapillariesAssets;

        main.ExposeResources();

        GameObject obj = new GameObject();
        obj.transform.SetParent(Module.transform);
        obj.transform.localPosition = new Vector3(0, 0.015f + 0.005f / 2f, 0);
        obj.transform.localScale = Vector3.one;
        obj.transform.localEulerAngles = Vector3.zero;

        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        moduleSelectable.ChildRowLength = (Dimension * 2 - 2);
        moduleSelectable.Children = new KMSelectable[(Dimension * 2 - 2) * (Dimension * 2 - 2)];

        for (int i = 0; i < Dimension * Dimension * 2; i++)
        {
            int state = _wireStates[i / 2 % Dimension, i / 2 / Dimension, i % 2];
            if (state == 0)
                continue;

            int left = i / 2 % Dimension;
            int right = i / 2 / Dimension;

            GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
            newPiece.transform.localPosition = new Vector3(-0.005f * (2 * (left - right) + 1 - 2 * (i % 2)), 0, 0.02f * (left + right) - 0.01f * (2 * Dimension - 3));
            newPiece.transform.localEulerAngles = new Vector3(i % 2 > 0 ? -90 : 90, 0, 0);

            if (state == 2)
            {
                GameObject newClot = GameObject.Instantiate(assets[3], newPiece.transform);
                newClot.transform.localPosition = Vector3.zero;
                newClot.transform.localScale = Vector3.one;
                newClot.transform.localEulerAngles = Vector3.zero;
                continue;
            }

            //selectable here
            KMSelectable newSelectable = GameObject.Instantiate(assets[1], obj.transform).GetComponent<KMSelectable>();
            newSelectable.transform.localPosition = newPiece.transform.localPosition;
            newSelectable.Parent = moduleSelectable;

            //_cutSelectables[i].Add(newSelectable);
            newSelectable.enabled = true;
            CorruptionReflectionTools.UpdateSelectable(newSelectable);
            moduleSelectable.Children[(Dimension * 2 - 3 - (left + right)) * (Dimension * 2 - 2) + right - left + i % 2 + 2] = newSelectable;

            int i2 = i;

            newSelectable.OnInteract += () =>
            {
                if (!newSelectable.enabled)
                    return false;

                newPiece.GetComponent<MeshFilter>().mesh = assets[2].GetComponent<MeshFilter>().mesh;
                newSelectable.enabled = false;
                moduleSelectable.Children[Array.IndexOf(moduleSelectable.Children, newSelectable)] = null;
                moduleSelectable.UpdateChildren();

                Module.PlaySound("Snip", newPiece.transform);

                _cutWires.Add(i2);
                if (!_solutionWires.Contains(i2) || _solutionWires.TakeWhile(x => x != i2).Any(x => !_cutWires.Contains(x)))
                {
                    Module.Log("Wire {0} has been cut, which is incorrect. Strike!", (i2 / 2 + 1) + ":" + "LR"[i2 % 2]);
                    Module.Strike();
                    return false;
                }

                Module.Log("Wire {0} has been cut, which is correct.", (i2 / 2 + 1) + ":" + "LR"[i2 % 2]);
                if (_solutionWires.Any(x => !_cutWires.Contains(x)))
                    return false;

                Module.Log("Module has been solved!");
                Module.Pass();

                return false;
            };
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
            newPiece.transform.localPosition = new Vector3(0, -0.005f, 0.01f * (2 * Dimension - 1) * (2 * i - 1));
            newPiece.transform.localEulerAngles = new Vector3(0, 90 * (2 * i - 1), 90);
        }

        moduleSelectable.UpdateChildren();

        main.CloseResources();

        _flowAngle = RandomFloat(-1f, 1f) * Mathf.PI / 6f;

        obj.transform.localEulerAngles = _flowAngle * new Vector3(0f, -180 / Mathf.PI, 0f);
    }

    private void GeneratePuzzle()
    {
        while (true)
        {
            while (true)
            {
                //pos 0 is increment 1st + left, pos 1 is increment 2nd + right
                _wireStates = new int[Dimension, Dimension, 2];
                for (int i = 0; i < Dimension; i++)
                    for (int j = 0; j < Dimension - 1; j++)
                    {
                        _wireStates[j, i, 0] = 1;
                        _wireStates[i, j, 1] = 1;
                    }

                Queue<int> omit = new Queue<int>(Shuffle(Enumerable.Range(0, Dimension * (Dimension - 1) * 2).ToList()));

                int attempts = 6;
                while (attempts-- > 0)
                {
                    int pos = omit.Dequeue();
                    _wireStates[pos / 2 / (Dimension - pos % 2), pos / 2 % (Dimension - pos % 2), pos % 2] = RandomInt(0, 2) * 2;
                }

                Queue<int> deadTest = new Queue<int>(Enumerable.Range(0, Dimension * Dimension));
                bool[] dead = new bool[deadTest.Count];
                while (deadTest.Count > 0)
                {
                    int test = deadTest.Dequeue();
                    if (test != Dimension * Dimension - 1 && _wireStates[test % Dimension, test / Dimension, 0] == 0 && _wireStates[test % Dimension, test / Dimension, 1] == 0)
                    {
                        dead[test] = true;
                        if (test % Dimension > 0 && !dead[test - 1] && !deadTest.Contains(test - 1))
                            deadTest.Enqueue(test - 1);
                        if (test / Dimension > 0 && !dead[test - Dimension] && !deadTest.Contains(test - Dimension))
                            deadTest.Enqueue(test - Dimension);

                        if (test % Dimension > 0)
                            _wireStates[test % Dimension - 1, test / Dimension, 0] = 0;
                        if (test / Dimension > 0)
                            _wireStates[test % Dimension, test / Dimension - 1, 1] = 0;
                    }

                    if (test != 0 && (test % Dimension == 0 || _wireStates[test % Dimension - 1, test / Dimension, 0] == 0) && (test / Dimension == 0 || _wireStates[test % Dimension, test / Dimension - 1, 1] == 0))
                    {
                        dead[test] = true;
                        if (test % Dimension + 1 < Dimension && !dead[test + 1] && !deadTest.Contains(test + 1))
                            deadTest.Enqueue(test + 1);
                        if (test / Dimension + 1 < Dimension && !dead[test + Dimension] && !deadTest.Contains(test + Dimension))
                            deadTest.Enqueue(test + Dimension);

                        _wireStates[test % Dimension, test / Dimension, 0] = 0;
                        _wireStates[test % Dimension, test / Dimension, 1] = 0;
                    }
                }

                if (dead.Count(x => x) <= 2)
                    break;
            }

            int[] diagramValue = new int[Dimension * Dimension];
            bool[] forwardFlow = new bool[Dimension * Dimension];
            bool[] backwardFlow = new bool[Dimension * Dimension];

            for (int i = 0; i < diagramValue.Length; i++)
            {
                //structure
                int shape = 0;
                if (i == 0 || i == diagramValue.Length - 1)
                    shape++;
                if (i % Dimension != 0 && _wireStates[i % Dimension - 1, i / Dimension, 0] != 0)
                    shape++;
                if (i / Dimension != 0 && _wireStates[i % Dimension, i / Dimension - 1, 1] != 0)
                    shape++;
                if (_wireStates[i % Dimension, i / Dimension, 0] != 0)
                    shape++;
                if (_wireStates[i % Dimension, i / Dimension, 1] != 0)
                    shape++;

                if (shape == 3)
                    diagramValue[i] |= 1;

                //past
                //cells get prepared for by previous cells
                if (i == 0)
                    forwardFlow[i] = true;
                if (i % Dimension < Dimension - 1)
                {
                    diagramValue[i + 1] |= ((diagramValue[i] & 2) >> 1) + _wireStates[i % Dimension, i / Dimension, 0] >= 2 ? 2 : 0;
                    forwardFlow[i + 1] |= forwardFlow[i] && _wireStates[i % Dimension, i / Dimension, 0] == 1;
                }
                if (i / Dimension < Dimension - 1)
                {
                    diagramValue[i + Dimension] |= ((diagramValue[i] & 2) >> 1) + _wireStates[i % Dimension, i / Dimension, 1] >= 2 ? 2 : 0;
                    forwardFlow[i + Dimension] |= forwardFlow[i] && _wireStates[i % Dimension, i / Dimension, 1] == 1;
                }

                //future
                //cells handle their own calculations
                int inv = diagramValue.Length - 1 - i;
                if (inv == diagramValue.Length - 1)
                    backwardFlow[inv] = true;
                if (inv % Dimension < Dimension - 1)
                {
                    diagramValue[inv] |= ((diagramValue[inv + 1] & 4) >> 2) + _wireStates[inv % Dimension, inv / Dimension, 0] >= 2 ? 4 : 0;
                    backwardFlow[inv] |= backwardFlow[inv + 1] && _wireStates[inv % Dimension, inv / Dimension, 0] == 1;
                }
                if (inv / Dimension < Dimension - 1)
                {
                    diagramValue[inv] |= ((diagramValue[inv + Dimension] & 4) >> 2) + _wireStates[inv % Dimension, inv / Dimension, 1] >= 2 ? 4 : 0;
                    backwardFlow[inv] |= backwardFlow[inv + Dimension] && _wireStates[inv % Dimension, inv / Dimension, 1] == 1;
                }
            }

            for (int i = 0; i < diagramValue.Length; i++)
                diagramValue[i] |= forwardFlow[i] && backwardFlow[i] ? 8 : 0;

            int[] nodeValues = diagramValue.Select(x => EulerDiagram[x]).ToArray();

            List<List<int>> lines = new List<List<int>>();
            List<List<int>> positions = new List<List<int>>();
            for (int i = 0; i < Dimension * Dimension; i++)
            {
                if (nodeValues[i] == -1)
                    continue;

                int line = i % Dimension + i / Dimension;
                while (lines.Count <= line)
                {
                    lines.Add(new List<int>());
                    positions.Add(new List<int>());
                }
                lines[line].Add(nodeValues[i]);
                positions[line].Add(i);
            }

            List<int> marked = new List<int>();
            for (int i = 0; i < lines.Count; i++)
            {
                switch (lines[i].Count)
                {
                    case 1:
                        marked.Add(positions[i][0]);
                        break;

                    case 2:
                        if (lines[i][0] == lines[i][1])
                            marked.Add(positions[i][0]);
                        else
                            marked.Add(positions[i][1]);
                        break;

                    case 3:
                        if (lines[i].Distinct().Count() == 1)
                            marked.Add(positions[i][1]);
                        else if (lines[i].Distinct().Count() == 2)
                            marked.Add(positions[i][lines[i].IndexOf(lines[i].First(x => lines[i].Count(y => y == x) == 1))]);
                        else
                            marked.Add(positions[i][lines[i].IndexOf(lines[i].Min())]);
                        break;

                    case 4:
                        if (lines[i].Distinct().Count() == 1)
                            marked.Add(positions[i][3]);
                        else if (lines[i].Distinct().Count() == 2 && lines[i].Any(x => lines[i].Count(y => y == x) == 1))
                            marked.Add(positions[i][3 - lines[i].IndexOf(lines[i].First(x => lines[i].Count(y => y == x) == 1))]);
                        else if (lines[i].Distinct().Count() == 2)
                            marked.Add(positions[i][lines[i].IndexOf(lines[i][3])]);
                        else if (lines[i].Distinct().Count() == 3)
                            marked.Add(positions[i][lines[i].LastIndexOf(lines[i].First(x => lines[i].Count(y => y == x) > 1))]);
                        else
                            marked.Add(positions[i][1]);
                        break;

                    default:
                        throw new Exception("Line has size " + lines.Count + " which is unsupported.");
                }


            }

            _solutionWires = new List<int>();

            for (int i = 0; i < marked.Count - 1; i++)
            {
                if (marked[i] % Dimension < Dimension - 1 && marked[i] + 1 == marked[i + 1] && _wireStates[marked[i] % 4, marked[i] / 4, 0] == 1)
                    _solutionWires.Add(marked[i] * 2);
                if (marked[i] / Dimension < Dimension - 1 && marked[i] + Dimension == marked[i + 1] && _wireStates[marked[i] % 4, marked[i] / 4, 1] == 1)
                    _solutionWires.Add(marked[i] * 2 + 1);
            }

            if (_solutionWires.Count == 0)
                continue;

            Module.Log("The connections (rotated to have the start be in the top left corner) is: {0}.", Enumerable.Range(0, Dimension).Select(x => Enumerable.Range(0, Dimension).Select(y => _wireStates[y, x, 0] + "" + _wireStates[y, x, 1]).Join(", ")).Join("; "));
            for (int i = 0; i < lines.Count; i++)
                Module.Log("Row {0} has values: {1}. The marked position is {2}.", i + 1, lines[i].Select(x => "ABCDEFG"[x]).Join(", "), positions[i].IndexOf(marked[i]) + 1);
            Module.Log("The capillaries to cut are: {0}.", _solutionWires.Select(x => (x / 2 + 1) + ":" + "LR"[x % 2]).Join(", "));

            break;
        }
    }
}