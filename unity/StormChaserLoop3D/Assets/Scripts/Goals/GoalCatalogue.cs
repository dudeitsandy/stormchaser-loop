using System.Collections.Generic;

/// <summary>Career goals persist across runs; bounties belong to one run (event-system.md Run Goals Rule 1).</summary>
public enum GoalKind { Career, Bounty }

/// <summary>v1 goal types. Destruction, Rescue, NPC Challenge and Collectible are reserved for later.</summary>
public enum GoalType { Score, Style, Storm }

/// <summary>What a run's Weather Plan must contain for a bounty to be drawable (Rule 3, "eligible when").</summary>
public enum BountyNeed { Always, AnyCell, Ef3Plus, Ef5, Anchor }

/// <summary>One catalogue entry. Immutable; IDs never change meaning once shipped (save-profile keeps unknown IDs).</summary>
public sealed class GoalDef
{
    /// <summary>Stable ID: <c>career.&lt;mode&gt;.&lt;map&gt;.&lt;goal&gt;</c> or <c>bounty.&lt;mode&gt;.&lt;name&gt;</c>.</summary>
    public readonly string Id;
    public readonly GoalKind Kind;
    public readonly GoalType Type;
    /// <summary>Full text: the HUD bounty list or the career page.</summary>
    public readonly string Text;
    /// <summary>Results-screen name, at most 16 characters (design/ux/run-screens.md Localization).</summary>
    public readonly string ShortName;
    /// <summary>In-run score bonus; career Score goals pay none.</summary>
    public readonly int Bonus;
    /// <summary>Bounties only: what the plan must contain.</summary>
    public readonly BountyNeed Need;
    /// <summary>Bounties only: has a time window that can fail.</summary>
    public readonly bool Timed;

    public GoalDef(string id, GoalKind kind, GoalType type, string text, string shortName, int bonus,
                   BountyNeed need = BountyNeed.Always, bool timed = false)
    {
        Id = id;
        Kind = kind;
        Type = type;
        Text = text;
        ShortName = shortName;
        Bonus = bonus;
        Need = need;
        Timed = timed;
    }

    /// <summary>The part after the last dot, e.g. <c>big_air</c>.</summary>
    public string Key => Id.Substring(Id.LastIndexOf('.') + 1);
}

/// <summary>
/// Run Goals v1 catalogue (event-system.md "# Run Goals (v1)", Rules 1–3): the fixed career list for compact /
/// heartland and the v1 KTVR bounty pool. Data only; thresholds live in <see cref="GoalTuning"/>.
/// </summary>
public static class GoalCatalogue
{
    public const string Mode = "compact";
    public const string Map = "heartland";
    /// <summary>Bonus for a career Style or Storm goal (Formulas: Run bonus).</summary>
    public const int CareerBonus = 150;

    private const string C = "career." + Mode + "." + Map + ".";
    private const string B = "bounty." + Mode + ".";

    /// <summary>The 10 career goals, in career-page order (Rule 2).</summary>
    public static readonly IReadOnlyList<GoalDef> Career = new[]
    {
        new GoalDef(C + "score_rookie", GoalKind.Career, GoalType.Score, "ROOKIE SCORE", "ROOKIE", 0),
        new GoalDef(C + "score_pro", GoalKind.Career, GoalType.Score, "PRO SCORE", "PRO", 0),
        new GoalDef(C + "score_sick", GoalKind.Career, GoalType.Score, "SICK SCORE", "SICK", 0),
        new GoalDef(C + "ef4_peak", GoalKind.Career, GoalType.Storm, "SHOOT AN EF4+ AT ITS PEAK", "EF4 AT PEAK", CareerBonus),
        new GoalDef(C + "point_blank", GoalKind.Career, GoalType.Storm, "SHOOT A TORNADO POINT-BLANK", "POINT BLANK", CareerBonus),
        new GoalDef(C + "toss_survivor", GoalKind.Career, GoalType.Storm, "GET TOSSED AND FINISH THE RUN", "TOSS SURVIVOR", CareerBonus),
        new GoalDef(C + "storm_drift", GoalKind.Career, GoalType.Style, "LONG DRIFT BESIDE A STORM", "STORM DRIFT", CareerBonus),
        new GoalDef(C + "big_air", GoalKind.Career, GoalType.Style, "BIG AIR", "BIG AIR", CareerBonus),
        new GoalDef(C + "near_misses", GoalKind.Career, GoalType.Style, "NEAR MISSES IN ONE RUN", "NEAR MISSES", CareerBonus),
        new GoalDef(C + "front_page", GoalKind.Career, GoalType.Storm, "PERFECT SHOT OF THE MAIN STORM", "FRONT PAGE", CareerBonus),
    };

    /// <summary>
    /// The v1 KTVR bounty pool in catalogue order (Rule 3). HUD text never states an EF number or any digit, and
    /// fits 28 characters.
    /// </summary>
    public static readonly IReadOnlyList<GoalDef> Bounties = new[]
    {
        new GoalDef(B + "point_blank_ef5", GoalKind.Bounty, GoalType.Storm, "POINT-BLANK ON A MONSTER", "POINT-BLANK", 500, BountyNeed.Ef5),
        new GoalDef(B + "warning_cell", GoalKind.Bounty, GoalType.Storm, "SHOOT THE NEXT WARNED STORM", "WARNED STORM", 300, BountyNeed.Ef3Plus, timed: true),
        new GoalDef(B + "before_touchdown", GoalKind.Bounty, GoalType.Storm, "CATCH THE MAIN STORM FORMING", "CAUGHT FORMING", 250, BountyNeed.Anchor),
        new GoalDef(B + "rope_out", GoalKind.Bounty, GoalType.Storm, "GET THE ROPE-OUT", "ROPE-OUT", 200, BountyNeed.AnyCell),
        new GoalDef(B + "close_call", GoalKind.Bounty, GoalType.Storm, "RIDE IT OUT UP CLOSE", "CLOSE CALL", 300, BountyNeed.Ef3Plus),
        new GoalDef(B + "double_near_miss", GoalKind.Bounty, GoalType.Style, "TWO NEAR MISSES, TEN SECONDS", "DOUBLE MISS", 250, BountyNeed.Always),
        new GoalDef(B + "drift_by", GoalKind.Bounty, GoalType.Style, "DRIFT PAST A TWISTER", "DRIFT-BY", 250, BountyNeed.AnyCell),
    };

    /// <summary>Looks up a goal or bounty by ID; null for an unknown ID (kept, never interpreted).</summary>
    public static GoalDef Find(string id)
    {
        foreach (GoalDef g in Career) if (g.Id == id) return g;
        foreach (GoalDef g in Bounties) if (g.Id == id) return g;
        return null;
    }
}
