using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConnectedCystsModule : CorruptionModule
{
    public static Graph[] Graphs =
    {
        new Graph(0, 1, 1, 2, 2, 3, 3, 4, 4, 5),
        new Graph(0, 1, 1, 2, 2, 3, 3, 4, 3, 5),
        new Graph(0, 1, 1, 2, 2, 3, 2, 4, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 3, 4, 3, 5),
        new Graph(0, 1, 1, 2, 2, 3, 2, 4, 2, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 0, 5),

        new Graph(0, 1, 0, 5, 1, 2, 2, 3, 3, 4, 4, 5),
        new Graph(0, 1, 0, 2, 0, 5, 2, 3, 3, 4, 4, 5),
        new Graph(0, 1, 0, 2, 1, 3, 2, 3, 3, 4, 4, 5),
        new Graph(0, 1, 1, 2, 1, 3, 2, 4, 3, 4, 4, 5),
        new Graph(0, 1, 1, 2, 1, 3, 2, 4, 2, 5, 3, 4),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 3, 5, 4, 5),

        new Graph(0, 1, 1, 2, 2, 3, 3, 4, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 3, 4, 3, 5, 4, 5),
        new Graph(0, 1, 1, 2, 1, 3, 2, 3, 3, 4, 4, 5),
        new Graph(0, 1, 0, 2, 0, 4, 0, 5, 2, 3, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 1, 2, 1, 4, 2, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 3, 4, 3, 5),

        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 1, 2),
        new Graph(0, 1, 0, 2, 1, 3, 1, 5, 2, 4, 2, 5, 3, 4),
        new Graph(0, 1, 0, 3, 0, 5, 1, 2, 2, 3, 3, 4, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 2, 4, 2, 5, 3, 4, 3, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 2, 5, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 1, 2, 1, 5, 2, 3, 3, 4, 4, 5),

        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 2, 4, 2, 5, 3, 4, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 2, 3, 2, 4, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 2, 3, 2, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 1, 2, 3, 4, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 3, 4, 4, 5),

        new Graph(0, 1, 1, 2, 2, 3, 2, 4, 3, 4, 3, 5, 4, 5),
        new Graph(0, 1, 1, 2, 2, 3, 2, 4, 2, 5, 3, 4, 3, 5),
        new Graph(0, 1, 1, 2, 1, 3, 2, 3, 2, 4, 3, 4, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 1, 2, 3, 4),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 3, 4, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 1, 3, 2, 5),

        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 2, 3, 2, 4, 2, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 1, 2, 1, 3),
        new Graph(0, 1, 0, 2, 0, 3, 1, 4, 1, 5, 2, 4, 2, 5, 3, 4),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 5, 2, 5, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 4, 1, 2, 1, 5, 2, 3, 3, 4, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 1, 5, 3, 5, 4, 5),

        new Graph(0, 1, 0, 2, 1, 2, 1, 3, 2, 4, 3, 4, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 1, 2, 1, 5, 2, 3, 3, 4, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 1, 5, 3, 4, 3, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 2, 3, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 1, 2, 1, 3, 1, 4, 2, 3, 2, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 1, 2, 1, 4, 2, 3, 3, 4, 4, 5),

        new Graph(0, 1, 0, 2, 0, 3, 1, 2, 1, 4, 1, 5, 2, 3, 3, 4),
        new Graph(0, 1, 0, 2, 0, 3, 1, 2, 1, 3, 1, 4, 2, 5, 3, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 3, 4, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 1, 2, 3, 4, 3, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 1, 5, 2, 3, 3, 4),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 2, 3, 2, 5, 3, 4),

        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 1, 2, 2, 3, 3, 4),
        new Graph(0, 1, 0, 2, 0, 3, 2, 3, 2, 4, 2, 5, 3, 4, 3, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 2, 5, 3, 5, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 1, 3, 2, 3, 4, 5),
        new Graph(0, 1, 0, 2, 0, 3, 1, 2, 1, 3, 1, 4, 2, 3, 2, 5),
        new Graph(0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 1, 2, 1, 3, 2, 3)
    };

    public static int[][] Permutations =
    {
        new int[] {0, 1, 2, 3, 4},
        new int[] {0, 4, 2, 1, 3},
        new int[] {1, 3, 4, 2, 0},
        new int[] {2, 3, 0, 1, 4},
        new int[] {3, 1, 2, 4, 0},
        new int[] {4, 0, 3, 1, 2},

        new int[] {0, 1, 3, 4, 2},
        new int[] {0, 4, 3, 2, 1},
        new int[] {1, 4, 0, 2, 3},
        new int[] {2, 3, 1, 4, 0},
        new int[] {3, 1, 4, 0, 2},
        new int[] {4, 1, 0, 3, 2},

        new int[] {0, 1, 4, 2, 3},
        new int[] {1, 0, 2, 4, 3},
        new int[] {1, 4, 2, 3, 0},
        new int[] {2, 3, 4, 0, 1},
        new int[] {3, 2, 0, 4, 1},
        new int[] {4, 1, 2, 0, 3},

        new int[] {0, 2, 1, 4, 3},
        new int[] {1, 0, 3, 2, 4},
        new int[] {1, 4, 3, 0, 2},
        new int[] {2, 4, 0, 3, 1},
        new int[] {3, 2, 1, 0, 4},
        new int[] {4, 1, 3, 2, 0},

        new int[] {0, 2, 3, 1, 4},
        new int[] {1, 0, 4, 3, 2},
        new int[] {2, 0, 1, 3, 4},
        new int[] {2, 4, 1, 0, 3},
        new int[] {3, 2, 4, 1, 0},
        new int[] {4, 2, 0, 1, 3},

        new int[] {0, 2, 4, 3, 1},
        new int[] {1, 2, 0, 3, 4},
        new int[] {2, 0, 3, 4, 1},
        new int[] {2, 4, 3, 1, 0},
        new int[] {3, 4, 0, 1, 2},
        new int[] {4, 2, 1, 3, 0},

        new int[] {0, 3, 1, 2, 4},
        new int[] {1, 2, 3, 4, 0},
        new int[] {2, 0, 4, 1, 3},
        new int[] {3, 0, 1, 4, 2},
        new int[] {3, 4, 1, 2, 0},
        new int[] {4, 2, 3, 0, 1},

        new int[] {0, 3, 2, 4, 1},
        new int[] {1, 2, 4, 0, 3},
        new int[] {2, 1, 0, 4, 3},
        new int[] {3, 0, 2, 1, 4},
        new int[] {3, 4, 2, 0, 1},
        new int[] {4, 3, 0, 2, 1},

        new int[] {0, 3, 4, 1, 2},
        new int[] {1, 3, 2, 0, 4},
        new int[] {2, 1, 3, 0, 4},
        new int[] {3, 0, 4, 2, 1},
        new int[] {4, 0, 1, 2, 3},
        new int[] {4, 3, 1, 0, 2},

        new int[] {0, 4, 1, 3, 2},
        new int[] {1, 3, 0, 4, 2},
        new int[] {2, 1, 4, 3, 0},
        new int[] {3, 1, 0, 2, 4},
        new int[] {4, 0, 2, 3, 1},
        new int[] {4, 3, 2, 1, 0}
    };

    public static int[][] Columns =
    {
        new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8},
        new int[] {3, 1, 4, 8, 9, 6, 5, 0, 2},
        new int[] {6, 3, 5, 9, 0, 1, 4, 7, 2},
        new int[] {7, 5, 2, 4, 8, 9, 0, 3, 6},
        new int[] {4, 8, 5, 7, 9, 3, 2, 6, 0}
    };

    public static bool[][] SymbolComponents =
    {
        new bool[] {true, true, false, false, true, true, false },
        new bool[] {true, true, true, false, false, true, false },
        new bool[] {true, true, false, false, false, true, true },
        new bool[] {true, true, true, true, false, false, false },
        new bool[] {true, true, false, false, true, false, true },
        new bool[] {true, true, true, false, true, false, false },
        new bool[] {true, false, true, false, false, true, true },
        new bool[] {true, false, true, true, false, true, false },
        new bool[] {true, true, false, true, false, false, true },
        new bool[] {true, false, true, true, false, false, true }
    };

    private int _graph;
    private int[] _symbols;
    private int[] _solution;

    private int _popCount;
    private KMSelectable[] _cystSelectables;
    private GameObject[] _cysts;
    private List<GameObject>[] _symbolPieces;

    public ConnectedCystsModule(CorruptionScript module) : base(module)
    {
    }

    protected override IEnumerator Generate()
    {
        GeneratePuzzle();

        int attemptCounter = 1;
        while (!GeneratePhysical()) attemptCounter++;

        yield break;
    }

    protected override float PointHeight(float x, float z)
    {
        return 0.015f;
    }

    public override string ModuleName()
    {
        return "Connected Cysts";
    }

    private bool GeneratePhysical()
    {
        float cystDistance = 0.0375f;
        float pathDistance = 0.01875f;
        List<Vector3> points = new List<Vector3>();
        while (points.Count < _symbols.Length)
        {
            Vector3 newPoint = new Vector3(RandomFloat(-0.1f + cystDistance, 0.1f - cystDistance), 0.015f, RandomFloat(-0.1f + cystDistance, 0.1f - cystDistance));
            if (points.Any(x => (x - newPoint).sqrMagnitude < cystDistance * cystDistance))
                continue;
            points.Add(newPoint);
        }

        foreach (GraphLink link in Graphs[_graph].Links)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (link.A == i || link.B == i)
                    continue;

                Vector3 ac = points[i] - points[link.A];
                Vector3 ab = points[link.B] - points[link.A];
                Vector3 projection = Vector3.Project(ac, ab) + points[link.A];
                Vector3 ad = projection - points[link.A];
                float k = ab.x * ab.x > ab.y * ab.y ? ad.x / ab.x : ad.y / ab.y;
                if (k < 0 || k > 1)
                    continue;
                if ((projection - points[i]).sqrMagnitude < pathDistance * pathDistance)
                    return false;
            }
        }

        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.ConnectedCystsAssets;

        main.ExposeResources();

        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        moduleSelectable.ChildRowLength = 0;
        moduleSelectable.Children = new KMSelectable[_symbols.Length];


        _cystSelectables = new KMSelectable[_symbols.Length];
        _cysts = new GameObject[_symbols.Length];
        _symbolPieces = new List<GameObject>[_symbols.Length];

        for (int i = 0; i < points.Count; i++)
        {
            GameObject obj = new GameObject();
            obj.transform.SetParent(Module.transform);
            obj.transform.localPosition = points[i];
            obj.transform.localScale = Vector3.one;
            obj.transform.localEulerAngles = RandomFloat(0, Mathf.PI * 2f) * new Vector3(0, 180 / Mathf.PI, 0);

            GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
            newPiece.transform.localPosition = Vector3.zero;
            newPiece.transform.localScale = Vector3.one;
            newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0);
            _cysts[i] = newPiece;

            _symbolPieces[i] = new List<GameObject>();
            for (int j = 0; j < SymbolComponents[_symbols[i]].Length; j++)
            {
                if (!SymbolComponents[_symbols[i]][j])
                    continue;

                GameObject segment = GameObject.Instantiate(assets[j + 3], obj.transform);
                segment.transform.localPosition = Vector3.zero;
                segment.transform.localScale = Vector3.one;
                segment.transform.localEulerAngles = new Vector3(-90, 0, 0);
                _symbolPieces[i].Add(segment);
            }

            KMSelectable newSelectable = GameObject.Instantiate(assets[1], obj.transform).GetComponent<KMSelectable>();
            newSelectable.transform.localPosition = Vector3.zero;
            newSelectable.transform.localScale = Vector3.one;
            newSelectable.transform.localEulerAngles = new Vector3(-90, 0, 0);
            newSelectable.Parent = moduleSelectable;

            _cystSelectables[i] = newSelectable;
            newSelectable.enabled = true;
            CorruptionReflectionTools.UpdateSelectable(newSelectable);
            moduleSelectable.Children[i] = newSelectable;

            int i2 = i;
            newSelectable.OnInteract += () =>
            {
                OnPop(i2);
                newSelectable.AddInteractionPunch(0.5f);

                return false;
            };
        }
        moduleSelectable.UpdateChildren();

        float cystRadius = 0.01f;
        float borderPieceRadius = 0.0025f;
        foreach (Vector3 point in points)
        {
            float initAngle = RandomFloat(0, Mathf.PI * 2f);
            for (int j = 0; j < Mathf.CeilToInt(cystRadius * 2f * Mathf.PI / borderPieceRadius); j++)
            {
                float angle = initAngle + (float)j / Mathf.CeilToInt(cystRadius * 2f * Mathf.PI / borderPieceRadius) * Mathf.PI * 2;
                float excessRadius = RandomFloat(0f, borderPieceRadius);

                GameObject borderPiece = GameObject.Instantiate(assets[2], Module.transform);
                borderPiece.transform.localPosition = point + (cystRadius + RandomFloat(-1f, 1f) * excessRadius) * new Vector3(Mathf.Cos(angle), 0.015f, Mathf.Sin(angle));
                borderPiece.transform.localScale = Vector3.one * (1 + excessRadius / borderPieceRadius);
                borderPiece.transform.localEulerAngles = Vector3.zero;
            }
        }
        foreach (GraphLink link in Graphs[_graph].Links)
        {
            for (int j = 0; j <= Mathf.CeilToInt(((points[link.A] - points[link.B]).magnitude - 2 * cystRadius) / borderPieceRadius); j++)
            {
                Vector3 point = (float)j / Mathf.CeilToInt(((points[link.A] - points[link.B]).magnitude - 2 * cystRadius) / borderPieceRadius) * (points[link.B] - points[link.A] - 2 * (points[link.B] - points[link.A]).normalized * cystRadius) + (points[link.B] - points[link.A]).normalized * cystRadius + points[link.A];
                float excessRadius = RandomFloat(0f, borderPieceRadius);
                Vector3 normal = new Vector3((points[link.B] - points[link.A]).z, (points[link.B] - points[link.A]).y, (points[link.B] - points[link.A]).x).normalized;

                GameObject borderPiece = GameObject.Instantiate(assets[2], Module.transform);
                borderPiece.transform.localPosition = point + RandomFloat(-1f, 1f) * excessRadius * normal;
                borderPiece.transform.localScale = Vector3.one * (1 + excessRadius / borderPieceRadius);
                borderPiece.transform.localEulerAngles = Vector3.zero;
            }
        }

        main.CloseResources();

        return true;
    }

    private void OnPop(int position)
    {
        if (!_cystSelectables[position].enabled)
            return;

        if (position != _solution[_popCount])
        {
            Module.Log("Attempted to pop {0}, when expected was {1}. Strike!", _symbols[position], _symbols[_solution[_popCount]]);
            Module.Strike();
            return;
        }

        Module.Log("Popped {0}.", _symbols[position]);

        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        _popCount++;
        _cystSelectables[position].enabled = false;
        moduleSelectable.Children[position] = null;
        moduleSelectable.UpdateChildren();

        Module.StartCoroutine(DeflateCyst(position));

        Module.PlaySound("Deflate", _cystSelectables[position].transform);

        if (_popCount < 6)
            return;

        Module.Log("Module has been solved!");
        Module.Pass();
    }

    private IEnumerator DeflateCyst(int cyst)
    {
        float totalTime = 0.5f;
        float amount = 0.5f;
        float riseAmount = 0.00125f;
        float currentTime = 0f;
        while (currentTime < totalTime)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;

            _cysts[cyst].transform.localScale = new Vector3(1f, 1f, 1f - amount * currentTime / totalTime);
            _cysts[cyst].transform.localPosition = new Vector3(0f, riseAmount * currentTime / totalTime, 0f);
            foreach (GameObject obj in _symbolPieces[cyst])
                obj.transform.localPosition = new Vector3(0f, (riseAmount - 0.01f * amount) * currentTime / totalTime, 0f);
        }
    }

    private void GeneratePuzzle()
    {
        _graph = RandomInt(0, Graphs.Length);
        _symbols = Shuffle(Enumerable.Range(0, SymbolComponents.Length).ToList()).Take(6).ToArray();

        bool[] presentSymbols = Enumerable.Range(0, SymbolComponents.Length).Select(x => _symbols.Contains(x)).ToArray();

        List<int> solution = new List<int>();

        int index = 0;
        bool stepForwards = true;
        while (presentSymbols.Count(x => x) > 1)
        {
            int column = Array.IndexOf(Permutations[_graph], _symbols.Length - presentSymbols.Count(x => x));

            if (presentSymbols[Columns[column][index]])
            {
                solution.Add(Array.IndexOf(_symbols, Columns[column][index]));
                presentSymbols[Columns[column][index]] = false;
                stepForwards = !stepForwards;
                continue;
            }

            index = (index + (stepForwards ? 1 : Columns[column].Length - 1)) % Columns[column].Length;
        }
        solution.Add(Array.IndexOf(_symbols, Array.IndexOf(presentSymbols, true)));
        _solution = solution.ToArray();

        Module.Log("The selected graph is graph {0}.", _graph + 1);
        Module.Log("The column numbering is: {0}.", Permutations[_graph].Select(x => x + 1).Join(" "));
        Module.Log("The selected symbols are: {0}.", _symbols.Join(", "));
        Module.Log("Pop the cysts in this order: {0}.", solution.Select(x => _symbols[x]).Join(", "));
    }

    public struct GraphLink
    {
        public int A;
        public int B;

        public GraphLink(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    public struct Graph
    {
        public GraphLink[] Links;

        public Graph(params int[] vertices)
        {
            Links = new GraphLink[vertices.Length / 2];
            for (int i = 0; i < Links.Length; i++)
            {
                Links[i] = new GraphLink(vertices[i * 2], vertices[i * 2 + 1]);
            }
        }
    }
}
