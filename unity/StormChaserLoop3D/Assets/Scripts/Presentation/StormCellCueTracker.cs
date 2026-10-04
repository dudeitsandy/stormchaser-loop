using System.Collections.Generic;

/// <summary>Run-scoped lifecycle deduplication; cancellation never manufactures touchdown audio.</summary>
public sealed class StormCellCueTracker
{
    private enum Stage { Forming, Peak, Rope, Ended }
    private readonly Dictionary<int, Stage> _cells = new Dictionary<int, Stage>();
    /// <summary>Registers formation; returns true only for a first EF3+ forming cue.</summary>
    public bool Forming(StormCellInfo cell)
    {
        if (_cells.ContainsKey(cell.CellId)) return false;
        _cells.Add(cell.CellId, Stage.Forming);
        return cell.EF >= 3;
    }
    /// <summary>Registers a real peak; only the anchor gets a sharper alert.</summary>
    public bool Peak(StormCellInfo cell)
    {
        if (!_cells.TryGetValue(cell.CellId, out Stage stage) || stage != Stage.Forming) return false;
        _cells[cell.CellId] = Stage.Peak;
        return cell.Role == StormCellRole.Anchor;
    }
    /// <summary>Returns true for failed touchdown; no peak alert is generated.</summary>
    public bool RopeOut(StormCellInfo cell)
    {
        if (!_cells.TryGetValue(cell.CellId, out Stage stage) || (stage != Stage.Forming && stage != Stage.Peak)) return false;
        _cells[cell.CellId] = Stage.Rope;
        return stage == Stage.Forming;
    }
    /// <summary>Ends a spawned cell; ignores duplicates and scheduled-but-never-spawned cells.</summary>
    public void Ended(StormCellInfo cell)
    {
        if (_cells.ContainsKey(cell.CellId)) _cells[cell.CellId] = Stage.Ended;
    }
    /// <summary>Queued radio must not play after the cell peaks, ropes out or ends.</summary>
    public bool IsForming(int cellId) => _cells.TryGetValue(cellId, out Stage stage) && stage == Stage.Forming;
    /// <summary>Clears identities at run start/end or presentation disable.</summary>
    public void Clear() => _cells.Clear();
}
