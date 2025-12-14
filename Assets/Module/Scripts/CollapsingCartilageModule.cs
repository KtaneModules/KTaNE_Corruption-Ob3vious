using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollapsingCartilageModule : CorruptionModule
{
    public static int[][] Codes = {
        new int[] {13, 15, 1, 8, 12, 3, 16},
        new int[] {11, 6, 0, 14, 0, 10, 7},
        new int[] {0, 0, 5, 5, 10, 0, 2},
        new int[] {6, 2, 7, 12, 7, 3, 9},
        new int[] {1, 3, 13, 13, 0, 12, 8},
        new int[] {1, 0, 13, 0, 4, 0, 3}
    };

    private CartilageChain[] _puzzleChains;
    private int[] _solutionStarts;
    private int[] _solutionLengths;
    private int _solutionIndex;

    private List<int>[] _madeCuts;
    private List<KMSelectable>[] _cutSelectables;
    private List<GameObject>[] _cartilageParts;

    private float _cliffAngle;
    private float _cliffWidth;

    public CollapsingCartilageModule(CorruptionScript module) : base(module)
    {
    }

    protected override IEnumerator Generate()
    {
        int attemptCounter = 1;
        yield return RunThread(() => { while (!GeneratePuzzle()) attemptCounter++; });

        GeneratePhysical();

        yield break;
    }

    protected override float PointHeight(float x, float z)
    {
        float xInfluence = Mathf.Cos(_cliffAngle);
        float zInfluence = Mathf.Sin(_cliffAngle);

        return Mathf.Abs(x * xInfluence + z * zInfluence) > _cliffWidth / 2f ? 0.015f : 0f;
    }

    public override string ModuleName()
    {
        return "Collapsing Cartilage";
    }

    private void GeneratePhysical()
    {
        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.CollapsingCartilageAssets;

        main.ExposeResources();

        List<GameObject> physicalChains = new List<GameObject>();

        _cartilageParts = new List<GameObject>[_puzzleChains.Length];

        for (int i = 0; i < _puzzleChains.Length; i++)
        {
            GameObject obj = new GameObject();
            obj.transform.SetParent(Module.transform);
            obj.transform.localScale = Vector3.one / 2.5f;
            physicalChains.Add(obj);

            //put in a null for substitution later
            _cartilageParts[i] = new List<GameObject> { null };

            for (int j = 0; j < _puzzleChains[i].Values.Length; j++)
            {
                GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
                newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0);

                int ringValue = _puzzleChains[i].Values[j];

                GameObject newRing = GameObject.Instantiate(assets[CartilageChain.PhysicalParts[ringValue]], newPiece.transform);
                newRing.transform.localEulerAngles = new Vector3(90 * (CartilageChain.PhysicalRotations[ringValue] % 4), 180 * (CartilageChain.PhysicalRotations[ringValue] / 4), 0);

                _cartilageParts[i].Add(newPiece);
            }

            //extra parts on the sides
            for (int j = 0; j < 2; j++)
            {
                GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
                newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0);

                _cartilageParts[i].Add(newPiece);
            }
            _cartilageParts[i][0] = _cartilageParts[i].Last();
            _cartilageParts[i].RemoveAt(_cartilageParts[i].Count - 1);
        }

        //now for selectable stuff
        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        moduleSelectable.ChildRowLength = _puzzleChains.Length;
        moduleSelectable.Children = new KMSelectable[_puzzleChains.Length * (_puzzleChains.Max(x => x.Values.Length) + 1)];

        _madeCuts = new List<int>[_puzzleChains.Length];
        _cutSelectables = new List<KMSelectable>[_puzzleChains.Length];

        for (int i = 0; i < _puzzleChains.Length; i++)
        {
            _madeCuts[i] = new List<int>();
            _cutSelectables[i] = new List<KMSelectable>();
            for (int j = 0; j < _puzzleChains[i].Values.Length + 1; j++)
            {
                int i2 = i;
                int j2 = j;
                KMSelectable newSelectable = GameObject.Instantiate(assets[1], physicalChains[i].transform).GetComponent<KMSelectable>();
                newSelectable.Parent = moduleSelectable;

                _cutSelectables[i].Add(newSelectable);
                newSelectable.enabled = true;
                CorruptionReflectionTools.UpdateSelectable(newSelectable);
                moduleSelectable.Children[j * _cutSelectables.Length + i] = newSelectable;

                newSelectable.OnInteract += () =>
                {
                    OnCut(i2, j2);
                    newSelectable.AddInteractionPunch(0.5f);

                    return false;
                };
            }
        }
        moduleSelectable.UpdateChildren();

        main.CloseResources();

        Realign();

        _cliffAngle = RandomFloat(0f, Mathf.PI * 2f);
        float chainAngle = RandomFloat(0f, Mathf.PI / 12f);
        _cliffWidth = (_cartilageParts[0].Count - 1) * 0.035f * physicalChains[0].transform.localScale.x * Mathf.Cos(chainAngle);
        for (int i = 0; i < physicalChains.Count; i++)
        {
            int flipChainAngle = RandomInt(0, 2) * 2 - 1;
            physicalChains[i].transform.localPosition = (1 / 30f) * (1 - i) * new Vector3(-Mathf.Sin(_cliffAngle), 0f, Mathf.Cos(_cliffAngle)) + new Vector3(0f, 0.015f / 2f, 0f);
            physicalChains[i].transform.localEulerAngles = (_cliffAngle + chainAngle * flipChainAngle) * new Vector3(0f, -180 / Mathf.PI, 0f);
        }
    }

    private void OnCut(int chain, int position)
    {
        if (!_cutSelectables[chain][position].enabled)
            return;

        Module.Log("Made a cut on chain {0} {1}.", chain + 1, position == 0 ? "before position 1" : (position == _cutSelectables[chain].Count - 1 ? "after position " + (_cutSelectables[chain].Count - 1) : ("between positions " + position + " and " + (position + 1))));

        Module.PlaySound("Crunch", _cutSelectables[chain][position].transform);

        _madeCuts[chain].Add(position);
        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        if (_madeCuts[chain].Count == 1)
        {


            _cutSelectables[chain][position].enabled = false;
            moduleSelectable.Children[position * _cutSelectables.Length + chain] = null;
            moduleSelectable.UpdateChildren();
            Realign();
            return;
        }

        Module.Log("Selected the portion {0} on chain {1}.", (_madeCuts[chain].Min() + 1) + (_madeCuts[chain].Max() - _madeCuts[chain].Min() > 1 ? "-" + _madeCuts[chain].Max() : ""), chain + 1);

        for (int i = 0; i < _cutSelectables[chain].Count; i++)
        {
            _cutSelectables[chain][i].enabled = false;
            moduleSelectable.Children[i * _cutSelectables.Length + chain] = null;
        }

        if (_madeCuts.Any(x => x.Count < 2))
        {


            moduleSelectable.UpdateChildren();
            Realign();
            return;
        }

        bool correct = true;
        for (int i = 0; i < _madeCuts.Length; i++)
        {
            if (!_madeCuts[i].Contains(_solutionStarts[i]) || Math.Abs(_madeCuts[i][0] - _madeCuts[i][1]) != _solutionLengths[i])
            {
                correct = false;
                break;
            }
        }

        if (correct)
        {
            Module.Log("Module has been solved!");
            Module.Pass();
            Realign();
            return;
        }

        for (int i = 0; i < _cutSelectables.Length; i++)
        {
            _madeCuts[i].Clear();
            for (int j = 0; j < _cutSelectables[i].Count; j++)
            {
                _cutSelectables[i][j].enabled = true;
                moduleSelectable.Children[j * _cutSelectables.Length + i] = _cutSelectables[i][j];
            }
        }
        moduleSelectable.UpdateChildren();
        Realign();

        Module.Log("Made incorrect cuts. Strike!");
        Module.Strike();
    }

    private void Realign()
    {
        for (int i = 0; i < _cartilageParts.Length; i++)
        {
            int cutsPassed = 0;
            int cutCount = _madeCuts[i].Count;
            for (int j = 0; j < _cartilageParts[i].Count; j++)
            {
                _cartilageParts[i][j].transform.localPosition = new Vector3(0.035f * (j - (_cartilageParts[i].Count - 1) / 2f) + 0.01f * (cutsPassed - cutCount / 2f), 0, 0);
                if (j != 0)
                    _cutSelectables[i][j - 1].transform.localPosition = new Vector3(0.035f * (j - _cartilageParts[i].Count / 2f) + 0.01f * (cutsPassed - cutCount / 2f), 0, 0);

                if (_madeCuts[i].Contains(j))
                    cutsPassed++;
            }
        }
    }

    private bool GeneratePuzzle()
    {
        int[] chainLengths = new int[] { 7, 7, 7 };

        _solutionIndex = RandomInt(0, Codes.Length);

        //placement of the segments for each chain
        int[] solutionOrder = Shuffle(Enumerable.Range(0, chainLengths.Length).ToList()).ToArray();
        //partitionings in the answer chain order
        int[] solutionCuts = chainLengths.Select(x => 1).ToArray();
        while (solutionCuts.Sum() < Codes[_solutionIndex].Length)
        {
            int pos = RandomInt(0, solutionCuts.Length);
            if (solutionCuts[pos] < chainLengths[solutionOrder[pos]])
                solutionCuts[pos]++;
        }
        //starting positions, associated with cuts not chains
        int[] startingPositions = new int[solutionCuts.Length];
        for (int i = 0; i < startingPositions.Length; i++)
            startingPositions[i] = RandomInt(0, chainLengths[solutionOrder[i]] - solutionCuts[i]);

        List<int[]> initialChains = new List<int[]>();
        for (int i = 0; i < chainLengths.Length; i++)
            initialChains.Add(Enumerable.Repeat(-1, chainLengths[i]).ToArray());

        for (int i = 0; i < solutionOrder.Length; i++)
            for (int j = 0; j < solutionCuts[i]; j++)
                initialChains[solutionOrder[i]][startingPositions[i] + j] = Codes[_solutionIndex][solutionCuts.Take(i).Sum() + j];

        while (initialChains.Any(x => x.Any(y => y < 0)))
        {
            int pos = RandomInt(0, solutionCuts.Length);
            if (!initialChains[pos].Any(x => x < 0))
                continue;

            List<int> indices = new List<int>();
            for (int i = 0; i < initialChains[pos].Length; i++)
            {
                if (initialChains[pos][i] >= 0)
                    continue;

                if ((i > 0 && initialChains[pos][i - 1] >= 0)
                    || (i < initialChains[pos].Length - 1 && initialChains[pos][i + 1] >= 0))
                    indices.Add(i);
            }

            int chosenIndex = PickRandom(indices);

            Queue<int> possibleValues = new Queue<int>(Shuffle(Enumerable.Range(0, 17).ToList()));
            bool success = false;
            while (possibleValues.Count > 0)
            {
                int[][] newChains = initialChains.Select(x => x.Select(y => y).ToArray()).ToArray();
                int newValue = possibleValues.Dequeue();
                newChains[pos][chosenIndex] = newValue;


                if (FindSolution(chosenIndex, new CartilageChain[] { new CartilageChain(newChains[pos]) }
                .Concat(newChains.Skip(pos + 1).Select(x => new CartilageChain(x)))
                .Concat(newChains.Take(pos).Select(x => new CartilageChain(x))).ToArray()))
                    continue;

                success = true;
                initialChains[pos][chosenIndex] = newValue;
                break;
            }

            if (!success)
                return false;
        }

        _puzzleChains = initialChains.Select(x => new CartilageChain(x)).ToArray();
        _solutionStarts = new int[_puzzleChains.Length];
        _solutionLengths = new int[_puzzleChains.Length];
        for (int i = 0; i < _puzzleChains.Length; i++)
        {
            int chainIndex = solutionOrder[i];
            _solutionStarts[chainIndex] = startingPositions[i];
            _solutionLengths[chainIndex] = solutionCuts[i];
        }

        for (int i = 0; i < _puzzleChains.Length; i++)
        {
            bool flip = RandomInt(0, 2) >= 1;
            if (flip)
            {
                _solutionStarts[i] = chainLengths[i] - _solutionStarts[i] - _solutionLengths[i];
                _puzzleChains[i].Flip();
            }
            _puzzleChains[i].Rotate(RandomInt(0, 4));
        }

        Module.Log("The selected code is: {0}.", new CartilageChain(Codes[_solutionIndex]));
        Module.Log("The chains created are: {0}.", _puzzleChains.Join(", "));
        Module.Log("Select the following positions on the chains: {0}.", Enumerable.Range(0, _solutionStarts.Length).Select(x => (_solutionStarts[x] + 1) + (_solutionLengths[x] > 1 ? "-" + (_solutionStarts[x] + _solutionLengths[x]) : "")).Join(", "));

        return true;
    }

    private bool FindSolution(int includeIndexFromFirst, CartilageChain[] chains)
    {
        int selectionLength = Codes[0].Length;

        if (!(includeIndexFromFirst == 0 || chains[0].Values[includeIndexFromFirst - 1] < 0))
        {
            chains[0].Flip();
            includeIndexFromFirst = chains[0].Values.Length - 1 - includeIndexFromFirst;
        }

        //the first layer of hell
        Stack<int> selectionSeparators = new Stack<int>();
        selectionSeparators.Push(0);

        while (true)
        {
            if (selectionSeparators.Count < chains.Length)
                selectionSeparators.Push(selectionSeparators.Peek());

            int latestSeparator;
            do
                latestSeparator = selectionSeparators.Pop();
            while (latestSeparator >= selectionLength - 1 && selectionSeparators.Count > 0);

            if (selectionSeparators.Count <= 0)
                break;

            selectionSeparators.Push(latestSeparator + 1);

            if (selectionSeparators.Count < chains.Length)
                continue;

            //the second layer of hell
            Stack<int> selectionIndices = new Stack<int>();
            selectionIndices.Push(includeIndexFromFirst);

            while (selectionIndices.Count >= 1 && selectionIndices.Last() == includeIndexFromFirst)
            {
                int latestIndex = selectionIndices.Peek();
                int lastPosition = latestIndex + (selectionIndices.Count == chains.Length ? selectionLength : selectionSeparators.ElementAt(chains.Length - selectionIndices.Count - 1)) - selectionSeparators.ElementAt(chains.Length - selectionIndices.Count) - 1;
                if (lastPosition < chains[selectionIndices.Count - 1].Values.Length && //this specifically to jump to overshot
                    chains[selectionIndices.Count - 1].Values[latestIndex] < 0 && chains[selectionIndices.Count - 1].Values.Skip(latestIndex).Any(x => x >= 0))
                {
                    selectionIndices.Push(selectionIndices.Pop() + 1);
                    continue;
                }
                if (lastPosition >= chains[selectionIndices.Count - 1].Values.Length ||
                    chains[selectionIndices.Count - 1].Values[lastPosition] < 0)
                {
                    selectionIndices.Pop();
                    if (selectionIndices.Count == 0)
                        break;
                    selectionIndices.Push(selectionIndices.Pop() + 1);
                    continue;
                }
                if (selectionIndices.Count < chains.Length)
                {
                    selectionIndices.Push(0);
                    continue;
                }

                //the third layer of hell
                foreach (int[] code in Codes)
                {
                    //the fourth layer of hell
                    Stack<int> order = new Stack<int>();

                    while (true)
                    {
                        int startingValue = 0;
                        if (order.Count == chains.Length)
                            startingValue = order.Pop() + 1;

                        while (order.All(x => x < chains.Length) && order.Count < chains.Length)
                        {
                            bool success = false;
                            for (int i = startingValue; i < chains.Length; i++)
                                if (!order.Contains(i))
                                {
                                    success = true;
                                    order.Push(i);
                                    break;
                                }

                            if (success)
                            {
                                startingValue = 0;
                                continue;
                            }

                            if (order.Count == 0)
                                break;

                            startingValue = order.Pop() + 1;
                        }
                        if (order.Count == 0)
                            break;

                        //the fifth layer of hell (the last one I swear)
                        Stack<int> orientation = new Stack<int>();
                        Stack<CartilageChain> cutChains = new Stack<CartilageChain>();

                        while (true)
                        {
                            if (orientation.Any() && orientation.Peek() > 7)
                            {
                                orientation.Pop();
                                if (orientation.Count == 0)
                                    break;

                                cutChains.Peek().Rotate(1);
                                if (orientation.Peek() == 3)
                                    cutChains.Peek().Flip();
                                orientation.Push(orientation.Pop() + 1);
                                continue;
                            }

                            if (cutChains.Count > 0)
                            {
                                bool broken = false;
                                int skip = cutChains.Skip(1).Sum(x => x.Values.Length);
                                for (int i = 0; i < cutChains.Peek().Values.Length; i++)
                                    if (cutChains.Peek().Values[i] != code[i + skip])
                                    {
                                        broken = true;
                                        break;
                                    }

                                if (broken)
                                {
                                    cutChains.Peek().Rotate(1);
                                    if (orientation.Peek() == 3)
                                        cutChains.Peek().Flip();
                                    orientation.Push(orientation.Pop() + 1);
                                    continue;
                                }
                            }

                            if (orientation.Count < chains.Length)
                            {
                                int arrayIndex = order.ElementAt(chains.Length - 1 - cutChains.Count);
                                cutChains.Push(new CartilageChain(chains[arrayIndex].Values
                                    .Skip(selectionIndices.ElementAt(chains.Length - 1 - arrayIndex))
                                    .Take((arrayIndex >= chains.Length - 1 ? selectionLength : selectionSeparators.ElementAt(chains.Length - 2 - arrayIndex))
                                    - selectionSeparators.ElementAt(chains.Length - 1 - arrayIndex))
                                    .ToArray()));
                                orientation.Push(0);
                                continue;
                            }

                            return true;
                        }
                    }
                }

                //increment only after loop
                selectionIndices.Push(selectionIndices.Pop() + 1);
            }

        }

        return false;
    }

    private class CartilageChain
    {
        public static string[] Labels = { "| ", "( ", "\\ ", ") ", "/ ", "|(", "|\\", "|)", "|/", ")|", "/|", "(|", "\\|", ")(", "/\\", "()", "\\/" };
        //rotating up
        public static int[] Rotations = { 0, 2, 3, 4, 1, 6, 7, 8, 5, 10, 11, 12, 9, 14, 15, 16, 13 };
        public static int[] Flips = { 0, 3, 2, 1, 4, 9, 12, 11, 10, 5, 8, 7, 6, 13, 16, 15, 14 };

        public static int[] PhysicalParts = { 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5 };
        public static int[] PhysicalRotations = { 0, 2, 3, 0, 1, 6, 5, 4, 7, 0, 1, 2, 3, 0, 1, 2, 3 };

        public int[] Values { get; private set; }

        public CartilageChain(int[] values)
        {
            Values = values;
        }

        public void Rotate(int amount)
        {
            for (int i = 0; i < Values.Length; i++)
                for (int j = 0; j < amount % 4; j++)
                    if (Values[i] >= 0)
                        Values[i] = Rotations[Values[i]];
        }

        public void Flip()
        {
            for (int i = 0; i < Values.Length / 2; i++)
            {
                Values[i] ^= Values[Values.Length - i - 1];
                Values[Values.Length - i - 1] ^= Values[i];
                Values[i] ^= Values[Values.Length - i - 1];
            }
            for (int i = 0; i < Values.Length; i++)
                if (Values[i] >= 0)
                    Values[i] = Flips[Values[i]];
        }

        public override string ToString()
        {
            return Values.Select(x => x < 0 ? "??" : Labels[x]).Join(" = ");
        }
    }
}