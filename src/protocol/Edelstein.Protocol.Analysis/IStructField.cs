namespace Edelstein.Protocol.Analysis;

/// <summary>Describes a single field within a native client struct.</summary>
public interface IStructField
{
    /// <summary>Name of the field.</summary>
    string Name { get; }

    /// <summary>Byte offset of the field within the owning struct.</summary>
    uint Offset { get; }
}
