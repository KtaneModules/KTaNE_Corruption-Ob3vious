using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CorruptionScript : MonoBehaviour
{
    public CorruptionCluster Cluster { get; set; }

    public bool IsActive = false;

    public bool IsSolved = false;
    public Action OnSolve = () => { };

    public CorruptionModule Module;

    public CorruptionMesh Terrain;

    public KMSelectable DummySelectable;
    public List<GameObject> CalamitousClockAssets;
    public List<GameObject> CausticCraterAssets;
    public List<GameObject> ClottedCapillariesAssets;
    public List<GameObject> CognisantCondensationAssets;
    public List<GameObject> CollapsingCartilageAssets;
    public List<GameObject> ConnectedCystsAssets;
    public List<GameObject> ControlledChasmsAssets;
    public List<GameObject> CryingCirriAssets;

    private int _moduleId = 0;
    private static int _moduleIdCounter = 1;

    void Start()
    {
        CloseResources();

        if (!IsActive)
        {
            _moduleId = _moduleIdCounter++;
            GetComponent<KMBombModule>().OnActivate += () =>
            {
                IsActive = true;
                Initialise();
            };
            return;
        }
    }

    private void Initialise()
    {
        if (Cluster == null)
        {
            Module = new CalamitousClockModule(this);

            Module.StartModule();

            return;
        }

        Module.StartModule();
    }

    public void Pass()
    {
        Module.Dummify();
        IsSolved = true;
        OnSolve();
        PlaySound("ModuleSolve", transform);
        GetComponent<KMBombModule>().HandlePass();
    }

    public void Strike()
    {
        GetComponent<KMBombModule>().HandleStrike();
    }

    private List<Renderer> _disabledRenderers = new List<Renderer>();
    public void ExposeResources()
    {
        foreach (Renderer renderer in _disabledRenderers)
            renderer.enabled = true;

        _disabledRenderers.Clear();
    }

    public void CloseResources()
    {
        List<GameObject> objects = new List<GameObject>();
        if (CalamitousClockAssets != null)
            objects.AddRange(CalamitousClockAssets);
        if (CausticCraterAssets != null)
            objects.AddRange(CausticCraterAssets);
        if (ClottedCapillariesAssets != null)
            objects.AddRange(ClottedCapillariesAssets);
        if (CognisantCondensationAssets != null)
            objects.AddRange(CognisantCondensationAssets);
        if (CollapsingCartilageAssets != null)
            objects.AddRange(CollapsingCartilageAssets);
        if (ConnectedCystsAssets != null)
            objects.AddRange(ConnectedCystsAssets);
        if (ControlledChasmsAssets != null)
            objects.AddRange(ControlledChasmsAssets);
        if (CryingCirriAssets != null)
            objects.AddRange(CryingCirriAssets);

        foreach (GameObject obj in objects)
            foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled == false)
                    continue;
                renderer.enabled = false;
                _disabledRenderers.Add(renderer);
            }
    }

    public void Log(string message, params object[] args)
    {
        if (Cluster == null)
        {
            SendMessageToLog(string.Format("[{0} #{1}] {2}", Module.ModuleName(), 1, string.Format(message, args)));
            return;
        }

        Cluster.Infected.First().SendMessageToLog(string.Format(
                "[{0} #{1}] {2}",
                Module.ModuleName(),
                Cluster.Infected.TakeWhile(x => x != this).Where(x => x.Module.ModuleName() == Module.ModuleName()).Count() + 1,
                string.Format(message, args)));
    }

    private void SendMessageToLog(string message)
    {
        Debug.LogFormat("[Corruption #{0}] {1}", _moduleId, message);
    }

    public void PlaySound(string name, Transform location)
    {
        if (Cluster == null)
        {
            GetComponent<KMAudio>().PlaySoundAtTransform(name, location);
            return;
        }

        Cluster.Infected.First().GetComponent<KMAudio>().PlaySoundAtTransform(name, location);
    }
}
