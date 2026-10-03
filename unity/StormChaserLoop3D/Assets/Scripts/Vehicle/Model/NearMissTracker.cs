using System.Collections.Generic;

/// <summary>
/// vehicle-feel.md near-miss (F9 refill, E3 gate). A near-miss is a pass through the band from a funnel's
/// damage radius out to damage radius + NearMissMargin, leaving it at more than NearMissMinSpeed without
/// having entered the damage radius. Each funnel then has a NearMissCooldown. Pure C#: the adapter feeds
/// distances; disasters are keyed by an int id.
/// </summary>
public sealed class NearMissTracker
{
    private struct Entry
    {
        public int Id;
        public bool Inside;
        public bool Spoiled;
        public float CooldownUntil;
        public int SeenStamp;
    }

    private readonly List<Entry> _entries = new List<Entry>();
    private readonly float _margin, _minSpeed, _cooldown;
    private int _stamp;

    public NearMissTracker(float margin, float minSpeed, float cooldown)
    {
        _margin = margin;
        _minSpeed = minSpeed;
        _cooldown = cooldown;
    }

    /// <summary>Call once per physics step before <see cref="Observe"/>.</summary>
    public void BeginStep() => _stamp++;

    /// <summary>
    /// Feeds one disaster. Returns true when this observation completes a near-miss. A damage radius of 0
    /// (a harmless, still-forming funnel) never counts.
    /// </summary>
    public bool Observe(int id, float distance, float damageRadius, float speed, float now)
    {
        int i = IndexOf(id);
        Entry e = i >= 0 ? _entries[i] : new Entry { Id = id, CooldownUntil = float.NegativeInfinity };
        e.SeenStamp = _stamp;
        bool award = false;

        if (damageRadius <= 0f)
        {
            e.Inside = false;
            e.Spoiled = false;
        }
        else if (distance < damageRadius)
        {
            e.Inside = true;
            e.Spoiled = true; // touched the funnel: a hit, not a miss
        }
        else if (distance < damageRadius + _margin)
        {
            e.Inside = true;
        }
        else
        {
            award = e.Inside && !e.Spoiled && speed > _minSpeed && now >= e.CooldownUntil;
            if (award) e.CooldownUntil = now + _cooldown;
            e.Inside = false;
            e.Spoiled = false;
        }

        if (i >= 0) _entries[i] = e;
        else _entries.Add(e);
        return award;
    }

    /// <summary>Forgets disasters not observed this step (despawned). Call after the step's observations.</summary>
    public void EndStep()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
            if (_entries[i].SeenStamp != _stamp) _entries.RemoveAt(i);
    }

    private int IndexOf(int id)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].Id == id) return i;
        return -1;
    }
}
