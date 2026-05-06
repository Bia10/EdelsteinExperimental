namespace Edelstein.Common.Analysis;

/// <summary>Options for the client analysis services.</summary>
public sealed class AnalysisOptions
{
    /// <summary>
    /// Name of the GMS v95 client process without extension (default: <c>MapleStory</c>).
    /// Used by <see cref="Edelstein.Protocol.Analysis.IClientAnalysisService.TryAttach"/>.
    /// </summary>
    public string ProcessName { get; set; } = "MapleStory";

    /// <summary>
    /// Absolute path to the GMS v95 client executable (<c>MapleStory.exe</c>).
    /// Required when using <see cref="Edelstein.Protocol.Analysis.IClientStringPoolService"/>
    /// to decode the StringPool from the PE image on disk.
    /// </summary>
    public string? ClientExePath { get; set; }
}
