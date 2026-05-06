using System;
using System.Collections.Generic;

namespace Edelstein.Protocol.Analysis;

/// <summary>
/// Provides attached-process client analysis operations against a running GMS v95 client.
/// <para>
/// Implementations are Windows-only; on other platforms this interface should not be resolved.
/// </para>
/// </summary>
public interface IClientAnalysisService : IDisposable
{
    /// <summary>
    /// Indicates whether the service is currently attached to a live client process.
    /// </summary>
    bool IsAttached { get; }

    /// <summary>
    /// Attempts to attach to a running GMS v95 client process by name.
    /// </summary>
    /// <param name="processName">The process name without extension (e.g. "MapleStory").</param>
    /// <returns><c>true</c> when attachment succeeds; <c>false</c> when the process is not running.</returns>
    bool TryAttach(string processName);

    /// <summary>
    /// Reads a snapshot of the current login-flow state from the attached client.
    /// </summary>
    /// <returns>A state snapshot, or <c>null</c> when not attached or data is unavailable.</returns>
    IClientLoginState? ReadLoginState();

    /// <summary>
    /// Returns all basic blocks reachable from the function at <paramref name="functionAddress"/>
    /// in the attached client's memory.
    /// </summary>
    /// <param name="functionAddress">Absolute in-process address of the x86 function entry point.</param>
    /// <returns>A sequence of disassembled basic-block start addresses.</returns>
    IEnumerable<uint> GetControlFlowBlockAddresses(uint functionAddress);
}
