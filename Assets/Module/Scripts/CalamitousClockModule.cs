using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class CalamitousClockModule : CorruptionModule
{
    public int FastHand { get; private set; }
    public int SlowHand { get; private set; }
    public int CycleCount { get; private set; }

    private float _scale;
    private Vector3 _offset;
    private Dictionary<CorruptionScript, GameObject> _moduleMarkers;
    private GameObject[] _hands;
    private KMSelectable _selectable;

    private bool _fast = false;
    private bool _ready = false;

    private CorruptionService _service;

    public CalamitousClockModule(CorruptionScript module) : base(module)
    {
    }

    protected override IEnumerator Generate()
    {
        if (Module.Cluster == null)
            BuildCluster();

        _service = GameObject.FindObjectOfType<CorruptionService>();
        if (_service != null)
        {
            yield return new WaitUntil(() => _service.SettingsLoaded);
            var offendingModule = Module.Cluster.TargetableModules.FirstOrDefault(n => _service.MustAutoSolve(n.ModuleType));
            if (offendingModule != null)
            {
                Module.Log("The module \"{0}\" is present, disallowing any module from being infected.");
                Module.Cluster.TargetableModules.Clear();
            }
            Module.Cluster.TargetableModules.RemoveAll(x => _service.MustNotBeInfected(x.ModuleType));
        }

        Module.Cluster.GetInfectionCandidates();

        GeneratePhysical();

        Module.Cluster.OnInfect += (m) =>
        {
            UpdateMiniMap(m);
        };

        Module.StartCoroutine(ClockCycle());

        yield break;
    }

    protected override float PointHeight(float x, float z)
    {
        return 0.015f;
    }

    public override string ModuleName()
    {
        return "Calamitous Clock";
    }

    private void BuildCluster()
    {
        Transform transform = Module.transform;
        Transform parentTransform;
        KMBomb bomb = transform.GetComponentInParent<KMBomb>();
        if (bomb != null)
            parentTransform = bomb.transform;
        else
        {
            parentTransform = transform;
            while (parentTransform.transform.parent != null && parentTransform.GetComponent("Bomb") == null)
                parentTransform = parentTransform.parent;
        }

        Module.Cluster = new CorruptionCluster(parentTransform.GetComponentsInChildren<KMBombModule>().ToList(), Module);
        Module.Cluster.TargetableModules.Remove(Module.GetComponent<KMBombModule>());

        foreach (KMBombModule otherModule in Module.Cluster.TargetableModules)
        {
            otherModule.OnPass += () => { Module.Cluster.TargetableModules.Remove(otherModule); return false; };
        }
    }

    private void GeneratePhysical()
    {
        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.CalamitousClockAssets;

        main.ExposeResources();

        _moduleMarkers = new Dictionary<CorruptionScript, GameObject>();
        IEnumerable<Vector3> coordinates = Module.Cluster.TargetableModules.Select(x => x.transform.position).Concat(Module.Cluster.Infected.Select(x => x.transform.position)).Select(x => Module.transform.InverseTransformPoint(x));

        float minX = coordinates.Select(x => x.x).Min();
        float maxX = coordinates.Select(x => x.x).Max();
        float minZ = coordinates.Select(x => x.z).Min();
        float maxZ = coordinates.Select(x => x.z).Max();

        float correctionFactor = 0.5f;
        Vector3 offset = new Vector3((minX + maxX) / 2f * -correctionFactor, 0.015f, (minZ + maxZ) / 2f * -correctionFactor);

        float maxDistance = 0.075f;

        _scale = maxDistance / Mathf.Max(Mathf.Abs(minX + offset.x), Mathf.Abs(maxX + offset.x), Mathf.Abs(minZ + offset.z), Mathf.Abs(maxZ + offset.z), maxDistance); //maxdistance is here to prevent division by 0
        _offset = new Vector3(offset.x * _scale, offset.y, offset.z * _scale);

        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        moduleSelectable.ChildRowLength = 0;
        moduleSelectable.Children = new KMSelectable[1];

        GameObject obj = new GameObject();
        obj.transform.SetParent(Module.transform);
        obj.transform.localPosition = _offset;
        obj.transform.localScale = Mathf.Min(_scale / 0.2f, 1f) * 1.5f * Vector3.one;
        obj.transform.localEulerAngles = Vector3.zero;

        GameObject newPiece = GameObject.Instantiate(assets[0], obj.transform);
        newPiece.transform.localPosition = Vector3.zero;
        newPiece.transform.localScale = Vector3.one;
        newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0) + RandomFloat(0, Mathf.PI * 2f) * new Vector3(0, 0, 180 / Mathf.PI);

        _moduleMarkers.Add(Module, newPiece);

        Module.OnSolve += () =>
        {
            Module.StartCoroutine(FadeCrystal(newPiece.GetComponent<MeshRenderer>()));
            foreach (GameObject go in _hands)
                Module.StartCoroutine(FadeCrystal(go.GetComponent<MeshRenderer>()));
        };

        _hands = new GameObject[2];
        for (int i = 0; i < _hands.Length; i++)
        {
            GameObject newHand = GameObject.Instantiate(assets[2], obj.transform);
            newHand.transform.localPosition = new Vector3(0, 0, -0.025f - 0.035f * i);
            newHand.transform.localScale = Vector3.one;
            newHand.transform.localEulerAngles = new Vector3(-90, 0, 0);
            _hands[i] = newHand;
        }

        _selectable = GameObject.Instantiate(assets[1], obj.transform).GetComponent<KMSelectable>();
        _selectable.transform.localPosition = Vector3.zero;
        _selectable.transform.localScale = Vector3.one;
        _selectable.transform.localEulerAngles = new Vector3(-90, 0, 0);
        _selectable.Parent = moduleSelectable;

        _selectable.enabled = true;
        CorruptionReflectionTools.UpdateSelectable(_selectable);
        moduleSelectable.Children[0] = _selectable;
        moduleSelectable.UpdateChildren();

        main.CloseResources();

        foreach (CorruptionScript m in Module.Cluster.Infected)
            if (!_moduleMarkers.ContainsKey(m))
                UpdateMiniMap(m);

        _selectable.OnInteract += () =>
        {
            if (_ready)
            {
                Module.Log("Module has been solved!");
                Module.Pass();
            }
            else
                _fast = true;
            _selectable.AddInteractionPunch(0.5f);

            return false;
        };

        _selectable.OnInteractEnded += () =>
        {
            _fast = false;
        };
    }

    private void UpdateMiniMap(CorruptionScript module)
    {
        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.CalamitousClockAssets;

        main.ExposeResources();

        GameObject obj = new GameObject();
        obj.transform.SetParent(Module.transform);
        obj.transform.localPosition = _offset + Module.transform.InverseTransformPoint(module.transform.position) * _scale;
        obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, _offset.y, obj.transform.localPosition.z);
        obj.transform.localScale = Mathf.Min(_scale / 0.2f, 1f) * 1.5f * Vector3.one;
        obj.transform.localEulerAngles = RandomFloat(0, Mathf.PI / 6f) * new Vector3(180 / Mathf.PI, 0, 0) + RandomFloat(0, Mathf.PI * 2f) * new Vector3(0, 180 / Mathf.PI, 0);

        GameObject newPiece = GameObject.Instantiate(assets[3], obj.transform);
        newPiece.transform.localPosition = Vector3.zero;
        newPiece.transform.localScale = Vector3.one;
        newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0) + RandomFloat(0, Mathf.PI * 2f) * new Vector3(0, 0, 180 / Mathf.PI);

        _moduleMarkers.Add(module, newPiece);

        module.OnSolve += () =>
        {
            Module.StartCoroutine(FadeCrystal(newPiece.GetComponent<MeshRenderer>()));

            if (Module.Cluster.Infected.Any(x => !x.IsSolved && !(x.Module is CalamitousClockModule)))
                return;

            Ready();
        };

        //discolor immediately
        if (module.IsSolved)
        {
            MeshRenderer crystal = newPiece.GetComponent<MeshRenderer>();
            crystal.material.color = new Color32(0x44, 0x33, 0x44, 0xc0);
            crystal.material.SetColor("_Emission", new Color32(0x00, 0x00, 0x00, 0xc0));
        }

        main.CloseResources();
    }

    private IEnumerator ClockCycle()
    {
        yield return new WaitUntil(() => Module.IsActive);
        float subSecondTimer = 0;
        FastHand = 0;
        SlowHand = 0;
        CycleCount = 4;
        while (!_ready)
        {
            yield return null;
            subSecondTimer += Time.deltaTime * (_fast ? 15 : 1);
            while (subSecondTimer >= 1f)
            {
                if (FastHand % 3 == 0 || !_fast)
                    Module.PlaySound("ClockTick", _selectable.transform);

                subSecondTimer--;
                FastHand = (FastHand + 1) % 15;
                if (FastHand != 0)
                    continue;

                List<KMBombModule> candidates = Module.Cluster.GetInfectionCandidates();
                Ready();

                if (CycleCount > 0)
                {
                    SlowHand = (SlowHand + 1) % CycleCount;
                    if (SlowHand != 0)
                        continue;

                    CycleCount++;

                    if (candidates.Count == 0)
                    {
                        //Module.Log("No more modules left to be infected. Stopping the larger hand.");
                        //CycleCount = 0;
                        continue;
                    }

                    KMBombModule module = PickRandom(candidates);
                    Vector3 coordinate = Module.transform.InverseTransformPoint(module.transform.position) / 0.2f;
                    Module.Log("Infecting the module \"{0}\" at relative position: ({1}, {2}).", module.ModuleDisplayName, coordinate.x.ToString("0.00"), coordinate.z.ToString("0.00"));

                    Infect(module);
                }
            }

            float[] angles = new float[2];
            angles[0] = FastHand / 15f * Mathf.PI * 2f;
            angles[1] = CycleCount > 0 ? (SlowHand + FastHand / 15f) / CycleCount * Mathf.PI * 2f : 0;

            for (int i = 0; i < _hands.Length; i++)
            {
                float magnitude = -0.025f - 0.035f * i;
                _hands[i].transform.localPosition = magnitude * new Vector3(-Mathf.Sin(angles[i]), 0, Mathf.Cos(angles[i]));
                _hands[i].transform.localEulerAngles = new Vector3(-90, 0, 0) + angles[i] * new Vector3(0, 0, -180 / Mathf.PI);
            }
        }
        foreach (GameObject go in _hands)
            Module.StartCoroutine(ShootCrystal(go));
    }

    private IEnumerator FadeCrystal(MeshRenderer crystal)
    {
        float totalTime = 0.5f;
        float currentTime = 0f;
        Color initColor = crystal.material.color;
        Color initColor2 = crystal.material.GetColor("_Emission");
        while (currentTime < totalTime)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;
            crystal.material.color = Color.Lerp(initColor, new Color32(0x44, 0x33, 0x44, 0xc0), currentTime / totalTime);
            crystal.material.SetColor("_Emission", Color.Lerp(initColor2, new Color32(0x00, 0x00, 0x00, 0xc0), currentTime / totalTime));
        }
    }

    private IEnumerator ShootCrystal(GameObject crystal)
    {
        float totalTime = 0.25f;
        float speed = 2f;
        float currentTime = 0f;
        Vector3 intitialPosition = crystal.transform.localPosition;
        Vector3 initialScale = crystal.transform.localScale;
        while (currentTime < totalTime)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;
            crystal.transform.localPosition = intitialPosition + crystal.transform.localPosition.normalized * speed * currentTime;
            crystal.transform.localScale = initialScale * (1 - currentTime / totalTime);
        }
    }

    public bool Ready()
    {
        if (_ready)
            return _ready;

        List<KMBombModule> candidates = Module.Cluster.GetInfectionCandidates();
        if (Module.Cluster.Infected.Any(x => !x.IsSolved && !(x.Module is CalamitousClockModule)) || candidates.Count > 0)
            return false;

        Module.Log("No more modules left to be infected or solved. This module is now ready for solve.");
        _ready = true;
        return true;
    }

    private void Infect(KMBombModule module)
    {
        List<Component> remove = module.GetComponents<Component>().Where(x => !new Type[] { typeof(KMBombModule), typeof(Transform), typeof(KMSelectable) }.Contains(x.GetType()))
            .Where(x => !new string[] { "TestSelectable", "ModSelectable", "ModSource", "ModBombComponent", "ExcludeFromStaticBatch" }.Contains(x.GetType().Name)).ToList();
        foreach (Component component in remove)
        {
            GameObject.Destroy(component);
        }

        Transform hlTransform = module.GetComponent<KMSelectable>().Highlight.transform;
        hlTransform.SetParent(module.transform);
        hlTransform.localPosition = Vector3.zero;
        int iterator = 0;
        while (module.transform.childCount > iterator)
        {
            if (module.transform.GetChild(iterator) == hlTransform || module.transform.GetChild(iterator).gameObject.name == "Module Popup(Clone)")
            {
                iterator++;
                continue;
            }
            Debug.Log("Destroying \"" + module.transform.GetChild(iterator).gameObject.name + "\"");
            GameObject.Destroy(module.transform.GetChild(iterator).gameObject);
            iterator++;
        }

        module.StopAllCoroutines();

        VeryBadEvilCode(module.GetComponent<KMSelectable>());

        module.gameObject.AddComponent(typeof(CorruptionScript));

        Module.Cluster.Infect(module);

        Module.Cluster.Infected.Last().Terrain = GameObject.Instantiate(Module.Terrain, module.transform);




        string moduleType = RandomInt(0, 32) == 0 ? "CalamitousClock" : (_service == null ? "Unexamined" : _service.GetModuleType(module.ModuleType));
        if (moduleType == "Unexamined")
            moduleType = PickRandom(new List<string> { "CausticCrater", "ClottedCapillaries", "CognisantCondensation", "CollapsingCartilage", "ConnectedCysts", "ControlledChasms", "CryingCirri" });

        CorruptionModule newMod;
        switch (moduleType)
        {
            case "CalamitousClock":
                newMod = new CalamitousClockModule(Module.Cluster.Infected.Last());
                break;
            case "CausticCrater":
                newMod = new CausticCraterModule(Module.Cluster.Infected.Last());
                break;
            case "ClottedCapillaries":
                newMod = new ClottedCapillariesModule(Module.Cluster.Infected.Last());
                break;
            case "CognisantCondensation":
                newMod = new CognisantCondensationModule(Module.Cluster.Infected.Last());
                break;
            case "CollapsingCartilage":
                newMod = new CollapsingCartilageModule(Module.Cluster.Infected.Last());
                break;
            case "ConnectedCysts":
                newMod = new ConnectedCystsModule(Module.Cluster.Infected.Last());
                break;
            case "ControlledChasms":
                newMod = new ControlledChasmsModule(Module.Cluster.Infected.Last());
                break;
            case "CryingCirri":
                newMod = new CryingCirriModule(Module.Cluster.Infected.Last());
                break;
            default:
                throw new Exception("Unidentified module type \"" + moduleType + "\" found in \"" + module.ModuleDisplayName + "\"");
        }

        Module.Cluster.Infected.Last().Module = newMod;

        newMod.StartModule();
    }

