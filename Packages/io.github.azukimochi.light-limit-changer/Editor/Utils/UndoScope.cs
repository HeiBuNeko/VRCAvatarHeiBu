namespace io.github.azukimochi;

internal readonly struct UndoScope : IDisposable
{
    public readonly int GroupIndex;
    
    public UndoScope(string groupName)
    {
        Undo.SetCurrentGroupName(groupName);
        GroupIndex = Undo.GetCurrentGroup();
    }

    public void Dispose()
    {
        Undo.CollapseUndoOperations(GroupIndex);
    }
}

