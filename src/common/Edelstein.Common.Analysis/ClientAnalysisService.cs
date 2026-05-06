using System.Runtime.Versioning;
using Edelstein.Protocol.Analysis;
using Maple.Client.V95;
using Maple.Client.V95.Analysis;
using Maple.Client.V95.Runtime;
using Maple.Memory;
using Maple.Process;

namespace Edelstein.Common.Analysis;

/// <summary>
/// Attaches to a live GMS v95 client process and provides login-state snapshots and
/// x86 control-flow analysis via the Maple domain libraries.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ClientAnalysisService : IClientAnalysisService
{
    private ProcessHandle? _processHandle;
    private WindowsProcessMemory? _processMemory;
    private MemoryAccessor? _accessor;
    private LoginStateResolver? _loginStateResolver;
    private RuntimeFunctionAnalyzer? _functionAnalyzer;

    private bool _disposed;

    public ClientAnalysisService() { }

    public bool IsAttached => _processHandle?.IsAttached ?? false;

    public bool TryAttach(string processName)
    {
        DisposeRuntimeResources();

        if (!ProcessHandle.TryAttach(processName, out ProcessHandle? handle, WindowsProcessAccess.Default))
            return false;

        _processHandle = handle!;
        _processMemory = WindowsProcessMemory.Open(_processHandle, WindowsProcessAccess.Default);
        _accessor = new MemoryAccessor(_processMemory, ownsProcessMemory: false);
        _loginStateResolver = new LoginStateResolver(_accessor, ClientStructs.Registry);

        ProcessPeImageSnapshot snapshot = ProcessPeImageSnapshot.CaptureMainModule(_processHandle, _accessor);
        _functionAnalyzer = new RuntimeFunctionAnalyzer(snapshot);

        return true;
    }

    public IClientLoginState? ReadLoginState()
    {
        if (_loginStateResolver is null || !IsAttached)
            return null;

        return _loginStateResolver.TryResolve(out ResolvedLoginState state)
            ? new ClientLoginState(state)
            : null;
    }

    public IEnumerable<uint> GetControlFlowBlockAddresses(uint functionAddress)
    {
        if (_functionAnalyzer is null || !IsAttached)
            return [];

        return _functionAnalyzer.TryTraverse(functionAddress, maxInstructionCount: 1000, maxPathCount: 100, out FunctionTraversal traversal)
            ? traversal.PathEntryAddresses
            : [];
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeRuntimeResources();
    }

    private void DisposeRuntimeResources()
    {
        _accessor?.Dispose();
        _processMemory?.Dispose();
        _processHandle?.Dispose();
        _accessor = null;
        _processMemory = null;
        _processHandle = null;
        _loginStateResolver = null;
        _functionAnalyzer = null;
    }
}
