using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CognisantCondensationModule : CorruptionModule
{
    public static int ElementCount = 4;

    private List<CognisantStage> _stages = new List<CognisantStage>();
    private int _stagesCleared = 0;
    private List<int> _sequence;

    private GameObject[] _buttons;
    private List<GameObject> _buttonDroplets = new List<GameObject>();
    private List<GameObject> _droplets = new List<GameObject>();
    private List<float> _radii = new List<float>();

    public CognisantCondensationModule(CorruptionScript module) : base(module)
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
        return "Cognisant Condensation";
    }

    private void GeneratePhysical()
    {
        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.CognisantCondensationAssets;

        main.ExposeResources();

        GameObject obj = new GameObject();
        obj.transform.SetParent(Module.transform);
        obj.transform.localPosition = new Vector3(-0.04f, 0.015f - RandomFloat(0.005f, 0.015f), 0f);
        obj.transform.localScale = Vector3.one * 2;
        obj.transform.localEulerAngles = RandomFloat(-1f, 1f) * Mathf.PI / 24 * new Vector3(0f, -180 / Mathf.PI, 0f);

        GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
        newPiece.transform.localPosition = Vector3.zero;
        newPiece.transform.localScale = Vector3.one;
        newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0);

        int attempts = 4096;
        while (attempts-- > 0)
        {
            Vector3 pos = new Vector3(RandomFloat(-0.0125f, 0.0125f), 0f, RandomFloat(-0.0325f, 0.0325f));
            float radius = RandomFloat(0.0003125f, 0.0025f);

            if (Mathf.Abs(pos.x) + radius /*+ 0.0003125f*/ > 0.0125f || Mathf.Abs(pos.z) + radius /*+ 0.0003125f*/ > 0.0325f)
                continue;

            bool good = true;
            for (int i = 0; i < _droplets.Count; i++)
                if ((_droplets[i].transform.localPosition - pos).magnitude < radius + _radii[i] /*+ 0.0003125f*/)
                {
                    good = false;
                    break;
                }
            if (!good)
                continue;

            GameObject newDrop = GameObject.Instantiate(assets[3], obj.transform);
            newDrop.transform.localPosition = pos;
            newDrop.transform.localScale = new Vector3(radius / 0.0025f, radius / 0.0025f, 1);
            newDrop.transform.localEulerAngles = new Vector3(-90, 0, 0);

            _droplets.Add(newDrop);
            _radii.Add(radius);
        }

        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        moduleSelectable.ChildRowLength = 0;
        moduleSelectable.Children = new KMSelectable[ElementCount];

        _buttons = new GameObject[ElementCount];
        for (int i = 0; i < ElementCount; i++)
        {
            GameObject obj2 = new GameObject();
            obj2.transform.SetParent(Module.transform);
            obj2.transform.localPosition = new Vector3(0.04f, 0.015f - RandomFloat(0.005f, 0.015f), -0.02f * (i * 2 - ElementCount + 1));
            obj2.transform.localScale = Vector3.one * 2;
            obj2.transform.localEulerAngles = RandomFloat(-1f, 1f) * Mathf.PI / 24 * new Vector3(0f, -180 / Mathf.PI, 0f);
            _buttons[i] = obj2;

            GameObject newPiece2 = GameObject.Instantiate(assets[2], obj2.transform);
            newPiece2.transform.localPosition = Vector3.zero;
            newPiece2.transform.localScale = Vector3.one;
            newPiece2.transform.localEulerAngles = new Vector3(-90, 0, 0);

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

                newSelectable.AddInteractionPunch(1f);

                Module.PlaySound("Clack", obj2.transform);

                if (i2 == _stages.Last().SolutionIndex)
                    Module.Log("Panel {0} has been pressed.", i2 + 1);
                else
                {
                    Module.Log("Panel {0} has been pressed, when the correct response would have been panel {1}. Strike!", i2 + 1, _stages.Last().SolutionIndex + 1);
                    Module.Strike();
                    //actually wipe the past
                    _stages = new List<CognisantStage> { _stages.Last().UpdateSolution(i2) };
                }

                _stagesCleared++;
                if (_stagesCleared >= 5)
                {
                    SpawnAnswerDroplets(new int[0]);
                    ShowInRadius(Vector3.zero, 0);

                    foreach (KMSelectable selectable in moduleSelectable.Children)
                        selectable.enabled = false;
                    Module.Log("Module has been solved!");
                    Module.Pass();
                    return false;
                }

                GenerateStage();

                return false;
            };
        }
        moduleSelectable.UpdateChildren();

        main.CloseResources();
    }

    private void SpawnAnswerDroplets(int[] amounts)
    {
        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.CognisantCondensationAssets;

        main.ExposeResources();

        foreach (GameObject droplet in _buttonDroplets)
            GameObject.Destroy(droplet);

        for (int i = 0; i < amounts.Length; i++)
        {
            int needed = amounts[i];
            List<float> radii = new List<float>();
            List<Vector3> currentDroplets = new List<Vector3>();

            while (needed > 0)
            {
                Vector3 pos = new Vector3(RandomFloat(-0.0125f, 0.0125f), 0f, RandomFloat(-0.0325f, 0.0325f));
                float radius = RandomFloat(0.000625f, 0.0025f);

                if (Mathf.Abs(pos.x) + radius /*+ 0.0003125f*/ > 0.0125f || Mathf.Abs(pos.z) + radius /*+ 0.0003125f*/ > 0.0025f)
                    continue;

                bool good = true;
                for (int j = 0; j < currentDroplets.Count; j++)
                    if ((currentDroplets[j] - pos).magnitude < radius + radii[j] /*+ 0.0003125f*/)
                    {
                        good = false;
                        break;
                    }
                if (!good)
                    continue;

                GameObject newDrop = GameObject.Instantiate(assets[3], _buttons[i].transform);
                newDrop.transform.localPosition = pos;
                newDrop.transform.localScale = new Vector3(radius / 0.0025f, radius / 0.0025f, 1);
                newDrop.transform.localEulerAngles = new Vector3(-90, 0, 0);

                _buttonDroplets.Add(newDrop);
                currentDroplets.Add(pos);
                radii.Add(radius);

                needed--;
            }
        }

        main.CloseResources();
    }

    private void ShowInRadius(Vector3 pos, float radius)
    {
        for (int i = 0; i < _droplets.Count; i++)
        {
            float scale = 1 - (_droplets[i].transform.localPosition - pos).magnitude / radius;

            //float maxDropletRadius = radius - (_droplets[i].transform.localPosition - pos).magnitude;
            //if (maxDropletRadius < 0.0003125f)
            float dropletRadius = _radii[i] * scale;
            if (dropletRadius < 0.0003125f)
            {
                _droplets[i].GetComponent<MeshRenderer>().enabled = false;
                continue;
            }

            _droplets[i].GetComponent<MeshRenderer>().enabled = true;
            //maxDropletRadius = Mathf.Min{maxDropletRadius, _radii[i]);

            _droplets[i].transform.localScale = new Vector3(dropletRadius / 0.0025f, dropletRadius / 0.0025f, 1);
        }
    }

    private IEnumerator TracePath(Vector3 pos1, Vector3 pos2, float radius, float speed)
    {
        int memStagesCleared = _stagesCleared;

        float distance = (pos2 - pos1).magnitude;
        float totalTime = distance / speed;
        float currentTime = 0f;

        while (currentTime < totalTime)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;

            if (memStagesCleared != _stagesCleared)
                break;

            ShowInRadius(Vector3.Lerp(pos1, pos2, currentTime / totalTime), radius);
        }
    }

    private IEnumerator TraceArc(Vector3 centre, float arcRadius, float startAngle, float endAngle, float radius, float speed)
    {
        int memStagesCleared = _stagesCleared;

        //angles in radians my beloved
        float distance = Mathf.Abs(endAngle - startAngle) * arcRadius;
        float totalTime = distance / speed;
        float currentTime = 0f;

        while (currentTime < totalTime)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;

            if (memStagesCleared != _stagesCleared)
                break;

            float angle = Mathf.Lerp(startAngle, endAngle, currentTime / totalTime);
            ShowInRadius(centre + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * arcRadius, radius);
        }
    }

    private IEnumerator RadiusShift(Vector3 pos, float radius1, float radius2, float speed)
    {
        int memStagesCleared = _stagesCleared;

        float distance = Mathf.Abs(radius2 - radius1);
        float totalTime = distance / speed;
        float currentTime = 0f;

        while (currentTime < totalTime)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;

            if (memStagesCleared != _stagesCleared)
                break;

            ShowInRadius(pos, Mathf.Lerp(radius1, radius2, currentTime / totalTime));
        }
    }

    private IEnumerator PlaySequence()
    {
        int memStagesCleared = _stagesCleared;
        while (true)
        {
            int dir = RandomInt(0, 2) * 2 - 1;

            for (int i = 0; i < _sequence.Count; i++)
            {
                switch (_sequence[i])
                {
                    case 0:
                    case 1:
                        {
                            int hmult = _sequence[i] == 0 ? 1 : -1;
                            yield return RadiusShift(new Vector3(-0.0125f * hmult, 0, 0), 0f, 0.01f, 0.02f);
                            if (memStagesCleared != _stagesCleared)
                                yield break;
                            yield return TracePath(new Vector3(-0.0125f * hmult, 0, 0), new Vector3(0, 0, 0.0325f) * dir, 0.01f, 0.04f);
                        }
                        break;
                    case 2:
                    case 3:
                        {
                            int hmult = _sequence[i] == 2 ? -1 : 1;
                            float arcRadius = (0.0125f + (0.01625f * 0.01625f) / 0.0125f) / 2;
                            Vector3 centre = new Vector3((0.0125f - arcRadius) * hmult, 0, -0.0325f / 2 * dir);
                            float angle1 = Mathf.Atan2(0.0325f / 2, arcRadius - 0.0125f) * -dir;
                            float angle2 = -angle1;

                            if (hmult == -1)
                            {
                                angle1 = Mathf.PI - angle1;
                                angle2 = Mathf.PI - angle2;
                            }

                            yield return TraceArc(centre, arcRadius, angle1, angle2, 0.01f, 0.04f);
                            if (memStagesCleared != _stagesCleared)
                                yield break;
                            yield return TraceArc(-centre, arcRadius, angle2 + Mathf.PI, angle1 + Mathf.PI, 0.01f, 0.04f);
                        }
                        break;
                    case 4:
                        yield return TracePath(new Vector3(0, 0, -0.0325f * dir), new Vector3(0, 0, 0.0325f * dir), 0.01f, 0.04f);
                        break;
                    case 5:
                    case 6:
                        {
                            int hmult = _sequence[i] == 5 ? 1 : -1;
                            yield return TracePath(new Vector3(0, 0, -0.0325f) * dir, new Vector3(-0.0125f * hmult, 0, 0), 0.01f, 0.04f);
                            if (memStagesCleared != _stagesCleared)
                                yield break;
                            yield return RadiusShift(new Vector3(-0.0125f * hmult, 0, 0), 0.01f, 0f, 0.02f);
                        }
                        break;
                    default:
                        break;
                }

                if (memStagesCleared != _stagesCleared)
                    yield break;

                dir *= -1;
            }
        }

    }

    private void GenerateStage()
    {
        int pastStageCount = _stages.Count;
        int timeTravel = RandomInt(1 * pastStageCount, 3 * pastStageCount);

        int commandCount = RandomInt(pastStageCount == 0 ? 2 : timeTravel / pastStageCount + 1, 5);

        int[] commandTimeTravels = new int[commandCount];

        while (timeTravel > 0)
        {
            int index = RandomInt(0, commandTimeTravels.Length);
            if (commandTimeTravels[index] >= pastStageCount)
                continue;
            commandTimeTravels[index]++;
            timeTravel--;
        }

        _sequence = new List<int>();
        for (int i = 0; i < commandCount; i++)
        {
            if (i == 0)
                _sequence.Add(RandomInt(0, 2));
            else
                _sequence.Add(RandomInt(2, 4));

            _sequence.AddRange(Enumerable.Repeat(4, commandTimeTravels[i]));
        }
        _sequence.Add(RandomInt(5, 7));

        CognisantStage newStage = new CognisantStage(Shuffle(Enumerable.Range(0, ElementCount).ToList()), RandomInt(0, ElementCount), -1);
        _stages.Add(newStage);

        List<int> solutions = Enumerable.Range(0, ElementCount).ToList();
        int pointer = 0;
        while (pointer < _sequence.Count)
        {
            int command = _sequence[pointer];
            int timeTravelCount = 0;
            while (++pointer < _sequence.Count && _sequence[pointer] == 4)
                timeTravelCount++;

            CognisantStage currentlyAnalysing = _stages[_stages.Count - 1 - timeTravelCount];
            switch (command)
            {
                case 0:
                    if (currentlyAnalysing.SolutionIndex != -1)
                        solutions = Enumerable.Repeat(currentlyAnalysing.SolutionIndex, ElementCount).ToList();
                    solutions = solutions.Select(x => currentlyAnalysing.PosToLabel(x)).ToList();
                    break;
                case 1:
                    if (currentlyAnalysing.SolutionIndex != -1)
                        solutions = Enumerable.Repeat(currentlyAnalysing.SolutionIndex, ElementCount).ToList();
                    break;
                case 2:
                    solutions = solutions.Select(x => currentlyAnalysing.LabelToPos(x)).ToList();
                    break;
                case 3:
                    solutions = solutions.Select(x => currentlyAnalysing.PosToLabel(x)).ToList();
                    break;
                case 5:
                    solutions = solutions.Select(x => currentlyAnalysing.LabelToPos(x)).ToList();
                    break;
                case 6:
                    break;
                default:
                    throw new Exception(command + " is not a valid command index.");
            }
        }

        List<int> solutionSlots = Enumerable.Range(0, ElementCount).Where(x => solutions[x] == x).ToList();

        _stages[_stages.Count - 1] = newStage.UpdateSolution(solutionSlots.Count != 1 ? newStage.ParadoxIndex : solutionSlots.First());

        Module.Log("The panel order is: {0}.", Enumerable.Range(0, ElementCount).Select(x => x == newStage.ParadoxIndex ? "*" : (newStage.PosToLabel(x) + 1).ToString()).Join(", "));
        Module.Log("The displayed sequence for the current stage is: {0}.", _sequence.Select(x => new string[] { "StartLeft", "StartRight", "LeftRight", "RightLeft", "Vertical", "EndLeft", "EndRight" }[x]).Join(", "));
        if (solutionSlots.Count != 1)
            Module.Log("The solutions are: {0}. This is not exactly one solution, meaning the paradox panel should be pressed instead.", solutionSlots.Count == 0 ? "none" : solutionSlots.Select(x => x + 1).Join(", "));
        Module.Log("The expected panel to press is in position {0}.", _stages[_stages.Count - 1].SolutionIndex + 1);


        SpawnAnswerDroplets(Enumerable.Range(0, ElementCount).Select(x => x == newStage.ParadoxIndex ? 0 : (newStage.PosToLabel(x) + 1)).ToArray());

        Module.StartCoroutine(PlaySequence());
    }

    private struct CognisantStage
    {
        public List<int> Permutation { get; private set; }
        public int ParadoxIndex { get; private set; }
        public int SolutionIndex { get; private set; }

        public CognisantStage(List<int> permutation, int paradoxIndex, int solutionIndex)
        {
            Permutation = permutation;
            ParadoxIndex = paradoxIndex;
            SolutionIndex = solutionIndex;
        }

        public int PosToLabel(int pos)
        {
            return Permutation[pos];
        }

        public int LabelToPos(int label)
        {
            return Permutation.IndexOf(label);
        }

        public CognisantStage UpdateSolution(int index)
        {
            SolutionIndex = index;
            return this;
        }
    }
}
