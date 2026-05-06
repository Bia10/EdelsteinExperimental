using System.Diagnostics.CodeAnalysis;
using Edelstein.Protocol.Analysis;
using Maple.Client.V95;

namespace Edelstein.Common.Analysis;

/// <summary>
/// Wraps the static <see cref="ClientStructs.Registry"/> to implement <see cref="IClientStructRegistry"/>.
/// This implementation is cross-platform and stateless.
/// </summary>
public sealed class ClientStructRegistry : IClientStructRegistry
{
    public IEnumerable<string> StructNames => ClientStructs.Registry.StructNames;

    public bool TryGetField(string structName, string fieldName, [NotNullWhen(true)] out IStructField? field)
    {
        if (ClientStructs.Registry.TryGetField(structName, fieldName, out StructField raw))
        {
            field = new StructFieldAdapter(raw);
            return true;
        }

        field = null;
        return false;
    }

    public IReadOnlyDictionary<string, IStructField> GetFields(string structName) =>
        ClientStructs.Registry
            .GetFields(structName)
            .ToDictionary(
                kv => kv.Key,
                kv => (IStructField)new StructFieldAdapter(kv.Value));

    public uint CWvsContextSingletonAddress => ClientStructs.Addresses.CWvsContextSingletonPtr;

    public uint CLoginSingletonAddress => ClientStructs.Addresses.CUIWorldSelectSingletonPtr;
}
