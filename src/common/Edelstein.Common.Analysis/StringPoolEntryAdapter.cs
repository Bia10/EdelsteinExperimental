using Edelstein.Protocol.Analysis;
using Maple.StringPool;

namespace Edelstein.Common.Analysis;

/// <summary>Adapts a <see cref="StringPoolEntry"/> to <see cref="IStringPoolEntry"/>.</summary>
internal sealed class StringPoolEntryAdapter : IStringPoolEntry
{
    private readonly StringPoolEntry _inner;

    internal StringPoolEntryAdapter(StringPoolEntry inner) => _inner = inner;

    public int Index => (int)_inner.Index;
    public string Value => _inner.Value;
}
