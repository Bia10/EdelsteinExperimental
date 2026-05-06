using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Edelstein.Protocol.Analysis;

/// <summary>
/// Provides access to the GMS v95 client struct registry: field offset lookups and static singleton addresses.
/// </summary>
public interface IClientStructRegistry
{
    /// <summary>All known struct names in the registry.</summary>
    IEnumerable<string> StructNames { get; }

    /// <summary>
    /// Attempts to look up a field within a named struct.
    /// </summary>
    /// <param name="structName">Case-sensitive struct name (e.g. "CWvsContext").</param>
    /// <param name="fieldName">Case-sensitive field name (e.g. "WorldId").</param>
    /// <param name="field">The resolved field descriptor when found.</param>
    /// <returns><c>true</c> if the struct and field are registered; otherwise <c>false</c>.</returns>
    bool TryGetField(string structName, string fieldName, out IStructField? field);

    /// <summary>All fields registered for the specified struct, keyed by field name.</summary>
    IReadOnlyDictionary<string, IStructField> GetFields(string structName);

    /// <summary>Static in-process address of the <c>CWvsContext</c> singleton pointer.</summary>
    uint CWvsContextSingletonAddress { get; }

    /// <summary>Static in-process address of the <c>CLogin</c> singleton pointer.</summary>
    uint CLoginSingletonAddress { get; }
}
