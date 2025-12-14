using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CausticCraterModule : CorruptionModule
{
    private Vector3 _craterPosition;
    private float _craterRadius;

    private List<int> _values = new List<int>();
    private int _state;
    private int _score;

    private KMSelectable[] _selectables;
    private GameObject _bubble;
    private List<GameObject> _bubbles = new List<GameObject>();

    public CausticCraterModule(CorruptionScript module) : base(module)
    {
    }

    protected override IEnumerator Generate()
    {
        _score = 0;

        GeneratePhysical();

        StartIdleState();

        yield break;
    }

    protected override float PointHeight(float x, float z)
    {
        return (x - _craterPosition.x) * (x - _craterPosition.x) + (z - _craterPosition.z) * (z - _craterPosition.z) > _craterRadius * _craterRadius ? 0.015f : 0f;
    }

    public override string ModuleName()
    {
        return "Caustic Crater";
    }

    private void GeneratePhysical()
    {
        _craterPosition = new Vector3(RandomFloat(-0.025f, 0.025f), 0.015f / 2f, RandomFloat(-0.025f, 0.025f));
        _craterRadius = RandomFloat(0.025f, 0.05f);

        CorruptionScript main = Module.Cluster.Infected.First();

        List<GameObject> assets = main.CausticCraterAssets;

        main.ExposeResources();

        KMSelectable moduleSelectable = Module.GetComponent<KMSelectable>();
        moduleSelectable.ChildRowLength = 0;
        moduleSelectable.Children = new KMSelectable[1];

        GameObject obj = new GameObject();
        obj.transform.SetParent(Module.transform);
        obj.transform.localPosition = _craterPosition;
        obj.transform.localScale = Vector3.one;
        obj.transform.localEulerAngles = Vector3.zero;

        GameObject newPiece = GameObject.Instantiate(assets[2], obj.transform);
        newPiece.transform.localPosition = Vector3.zero;
        newPiece.transform.localScale = Vector3.one * (_craterRadius + 0.0125f) * 100;
        newPiece.transform.localEulerAngles = new Vector3(-90, 0, 0);

        _selectables = new KMSelectable[2];

        _selectables[0] = GameObject.Instantiate(assets[3], obj.transform).GetComponent<KMSelectable>();
        _selectables[0].transform.localPosition = new Vector3(0, 0.015f / 16f, 0f);
        _selectables[0].transform.localScale = Vector3.one * (_craterRadius + 0.0125f) * 100;
        _selectables[0].transform.localEulerAngles = new Vector3(90, 0, 0);
        _selectables[0].Parent = moduleSelectable;

        _selectables[0].enabled = true;
        CorruptionReflectionTools.UpdateSelectable(_selectables[0]);
        moduleSelectable.Children[0] = _selectables[0];
        moduleSelectable.UpdateChildren();

        //and now the bubble
        _bubble = new GameObject();
        _bubble.transform.SetParent(Module.transform);
        _bubble.transform.localPosition = _craterPosition;
        _bubble.transform.localScale = Vector3.zero;
        _bubble.transform.localEulerAngles = Vector3.zero;

        GameObject bubble = GameObject.Instantiate(assets[0], _bubble.transform);
        bubble.transform.localPosition = Vector3.zero;
        bubble.transform.localScale = Vector3.one * _craterRadius * 100;
        bubble.transform.localEulerAngles = new Vector3(-90, 0, 0);

        _selectables[1] = GameObject.Instantiate(assets[1], _bubble.transform).GetComponent<KMSelectable>();
        _selectables[1].transform.localPosition = Vector3.zero;
        _selectables[1].transform.localScale = Vector3.one * _craterRadius * 100;
        _selectables[1].transform.localEulerAngles = new Vector3(90, 0, 0);
        _selectables[1].Parent = moduleSelectable;

        _selectables[1].enabled = false;
        CorruptionReflectionTools.UpdateSelectable(_selectables[1]);

        main.CloseResources();

        _selectables[0].OnInteract += () =>
        {
            if (!_selectables[0].enabled)
                return false;

            if (_state == 0)
                Module.StartCoroutine(StartHold());
            _selectables[0].AddInteractionPunch(0.5f);

            return false;
        };

        _selectables[0].OnInteractEnded += () =>
        {
            if (!_selectables[0].enabled)
                return;

            if (_state == 1)
            {
                _state = 2;
                moduleSelectable.Children[0] = _selectables[1];
                moduleSelectable.UpdateChildren();
                _selectables[0].enabled = false;
                _selectables[1].enabled = true;
                Module.StartCoroutine(SummonBigBubble());
                Module.Log("The shown bubble counts are: {0}.", _values.Join(", "));
                Module.Log("The calculated fraction is {0}, meaning an angle of about {1} degrees is expected.", GetTargetAngle(), Mathf.Round((GetTargetAngle().AsFloat() % 1) * 360));
            }
        };

        _selectables[1].OnInteract += () =>
        {
            if (!_selectables[1].enabled)
                return false;

            Module.PlaySound("BigBubble", _bubble.transform);
            Module.StartCoroutine(PopBubble(_bubble.transform.localPosition, _bubble.transform.localScale.x * 0.5f * _craterRadius));
            _bubble.transform.localScale *= 0;
            moduleSelectable.Children[0] = _selectables[0];
            moduleSelectable.UpdateChildren();
            _selectables[0].enabled = true;
            _selectables[1].enabled = false;

            Fraction target = GetTargetAngle().InZeroToOne();
            Fraction[] available = Module.Cluster.Infected.Where(x => x.Module is CalamitousClockModule).Select(x => x.Module as CalamitousClockModule).Select(x => new Fraction(x.FastHand, 15).Subtract(x.CycleCount > 0 ? new Fraction(x.SlowHand * 15 + x.FastHand, x.CycleCount * 15) : new Fraction(0, 1)).InZeroToOne()).ToArray();
            Module.Log("The clock angles present when popping the bubble were: {0}.", available.Join(", "));
            Fraction[] viable = available.Where(x =>
                x.Subtract(target).Absolute().LessThanOrEqual(new Fraction(1, 15)) ||
                x.Subtract(target).Add(new Fraction(1, 1)).Absolute().LessThanOrEqual(new Fraction(1, 15)) ||
                x.Subtract(target).Subtract(new Fraction(1, 1)).Absolute().LessThanOrEqual(new Fraction(1, 15))).ToArray();
            Module.Log("Out of those angles, within range were: {0}.", viable.Join(", "));
            if (viable.Length > 0)
            {
                Module.Log("The bubble has been popped successfully, adding {0} units of progress.", _values.Count - 1);
                _score += _values.Count - 1;
                if (_score >= 5)
                {
                    _selectables[0].enabled = false;
                    Module.Log("Module has been solved!");
                    Module.Pass();
                    return false;
                }
            }
            else
            {
                Module.Log("The bubble has been popped at a wrong time. Strike!");
                Module.Strike();
            }

            StartIdleState();

            return false;
        };
    }

    private void StartIdleState()
    {
        _state = 0;
        _values.Clear();
        _values.Add(RandomInt(4, 7));
        for (int i = 0; i < _values.Last(); i++)
            Module.StartCoroutine(SpawnBubble(true, RandomFloat(0f, 0.5f)));
    }

    private IEnumerator SpawnBubble(bool respawnIfIdle = false, float startAge = 0)
    {
        CorruptionScript main = Module.Cluster.Infected.First();
        List<GameObject> assets = main.CausticCraterAssets;

        float currentTime = startAge;
        do
        {
            main.ExposeResources();

            GameObject bubble = GameObject.Instantiate(assets[4], Module.transform);
            bubble.transform.localPosition = Vector3.zero;
            bubble.transform.localScale = Vector3.one * _craterRadius * 100;
            bubble.transform.localEulerAngles = new Vector3(-90, 0, 0);

            main.CloseResources();

            float radius = .00125f * _craterRadius * 100;

            Vector3 position = Vector3.zero;
            do
            {
                position = new Vector3(RandomFloat(-1f, 1f), 0, RandomFloat(-1f, 1f)) * _craterRadius;
            } while (position.magnitude > _craterRadius - 0.00625 - radius || _bubbles.Any(x => (x.transform.localPosition - _craterPosition - position - new Vector3(0, (x.transform.localPosition - _craterPosition - position).y, 0)).magnitude < 2 * radius));
            position += _craterPosition;

            _bubbles.Add(bubble);

            float totalTime = 1f;

            Vector3 intitialPosition = position - new Vector3(0, radius, 0);
            bubble.transform.localPosition = intitialPosition + 3 * new Vector3(0, radius, 0) * currentTime / totalTime;
            while (currentTime < totalTime)
            {
                yield return null;
                currentTime += Time.deltaTime;
                if (currentTime > totalTime)
                    currentTime = totalTime;
                bubble.transform.localPosition = intitialPosition + 3 * new Vector3(0, radius, 0) * currentTime / totalTime;
            }

            _bubbles.Remove(bubble);
            GameObject.Destroy(bubble);
            Module.StartCoroutine(PopBubble(bubble.transform.localPosition, radius));
            if (!respawnIfIdle)
                Module.PlaySound("SmallBubble", _bubble.transform);
            currentTime = 0f;
        } while (respawnIfIdle && _state == 0);
    }

    private IEnumerator PopBubble(Vector3 position, float radius)
    {
        List<Vector3> dropletPositions = new List<Vector3>();
        List<GameObject> droplets = new List<GameObject>();

        float dropletRadius = 0.00125f;

        CorruptionScript main = Module.Cluster.Infected.First();
        List<GameObject> assets = main.CausticCraterAssets;
        main.ExposeResources();

        int attempts = 4000;
        while (attempts-- > 0)
        {
            Vector3 attempt = new Vector3(RandomFloat(-1f, 1f), RandomFloat(-1f, 1f), RandomFloat(-1f, 1f));
            if (attempt.sqrMagnitude > 1)
                continue;
            attempt = attempt.normalized * radius;
            if (dropletPositions.Any(x => (x - attempt).sqrMagnitude < dropletRadius * dropletRadius))
                continue;

            dropletPositions.Add(attempt);

            GameObject bubble = GameObject.Instantiate(assets[4], Module.transform);
            bubble.transform.localPosition = Vector3.zero;
            bubble.transform.localScale = Vector3.one * dropletRadius / 0.00125f;
            bubble.transform.localEulerAngles = new Vector3(-90, 0, 0);

            droplets.Add(bubble);
        }

        main.CloseResources();

        float totalTime = 0.25f;
        float speed = 16f * radius;
        float currentTime = 0f;
        while (currentTime < totalTime)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;

            for (int i = 0; i < droplets.Count; i++)
            {
                droplets[i].transform.localPosition = position + dropletPositions[i] + dropletPositions[i].normalized * speed * currentTime;
                droplets[i].transform.localScale = (dropletRadius / 0.00125f) * (1 - currentTime / totalTime) * Vector3.one;
            }
        }

        for (int i = 0; i < droplets.Count; i++)
            GameObject.Destroy(droplets[i]);
    }

    private IEnumerator StartHold()
    {
        _state = 1;

        float currentTime = 0;
        int intTimer = 0;
        while (_state == 1)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime < 1f)
                continue;

            currentTime -= 1f;
            intTimer++;

            if (intTimer > 5)
            {
                Module.Log("You exceeded 5 seconds of bubble growth. This is not allowed. Strike!");
                Module.Strike();
                StartIdleState();
                yield break;
            }

            int amount = RandomInt(1, 5);
            _values.Add(amount);
            for (int i = 0; i < amount; i++)
                Module.StartCoroutine(SpawnBubble());
        }
    }

    private IEnumerator SummonBigBubble()
    {
        float radiusMult = Mathf.Pow(_values.Count, 1 / 3f) / 2f;
        float radius = 0.5f * _craterRadius * radiusMult;
        _bubble.transform.localScale = Vector3.one * radiusMult;

        Vector3 position = Vector3.zero;
        do
        {
            position = new Vector3(RandomFloat(-1f, 1f), 0, RandomFloat(-1f, 1f)) * _craterRadius / 4f;
        } while (position.magnitude > _craterRadius - 0.00625 - radius);
        position += _craterPosition;

        float currentTime = 0f;
        float totalTime = 5f;

        Vector3 intitialPosition = position - new Vector3(0, radius, 0);
        _bubble.transform.localPosition = intitialPosition + 3 * new Vector3(0, radius, 0) * currentTime / totalTime;
        while (currentTime < totalTime && _state == 2)
        {
            yield return null;
            currentTime += Time.deltaTime;
            if (currentTime > totalTime)
                currentTime = totalTime;
            _bubble.transform.localPosition = intitialPosition + 3 * new Vector3(0, radius, 0) * currentTime / totalTime;
        }
    }

    private Fraction GetTargetAngle()
    {
        Fraction total = new Fraction(0, 1);
        Fraction frac = new Fraction(0, _values.First());
        for (int i = 1; i < _values.Count; i++)
        {
            frac = new Fraction(_values[i], frac.Denominator);
            total = total.Add(frac);
            frac = new Fraction(0, frac.Numerator + frac.Denominator);
        }

        return total;
    }

    private struct Fraction
    {
        public int Numerator { get; private set; }
        public int Denominator { get; private set; }

        public Fraction(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public Fraction Simplify()
        {
            int biggest = Denominator;
            int smallest = Numerator;

            while (smallest != 0)
            {
                biggest %= smallest;
                biggest ^= smallest;
                smallest ^= biggest;
                biggest ^= smallest;
            }

            if (biggest < 0)
                biggest *= -1;
            Numerator /= biggest;
            Denominator /= biggest;

            return this;
        }

        public Fraction Add(Fraction other)
        {
            int num = this.Numerator * other.Denominator + other.Numerator * this.Denominator;
            int den = this.Denominator * other.Denominator;
            return new Fraction(num, den).Simplify();
        }

        public Fraction Subtract(Fraction other)
        {
            return Add(new Fraction(-other.Numerator, other.Denominator));
        }

        public Fraction Absolute()
        {
            return new Fraction(Mathf.Abs(this.Numerator), Mathf.Abs(this.Denominator));
        }

        public bool LessThanOrEqual(Fraction other)
        {
            return this.Numerator * other.Denominator <= other.Numerator * this.Denominator;
        }

        public float AsFloat()
        {
            return (float)Numerator / Denominator;
        }

        public Fraction InZeroToOne()
        {
            return new Fraction((Numerator % Denominator + Denominator) % Denominator, Denominator);
        }

        public override string ToString()
        {
            return Numerator + "/" + Denominator;
        }
    }
}
