using Edelstein.Protocol.Analysis;
using Maple.Client.V95;

namespace Edelstein.Common.Analysis;

/// <summary>Adapts a <see cref="StructField"/> to the protocol <see cref="IStructField"/>.</summary>
internal sealed class StructFieldAdapter : IStructField
{
    private readonly StructField _inner;

    internal StructFieldAdapter(StructField inner) => _inner = inner;

    public string Name => _inner.FieldName;
    public uint Offset => (uint)_inner.Offset;
}
