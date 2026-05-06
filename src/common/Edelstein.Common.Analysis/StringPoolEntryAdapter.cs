using Edelstein.Protocol.Analysis;
using Maple.StringPool;

namespace Edelstein.Common.Analysis;

/// <summary>Adapts a <see cref="StringPoolEntry"/> to <see cref="IStringPoolEntry"/>.</summary>
internal sealed class StringPoolEntryAdapter : IStringPoolEntry
{
    private readonly StringPoolEntry _inner;

    internal StringPoolEntryAdapter(StringPoolEntry inner) => _inner = inner;

    internal StringPoolEntryAdapter(uint index, string value) => _inner = new StringPoolEntry(index, value);

    public int Index => (int)_inner.Index;
    public string Value => _inner.Value;
}