#warning TODO: patch game's OnInteract invocations (Harmony with community modkit)
    private void VeryBadEvilCode(KMSelectable selectable)
    {
        if (selectable.OnInteract != null)
        {
            Delegate[] delegates = selectable.OnInteract.GetInvocationList();
            selectable.OnInteract = null;
            foreach (Delegate del in delegates)
                selectable.OnInteract += () => { try { return (bool)del.DynamicInvoke(); } catch { return true; }; };
        }

        if (selectable.OnInteractEnded != null)
        {
            Delegate[] delegates = selectable.OnInteractEnded.GetInvocationList();
            selectable.OnInteractEnded = null;
            foreach (Delegate del in delegates)
                selectable.OnInteractEnded += () => { try { del.DynamicInvoke(); } catch { }; };
        }

        if (selectable.OnHighlight != null)
        {
            Delegate[] delegates = selectable.OnHighlight.GetInvocationList();
            selectable.OnHighlight = null;
            foreach (Delegate del in delegates)
                selectable.OnHighlight += () => { try { del.DynamicInvoke(); } catch { }; };
        }

        if (selectable.OnHighlightEnded != null)
        {
            Delegate[] delegates = selectable.OnHighlightEnded.GetInvocationList();
            selectable.OnHighlightEnded = null;
            foreach (Delegate del in delegates)
                selectable.OnHighlightEnded += () => { try { del.DynamicInvoke(); } catch { }; };
        }

        if (selectable.OnSelect != null)
        {
            Delegate[] delegates = selectable.OnSelect.GetInvocationList();
            selectable.OnSelect = null;
            foreach (Delegate del in delegates)
                selectable.OnSelect += () => { try { del.DynamicInvoke(); } catch { }; };
        }

        if (selectable.OnFocus != null)
        {
            Delegate[] delegates = selectable.OnFocus.GetInvocationList();
            selectable.OnFocus = null;
            foreach (Delegate del in delegates)
                selectable.OnFocus += () => { try { del.DynamicInvoke(); } catch { }; };
        }

        if (selectable.OnDeselect != null)
        {
            Delegate[] delegates = selectable.OnDeselect.GetInvocationList();
            selectable.OnDeselect = null;
            foreach (Delegate del in delegates)
                selectable.OnDeselect += () => { try { del.DynamicInvoke(); } catch { }; };
        }

        if (selectable.OnDefocus != null)
        {
            Delegate[] delegates = selectable.OnDefocus.GetInvocationList();
            selectable.OnDefocus = null;
            foreach (Delegate del in delegates)
                selectable.OnDefocus += () => { try { del.DynamicInvoke(); } catch { }; };
        }
    }
}
