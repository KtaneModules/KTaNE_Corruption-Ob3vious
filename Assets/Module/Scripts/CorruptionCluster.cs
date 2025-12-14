using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CorruptionCluster
{
    public List<KMBombModule> TargetableModules { get; private set; }
    public List<CorruptionScript> Infected { get; private set; }
    public Action<CorruptionScript> OnInfect = (_) => { };

    public CorruptionCluster(List<KMBombModule> modules, CorruptionScript main)
    {
        TargetableModules = modules;
        Infected = new List<CorruptionScript> { main };
    }

    public List<KMBombModule> GetInfectionCandidates()
    {
        Queue<Transform> toScan = new Queue<Transform>(Infected.Where(x => !x.IsSolved).Select(x => x.transform));
        List<KMBombModule> joinedModules = new List<KMBombModule>();

        List<KMBombModule> modules = new List<KMBombModule>();
        List<float> weights = new List<float>();

        List<KMBombModule> toRemove = new List<KMBombModule>();

        while (toScan.Count > 0)
        {
            Transform current = toScan.Dequeue();

            foreach (KMBombModule potentialTarget in TargetableModules)
            {
                if (potentialTarget.GetComponent<CorruptionScript>() != null)
                {
                    toRemove.Add(potentialTarget);
                    continue;
                }

                if (CorruptionReflectionTools.IsSolved(potentialTarget))
                {
                    toRemove.Add(potentialTarget);
                    continue;
                }

                Vector3 globalDifference = current.position - potentialTarget.transform.position;
                Vector3 localDiff1 = current.transform.InverseTransformVector(globalDifference);
                Vector3 localDiff2 = potentialTarget.transform.InverseTransformVector(globalDifference);

                if ((localDiff1.normalized - localDiff2.normalized).magnitude > 1)
                    continue;

                if (Mathf.Abs(localDiff1.y) > 0.05f || Mathf.Abs(localDiff2.y) > 0.05f)
                    continue;

                if (Mathf.Min(Mathf.Abs(localDiff1.x), Mathf.Abs(localDiff1.z)) > 0.15f || Mathf.Min(Mathf.Abs(localDiff2.x), Mathf.Abs(localDiff2.z)) > 0.15f)
                    continue;

                if (Mathf.Max(Mathf.Abs(localDiff1.x), Mathf.Abs(localDiff1.z)) > 0.3f || Mathf.Max(Mathf.Abs(localDiff2.x), Mathf.Abs(localDiff2.z)) > 0.3f)
                    continue;

                if (Infected.Select(x => x.transform).Contains(current))
                {
                    modules.Add(potentialTarget);
                    weights.Add(Mathf.Min(Mathf.Abs(localDiff1.x), Mathf.Abs(localDiff1.z)));
                }

                if (!joinedModules.Contains(potentialTarget))
                {
                    joinedModules.Add(potentialTarget);
                    toScan.Enqueue(potentialTarget.transform);
                }
            }
        }

        TargetableModules.RemoveAll(x => toRemove.Contains(x) || !joinedModules.Contains(x));

        return modules;
    }

    public void Infect(KMBombModule module)
    {
        TargetableModules.Remove(module);
        Infected.Add(module.GetComponent<CorruptionScript>());
        Infected.Last().Cluster = this;
        Infected.Last().IsActive = true;

        OnInfect(Infected.Last());
    }
}
