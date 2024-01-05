using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;

namespace Jellyshare.State;

public class Startup : IServerEntryPoint
{
    public void Dispose()
    {
        Plugin.Instance?.RemoteVideos.Clear();
        Plugin.Instance?.UserMap.Clear();
        GC.SuppressFinalize(this);
    }

    public async Task RunAsync()
    {
        await Plugin.Instance!.LoadState(CancellationToken.None);
    }
}
