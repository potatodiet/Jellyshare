using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyshare.Synchronize;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyshare.Tasks;

public class SyncTask : IScheduledTask
{
    public string Name => "Jellyshare Sync";
    public string Key => Name;
    public string Description => "Synchronize local libraries with remote federated servers.";
    public string Category => "Jellyshare";

    private readonly LibrarySync _librarySync;
    private readonly UserSync _userSync;
    private readonly VideoSync _videoSync;

    public SyncTask(
        ILibraryManager libraryManager,
        IServerApplicationHost applicationHost,
        IUserManager userManager,
        HttpClient httpClient,
        ILogger<SyncTask> logger
    )
    {
        _librarySync = new LibrarySync(httpClient, libraryManager, logger);
        _userSync = new UserSync(httpClient, applicationHost, userManager);
        _videoSync = new VideoSync(httpClient, libraryManager, logger);
    }

    // Create some dummy video file for each remote video.
    // Design a new IItemResolver with a custom ResolvePath for remoteVideos, which then sets the ID to be the video file name.
    // Might need to create a custom item type as well if possible.

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        await _librarySync.SyncLibraries(cancellationToken);
        await _userSync.SyncUsers(cancellationToken);
        await Task.Delay(10000, cancellationToken); // Ensure the remote libraries are saved before creating the remote videos.
        await _videoSync.SyncVideos(cancellationToken);
        await Plugin.Instance!.SaveState(cancellationToken);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Enumerable.Empty<TaskTriggerInfo>();
}
