using System.Runtime.Versioning;
using Edelstein.Protocol.Analysis;
using Maple.StringPool;
using Maple.StringPool.Source;
using Microsoft.Extensions.Options;

namespace Edelstein.Common.Analysis;

/// <summary>
/// Decodes the GMS v95 <c>StringPool</c> from the client PE image on disk.
/// Requires <see cref="AnalysisOptions.ClientExePath"/> to be configured.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ClientStringPoolService : IClientStringPoolService, IDisposable
{
    private readonly StringPoolDecoder _decoder;

    public ClientStringPoolService(IOptions<AnalysisOptions> options)
    {
        string? exePath = options.Value.ClientExePath;
        if (string.IsNullOrWhiteSpace(exePath))
            throw new InvalidOperationException(
                $"{nameof(AnalysisOptions.ClientExePath)} must be configured to use {nameof(ClientStringPoolService)}.");

        _decoder = StringPoolDecoder.Open(exePath);
    }

    public int SlotCount => _decoder.Count;

    public bool TryGetEntry(int index, out IStringPoolEntry? entry)
    {
        if (index < 0 || index >= _decoder.Count)
        {
            entry = null;
            return false;
        }

        string value = _decoder.GetString((uint)index);
        entry = new StringPoolEntryAdapter(new StringPoolEntry((uint)index, value));
        return true;
    }

    public IEnumerable<IStringPoolEntry> ReadAll() =>
        _decoder.EnumerateAll().Select(e => (IStringPoolEntry)new StringPoolEntryAdapter(e));

    public void Dispose() => _decoder.Dispose();
}
