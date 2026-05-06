using System.Collections.Generic;

namespace Edelstein.Protocol.Analysis;

/// <summary>
/// Provides zero-allocation read access to the GMS v95 <c>StringPool</c> singleton.
/// </summary>
public interface IClientStringPoolService
{
    /// <summary>Total number of slots in the StringPool.</summary>
    int SlotCount { get; }

    /// <summary>
    /// Attempts to decode the StringPool entry at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">Zero-based slot index.</param>
    /// <param name="entry">The decoded entry when found.</param>
    /// <returns><c>true</c> when the slot is valid and successfully decoded.</returns>
    bool TryGetEntry(int index, out IStringPoolEntry? entry);

    /// <summary>Decodes and returns all non-empty StringPool entries.</summary>
    IEnumerable<IStringPoolEntry> ReadAll();
}
