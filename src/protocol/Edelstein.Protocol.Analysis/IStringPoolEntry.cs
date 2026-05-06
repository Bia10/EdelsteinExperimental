namespace Edelstein.Protocol.Analysis;

/// <summary>
/// Represents a decoded slot from the GMS v95 StringPool.
/// </summary>
public interface IStringPoolEntry
{
    /// <summary>Zero-based slot index.</summary>
    int Index { get; }

    /// <summary>Decoded string value of the slot.</summary>
    string Value { get; }
}
