using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ControlledChasmsModule : CorruptionModule
{
    public static int[,] Table =
    {
        {0, 0, 1, 1, 2, 0, 2, 0, 0},
        {0, 1, 2, 0, 0, 2, 2, 1, 0},
        {2, 2, 2, 0, 2, 1, 2, 2, 1},
        {0, 2, 1, 1, 1, 1, 0, 0, 1},
        {2, 0, 2, 1, -1, 1, 2, 0, 2},
        {1, 0, 0, 1, 1, 1, 1, 2, 0},
        {1, 2, 2, 1, 2, 0, 2, 2, 2},
        {0, 1, 2, 2, 0, 0, 2, 1, 0},
        {0, 0, 2, 0, 2, 1, 1, 0, 0}
    };

    private int[] _targetPoints;
    private ChasmGrid _storedPuzzle;
    private ChasmGrid _runningPuzzle;

    private List<GameObject> _eyes = new List<GameObject>();
    private List<GameObject> _pupils = new List<GameObject>();

    public ControlledChasmsModule(CorruptionScript module) : base(module)
    {
    }

    protected override IEnumerator Generate()
    {
        yield return RunThread(() => GeneratePuzzle());

        GeneratePhysical();

        Module.StartCoroutine(SummonSpike(_runningPuzzle.X + 9 * _runningPuzzle.Y));
    }

    protected override float PointHeight(float x, float z)
    {
        return Mathf.Abs(x) < 0.07f && Mathf.Abs(z) < 0.07f ? 0.005f : 0.015f;
    }

    public override string ModuleName()
    {
        return "Controlled Chasms";
    }

    private void GeneratePhysical()
    {
        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.ControlledChasmsAssets;

        main.ExposeResources();

        for (int i = 1; i < _storedPuzzle.Constraints.Count; i++)
        {
            ChasmGrid.IChasmConstraint constraint = _storedPuzzle.Constraints[i];
            if (constraint is ChasmGrid.WallConstraint)
                foreach (Vector3 position in ((ChasmGrid.WallConstraint)constraint).GetPositions())
                {
                    GameObject obj = new GameObject();
                    obj.transform.SetParent(Module.transform);
                    obj.transform.localPosition = position * 0.015f - new Vector3(0.06f, 0, -0.06f) + new Vector3(0, 0.005f, 0);
                    obj.transform.localScale = Vector3.one * 0.1875f;
                    obj.transform.localEulerAngles = RandomFloat(0, Mathf.PI / 12f) * new Vector3(180 / Mathf.PI, 0, 0) + RandomFloat(0, Mathf.PI * 2f) * new Vector3(0, 180 / Mathf.PI, 0);

                    GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
                    newPiece.transform.localPosition = Vector3.zero;
                    newPiece.transform.localScale = Vector3.one;
                    newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0) + RandomFloat(0, Mathf.PI * 2f) * new Vector3(0, 0, 180 / Mathf.PI);
                }
            else if (constraint is ChasmGrid.RadialConstraint)
            {
                GameObject obj = new GameObject();
                obj.transform.SetParent(Module.transform);
                obj.transform.localPosition = ((ChasmGrid.RadialConstraint)constraint).GetPosition() * 0.015f - new Vector3(0.06f, 0, -0.06f) + new Vector3(0, 0.006f, 0);
                obj.transform.localScale = Vector3.one * 0.25f;
                obj.transform.localEulerAngles = Vector3.zero;

                _eyes.Add(obj);

                for (int j = 3; j < 5; j++)
                {
                    GameObject newPiece = GameObject.Instantiate(assets[j], obj.transform);
                    newPiece.transform.localPosition = Vector3.zero;
                    newPiece.transform.localScale = Vector3.one;
                    newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0);

                    if (j == 4)
                        _pupils.Add(newPiece);
                }
            }
        }

        foreach (int target in _targetPoints)
        {
            GameObject obj = new GameObject();
            obj.transform.SetParent(Module.transform);
            obj.transform.localPosition = new Vector3(target % 9, 0, -target / 9) * 0.015f - new Vector3(0.06f, 0, -0.06f) + new Vector3(0, 0.006f, 0);
            obj.transform.localScale = Vector3.one * 0.375f - new Vector3(0, 0.125f, 0);
            obj.transform.localEulerAngles = Vector3.zero;

            for (int j = 3; j < 5; j++)
            {
                GameObject newPiece = GameObject.Instantiate(assets[j], obj.transform);
                newPiece.transform.localPosition = Vector3.zero;
                newPiece.transform.localScale = Vector3.one;
                newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0);
            }
        }

        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        moduleSelectable.ChildRowLength = 0;
        moduleSelectable.Children = new KMSelectable[4];

        List<Vector3> buttonPositions = new List<Vector3>() { new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, -1), };
        for (int i = 0; i < 4; i++)
        {
            GameObject obj = new GameObject();
            obj.transform.SetParent(Module.transform);
            obj.transform.localPosition = buttonPositions[i] * 0.08f + new Vector3(0, 0.0175f, 0);
            obj.transform.localScale = Vector3.one * 1.5f;
            obj.transform.localEulerAngles = new Vector3(0, 90 * (i + 1), 0);

            KMSelectable newSelectable = GameObject.Instantiate(assets[1], obj.transform).GetComponent<KMSelectable>();
            newSelectable.transform.localPosition = Vector3.zero;
            newSelectable.transform.localScale = Vector3.one;
            newSelectable.transform.localEulerAngles = new Vector3(-90, 0, 0);
            newSelectable.Parent = moduleSelectable;

            moduleSelectable.Children[i] = newSelectable;
            newSelectable.enabled = true;
            CorruptionReflectionTools.UpdateSelectable(newSelectable);
            moduleSelectable.Children[i] = newSelectable;

            int i2 = i;
            newSelectable.OnInteract += () =>
            {
                if (!newSelectable.enabled)
                    return false;

                newSelectable.AddInteractionPunch(0.5f);

                int newX = _runningPuzzle.X;
                int newY = _runningPuzzle.Y;

                bool good = false;
                switch (i2)
                {
                    case 0:
                        if (newX > 0)
                        {
                            newX--;
                            good = true;
                        }
                        break;
                    case 1:
                        if (newY > 0)
                        {
                            newY--;
                            good = true;
                        }
                        break;
                    case 2:
                        if (newX < _runningPuzzle.Width - 1)
                        {
                            newX++;
                            good = true;
                        }
                        break;
                    case 3:
                        if (newY < _runningPuzzle.Height - 1)
                        {
                            newY++;
                            good = true;
                        }
                        break;
                }

                if (good)
                {
                    int markerCount = _runningPuzzle.Constraints.First().Progress();
                    if (!_runningPuzzle.MoveTo(newX, newY))
                    {
                        Module.Log("Moving to ({0},{1}) is invalid. Strike!", newX + 1, newY + 1);
                        good = false;
                    }
                    else
                    {
                        Module.StartCoroutine(SummonSpike(newX + 9 * newY));

                        if (_runningPuzzle.Constraints.First().Progress() == 3 && newX == _storedPuzzle.X && newY == _storedPuzzle.Y)
                        {
                            foreach (KMSelectable selectable in moduleSelectable.Children)
                                selectable.enabled = false;

                            Module.Log("Succesfully returned to the starting position with all checkpoints reached.");
                            Module.Log("Module has been solved!");
                            Module.Pass();
                            return false;
                        }

                        if (_runningPuzzle.Constraints.First().Progress() > markerCount)
                        {
                            Module.Log("Successfully collected the checkpoint at ({0},{1}).", newX + 1, newY + 1);
                            return false;
                        }

                        Module.Log("Successfully moved ({0},{1}).", newX + 1, newY + 1);
                    }
                }
                else
                    Module.Log("An attempt to exit the bounds of the maze was made. Strike!");

                if (!good)
                {
                    Module.Strike();
                    Module.Log("Resetting the module");
                    _runningPuzzle = _storedPuzzle.Copy();
                    Module.StartCoroutine(SummonSpike(_runningPuzzle.X + 9 * _runningPuzzle.Y));
                }

                return false;
            };
        }

        moduleSelectable.UpdateChildren();

        main.CloseResources();

        Module.StartCoroutine(TrackPlayerLocation());
    }

    private IEnumerator TrackPlayerLocation()
    {
        while (!Module.IsSolved)
        {

            Vector3 player = new Vector3(_runningPuzzle.X, 0, -_runningPuzzle.Y) * 0.015f - new Vector3(0.06f, 0, -0.06f);

            for (int i = 0; i < _eyes.Count; i++)
            {
                _eyes[i].transform.localEulerAngles = new Vector3(0, 180 / Mathf.PI * Mathf.Atan2(_eyes[i].transform.localPosition.z - player.z, -(_eyes[i].transform.localPosition.x - player.x)), 0);
                _pupils[i].transform.localEulerAngles = new Vector3(-90 + 180 / Mathf.PI * Mathf.Atan2(new Vector3(_eyes[i].transform.localPosition.x - player.x, 0, _eyes[i].transform.localPosition.z - player.z).magnitude, 0.015f), 90, 0);
            }

            yield return null;
        }

        for (int i = 0; i < _eyes.Count; i++)
        {
            _eyes[i].transform.localEulerAngles = Vector3.zero;
            _pupils[i].transform.localEulerAngles = new Vector3(-90, 0, 0);
        }
    }

    private IEnumerator SummonSpike(int target)
    {
        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.ControlledChasmsAssets;

        main.ExposeResources();

        GameObject obj = new GameObject();
        obj.transform.SetParent(Module.transform);
        obj.transform.localPosition = new Vector3(target % 9, 0, -target / 9) * 0.015f - new Vector3(0.06f, 0, -0.06f) + new Vector3(0, 0.006f, 0);
        obj.transform.localScale = Vector3.one * 0.1875f;
        obj.transform.localEulerAngles = Vector3.zero;

        GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
        newPiece.transform.localPosition = new Vector3(0, -0.05f, 0);
        newPiece.transform.localScale = Vector3.one;
        newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0) + RandomFloat(0, Mathf.PI * 2f) * new Vector3(0, 0, 180 / Mathf.PI);

        main.CloseResources();

        Module.PlaySound("Spike", obj.transform);

        float totalTime = 0.125f;
        float currentTime = 0f;
        Vector3 intitialPosition = newPiece.transform.localPosition;
        while (currentTime < totalTime && _runningPuzzle.X + 9 * _runningPuzzle.Y == target)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;
            newPiece.transform.localPosition = Vector3.Lerp(intitialPosition, Vector3.zero, currentTime / totalTime);
        }

        yield return new WaitUntil(() => _runningPuzzle.X + 9 * _runningPuzzle.Y != target);

        while (currentTime > 0 && _runningPuzzle.X + 9 * _runningPuzzle.Y != target)
        {
            yield return null;
            currentTime -= Time.deltaTime;
            if (currentTime < 0)
                currentTime = 0;
            newPiece.transform.localPosition = Vector3.Lerp(intitialPosition, Vector3.zero, currentTime / totalTime);
        }
        GameObject.Destroy(obj);

        yield break;
    }

    private void GeneratePuzzle()
    {
        List<int> targetPoints = new List<int>();
        while (targetPoints.Count < 3)
        {
            int position = RandomInt(0, 81);
            if (Table[position / 9, position % 9] < 0)
                continue;
            if (targetPoints.Any(x => Table[x / 9, x % 9] == Table[position / 9, position % 9]))
                continue;
            targetPoints.Add(position);
        }
        targetPoints = targetPoints.OrderBy(x => Table[x / 9, x % 9]).ToList();

        Module.Log("The selected positions are: {0}.", targetPoints.Select(x => "(" + (x % 9 + 1) + "," + (x / 9 + 1) + ")").Join(", "));

        _targetPoints = targetPoints.ToArray();
        _storedPuzzle = new ChasmGrid(9, 9, 4, 4).GenerateMaze(_targetPoints, this);

        Module.Log("The maze generated with the following restrictions: {0}.", _storedPuzzle.Constraints.Skip(2).Join(", "));

        _runningPuzzle = _storedPuzzle.Copy();
    }

    private class ChasmGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        public List<IChasmConstraint> Constraints { get; private set; }

        public ChasmGrid(int width, int height, int x, int y)
        {
            Width = width;
            Height = height;
            X = x;
            Y = y;
            Constraints = new List<IChasmConstraint>();
        }

        public bool MoveTo(int x, int y)
        {
            X = x; Y = y;
            foreach (IChasmConstraint constraint in Constraints)
                if (!constraint.MoveTo(x, y))
                    return false;
            return true;
        }

        public ChasmGrid Copy()
        {
            ChasmGrid newGrid = new ChasmGrid(Width, Height, X, Y);
            newGrid.Constraints = Constraints.Select(x => x.Copy()).ToList();
            return newGrid;
        }

        public ChasmGrid GenerateMaze(int[] targets, CorruptionModule rng)
        {
            int moveCount = 0;

            ChasmGrid grid = new ChasmGrid(Width, Height, X, Y);
            grid.Constraints.Add(new MarkerConstraint(targets));
            grid.Constraints.Add(new MoveConstraint(-1));

            List<IChasmConstraint> candidateClues = new List<IChasmConstraint>();
            for (int i = 0; i < Width - 1; i++)
                for (int j = 0; j < Height; j++)
                    candidateClues.Add(new WallConstraint(i, j, true));
            for (int i = 0; i < Width; i++)
                for (int j = 0; j < Height - 1; j++)
                    candidateClues.Add(new WallConstraint(i, j, false));
            for (int i = 0; i < Width - 1; i++)
                for (int j = 0; j < Height - 1; j++)
                    candidateClues.Add(new RadialConstraint(i, j));

            Queue<List<IChasmConstraint>> clueOrder = new Queue<List<IChasmConstraint>>();
            clueOrder.Enqueue(rng.Shuffle(candidateClues));

            while (clueOrder.Count > 0)
            {
                List<IChasmConstraint> currentClues = clueOrder.Dequeue();
                ChasmGrid copy = new ChasmGrid(grid.Width, grid.Height, grid.X, grid.Y);
                copy.Constraints = grid.Constraints.Select(x => x.Copy()).ToList();
                copy.Constraints.AddRange(currentClues.Select(x => x.Copy()));
                copy.MoveTo(copy.X, copy.Y);

                Queue<ChasmGrid> statesToAnalyse = new Queue<ChasmGrid>();
                statesToAnalyse.Enqueue(copy);
                List<List<int>>[,] progressMarkers = new List<List<int>>[copy.Height, copy.Width];
                for (int i = 0; i < copy.Height; i++)
                    for (int j = 0; j < Width; j++)
                        progressMarkers[i, j] = new List<List<int>>();
                bool foundSomething = false;
                while (statesToAnalyse.Count > 0)
                {
                    ChasmGrid state = statesToAnalyse.Dequeue();

                    for (int i = 0; i < 4; i++)
                    {
                        int newX = state.X;
                        int newY = state.Y;

                        bool good = false;
                        switch (i)
                        {
                            case 0:
                                if (newX > 0)
                                {
                                    newX--;
                                    good = true;
                                }
                                break;
                            case 1:
                                if (newY > 0)
                                {
                                    newY--;
                                    good = true;
                                }
                                break;
                            case 2:
                                if (newX < state.Width - 1)
                                {
                                    newX++;
                                    good = true;
                                }
                                break;
                            case 3:
                                if (newY < state.Height - 1)
                                {
                                    newY++;
                                    good = true;
                                }
                                break;
                        }

                        if (!good)
                            continue;

                        ChasmGrid newState = new ChasmGrid(state.Width, state.Height, state.X, state.Y);
                        newState.Constraints = state.Constraints.Select(x => x.Copy()).ToList();

                        if (!newState.MoveTo(newX, newY))
                            continue;

                        List<List<int>> currentProgressMarkers = progressMarkers[newY, newX];
                        List<int> personalMarkers = newState.Constraints.Select(x => x.Progress()).ToList();
                        bool betterAnywhere = true;

                        if (personalMarkers[0] == targets.Length && newX == grid.X && newY == grid.Y)
                        {
                            moveCount = personalMarkers[1];
                            foundSomething = true;
                            break;
                        }

                        for (int j = 0; j < currentProgressMarkers.Count; j++)
                        {
                            bool oldIsSafe = false;
                            bool newIsSafe = false;
                            for (int k = 0; k < currentProgressMarkers[j].Count; k++)
                            {
                                if (currentProgressMarkers[j][k] > personalMarkers[k])
                                    oldIsSafe = true;
                                else if (personalMarkers[k] > currentProgressMarkers[j][k])
                                    newIsSafe = true;
                            }

                            if (!oldIsSafe && newIsSafe)
                            {
                                currentProgressMarkers.RemoveAt(j);
                                j--;
                            }

                            betterAnywhere &= newIsSafe;
                        }

                        if (!betterAnywhere && currentProgressMarkers.Count > 0)
                            continue;

                        progressMarkers[newY, newX].Add(personalMarkers);
                        statesToAnalyse.Enqueue(newState);
                    }

                    if (foundSomething)
                        break;
                }

                if (foundSomething)
                {
                    //Debug.Log("Adding " + currentClues.Join(", "));
                    grid.Constraints.AddRange(currentClues.Select(x => x.Copy()));
                    continue;
                }

                //Debug.Log("Cutting " + currentClues.Join(", "));

                if (currentClues.Count <= 1)
                    continue;

                clueOrder.Enqueue(currentClues.Take(currentClues.Count / 2).ToList());
                clueOrder.Enqueue(currentClues.Skip(currentClues.Count / 2).ToList());
            }

            grid.CleanMaze(targets.Length, -moveCount - 1, rng);

            grid.MoveTo(grid.X, grid.Y);

            return grid;
        }

        public void CleanMaze(int targetCount, int moveCount, CorruptionModule rng)
        {
            Constraints[1] = new MoveConstraint(moveCount);

            List<IChasmConstraint> candidateClues = Constraints.Skip(2).ToList();

            Queue<List<IChasmConstraint>> clueOrder = new Queue<List<IChasmConstraint>>();
            clueOrder.Enqueue(rng.Shuffle(candidateClues).Take((int)(candidateClues.Count * rng.RandomFloat(2 / 3f, 17 / 24f))).ToList());

            while (clueOrder.Count > 0)
            {
                List<IChasmConstraint> currentClues = clueOrder.Dequeue();
                ChasmGrid copy = new ChasmGrid(Width, Height, X, Y);
                copy.Constraints = Constraints.Where(x => !currentClues.Contains(x)).Select(x => x.Copy()).ToList();
                copy.MoveTo(copy.X, copy.Y);

                Queue<ChasmGrid> statesToAnalyse = new Queue<ChasmGrid>();
                statesToAnalyse.Enqueue(copy);
                List<List<int>>[,] progressMarkers = new List<List<int>>[copy.Height, copy.Width];
                for (int i = 0; i < copy.Height; i++)
                    for (int j = 0; j < Width; j++)
                        progressMarkers[i, j] = new List<List<int>>();
                bool foundSomething = false;
                while (statesToAnalyse.Count > 0)
                {
                    ChasmGrid state = statesToAnalyse.Dequeue();

                    for (int i = 0; i < 4; i++)
                    {
                        int newX = state.X;
                        int newY = state.Y;

                        bool good = false;
                        switch (i)
                        {
                            case 0:
                                if (newX > 0)
                                {
                                    newX--;
                                    good = true;
                                }
                                break;
                            case 1:
                                if (newY > 0)
                                {
                                    newY--;
                                    good = true;
                                }
                                break;
                            case 2:
                                if (newX < state.Width - 1)
                                {
                                    newX++;
                                    good = true;
                                }
                                break;
                            case 3:
                                if (newY < state.Height - 1)
                                {
                                    newY++;
                                    good = true;
                                }
                                break;
                        }

                        if (!good)
                            continue;

                        ChasmGrid newState = new ChasmGrid(state.Width, state.Height, state.X, state.Y);
                        newState.Constraints = state.Constraints.Select(x => x.Copy()).ToList();

                        if (!newState.MoveTo(newX, newY))
                            continue;

                        List<List<int>> currentProgressMarkers = progressMarkers[newY, newX];
                        List<int> personalMarkers = newState.Constraints.Select(x => x.Progress()).ToList();
                        bool betterAnywhere = true;

                        if (personalMarkers[0] == targetCount && newX == X && newY == Y)
                        {
                            //Debug.Log(-personalMarkers[1] + "/" + moveCount);
                            foundSomething = true;
                            break;
                        }

                        for (int j = 0; j < currentProgressMarkers.Count; j++)
                        {
                            bool oldIsSafe = false;
                            bool newIsSafe = false;
                            for (int k = 0; k < currentProgressMarkers[j].Count; k++)
                            {
                                if (currentProgressMarkers[j][k] > personalMarkers[k])
                                    oldIsSafe = true;
                                else if (personalMarkers[k] > currentProgressMarkers[j][k])
                                    newIsSafe = true;
                            }

                            if (!oldIsSafe && newIsSafe)
                            {
                                currentProgressMarkers.RemoveAt(j);
                                j--;
                            }

                            betterAnywhere &= newIsSafe;
                        }

                        if (!betterAnywhere && currentProgressMarkers.Count > 0)
                            continue;

                        progressMarkers[newY, newX].Add(personalMarkers);
                        statesToAnalyse.Enqueue(newState);
                        //Debug.Log(newState.X + "," + newState.Y + ":" + currentProgressMarkers.Select(x => x.Join("")).Join(", "));
                    }

                    if (foundSomething)
                        break;
                }

                if (foundSomething)
                {
                    //Debug.Log("Ignoring " + currentClues.Join(", "));

                    if (currentClues.Count <= 1)
                        continue;

                    clueOrder.Enqueue(currentClues.Take(currentClues.Count / 2).ToList());
                    clueOrder.Enqueue(currentClues.Skip(currentClues.Count / 2).ToList());
                    continue;
                }

                //Debug.Log("Dropping " + currentClues.Join(", "));
                Constraints.RemoveAll(x => currentClues.Contains(x));
            }

            Constraints.RemoveAt(1);
        }

        public interface IChasmConstraint
        {
            IChasmConstraint Copy();

            bool MoveTo(int x, int y);

            int Progress();
        }

        public class WallConstraint : IChasmConstraint
        {
            private int _prevPlayerX = 4, _prevPlayerY = 4;
            private int _x, _y;
            private bool _horizontal;

            public WallConstraint(int x, int y, bool horizontal)
            {
                _x = x; _y = y;
                _horizontal = horizontal;
            }

            public bool MoveTo(int x, int y)
            {
                if (!_horizontal && Math.Max(_prevPlayerY, y) > _y && Math.Min(_prevPlayerY, y) <= _y && x == _x)
                    return false;
                if (_horizontal && Math.Max(_prevPlayerX, x) > _x && Math.Min(_prevPlayerX, x) <= _x && y == _y)
                    return false;

                _prevPlayerX = x;
                _prevPlayerY = y;
                return true;
            }

            public int Progress()
            {
                return 0;
            }

            IChasmConstraint IChasmConstraint.Copy()
            {
                WallConstraint wallConstraint = new WallConstraint(_x, _y, _horizontal);
                wallConstraint._prevPlayerX = _prevPlayerX;
                wallConstraint._prevPlayerY = _prevPlayerY;

                return wallConstraint;
            }

            public override string ToString()
            {
                if (_horizontal)
                    return "Wall(" + (_x + 1) + "-" + (_x + 2) + "," + (_y + 1) + ")";
                return "Wall(" + (_x + 1) + "," + (_y + 1) + "-" + (_y + 2) + ")";
            }

            public List<Vector3> GetPositions()
            {
                return new List<Vector3>{
                    new Vector3(_x, 0, -_y) + (_horizontal ? new Vector3(0.5f, 0, 0.1875f) : new Vector3(0.1875f, 0, -0.5f)),
                    new Vector3(_x, 0, -_y) + (_horizontal ? new Vector3(0.5f, 0, -0.1875f) : new Vector3(-0.1875f, 0, -0.5f))};
            }
        }

        public class RadialConstraint : IChasmConstraint
        {
            private int _passes = 0;
            private bool _phase2 = false;
            private int _x, _y;

            public RadialConstraint(int x, int y)
            {
                _x = x; _y = y;
            }

            public bool MoveTo(int x, int y)
            {
                if (Math.Abs(x * 2 - (_x * 2 + 1)) > 1 || Math.Abs(y * 2 - (_y * 2 + 1)) > 1)
                {
                    if (_passes == 1)
                        _passes = 2;

                    if (_passes > 0)
                        _phase2 = true;

                    return true;
                }

                _passes++;

                if (_passes >= 3 && !_phase2)
                    return false;

                if (_passes >= 4)
                    return false;

                return true;
            }

            public int Progress()
            {
                return -_passes;
            }

            IChasmConstraint IChasmConstraint.Copy()
            {
                RadialConstraint radialCornerConstraint = new RadialConstraint(_x, _y);
                radialCornerConstraint._passes = _passes;
                radialCornerConstraint._phase2 = _phase2;

                return radialCornerConstraint;
            }

            public override string ToString()
            {
                return "Eye(" + (_x + 1) + "-" + (_x + 2) + "," + (_y + 1) + "-" + (_y + 2) + ")";
            }

            public Vector3 GetPosition()
            {
                return new Vector3(_x, 0, -_y) + new Vector3(0.5f, 0, -0.5f);
            }
        }

        public class MarkerConstraint : IChasmConstraint
        {
            private int[] _positions;
            private int _currentIndex;

            public MarkerConstraint(int[] positions)
            {
                _positions = positions;
            }

            public bool MoveTo(int x, int y)
            {
                if (!_positions.Contains(x + 9 * y))
                    return true;

                if (_currentIndex >= _positions.Length)
                    return false;

                if (_positions[_currentIndex] == x + 9 * y)
                {
                    _currentIndex++;
                    return true;
                }

                return false;
            }

            public int Progress()
            {
                return _currentIndex;
            }

            IChasmConstraint IChasmConstraint.Copy()
            {
                MarkerConstraint markerConstraint = new MarkerConstraint(_positions);
                markerConstraint._currentIndex = _currentIndex;

                return markerConstraint;
            }

            public override string ToString()
            {
                return "-";
            }
        }

        public class MoveConstraint : IChasmConstraint
        {
            private int _moveCount = 0;
            private int _maxMoves;

            public MoveConstraint(int maxMoves)
            {
                _maxMoves = maxMoves;
            }

            public bool MoveTo(int x, int y)
            {
                if (_moveCount == _maxMoves)
                    return false;

                _moveCount++;

                return true;
            }

            public int Progress()
            {
                return -_moveCount;
            }

            IChasmConstraint IChasmConstraint.Copy()
            {
                MoveConstraint moveConstraint = new MoveConstraint(_maxMoves);
                moveConstraint._moveCount = _moveCount;

                return moveConstraint;
            }

            public override string ToString()
            {
                return "Move";
            }
        }
    }
}
