namespace Haystack.Models;

/// <summary>
/// Which kind of record something points at · an alert's click-through, a
/// workflow task's subject. Shared so every pointing feature resolves records
/// the same way.
/// </summary>
public enum RecordKind
{
    Company,
    Contact,
}
