using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;

namespace Jellyshare.State;

public class Startup : IServerEntryPoint
{
    private readonly StateManager _stateManager;

    public Startup(StateManager stateManager)
    {
        _stateManager = stateManager;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public async Task RunAsync()
    {
        await _stateManager.Refresh(CancellationToken.None);
    }
}
