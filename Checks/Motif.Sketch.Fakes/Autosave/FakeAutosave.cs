namespace Motif.Sketch.Fakes;

/// <summary>Autosave in memory, with a pretend second tab that may own it.</summary>
public sealed class FakeAutosave : Autosave
{
    public AutosavedWork? Work { get; set; }

    /// <summary>True while another tab owns autosave.</summary>
    public bool OwnedElsewhere { get; set; }

    public int Stores { get; private set; }

    public event Action? Released;

    public Task<bool> Claim() => Task.FromResult(!OwnedElsewhere);

    public Task TakeOver()
    {
        OwnedElsewhere = false;
        return Task.CompletedTask;
    }

    /// <summary>Another tab clicks "Use this tab instead".</summary>
    public void TakenOverElsewhere()
    {
        OwnedElsewhere = true;
        Released?.Invoke();
    }

    public Task<AutosavedWork?> Restore() => Task.FromResult(Work);

    public Task Store(AutosavedWork work)
    {
        if (OwnedElsewhere) throw new InvalidOperationException("A tab that doesn't own autosave wrote to it.");
        Work = work;
        Stores++;
        return Task.CompletedTask;
    }
}
