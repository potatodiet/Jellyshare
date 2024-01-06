using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyshare.State;
using Jellyshare.Synchronize;
using MediaBrowser.Model.Tasks;

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
    private readonly StateManager _stateManager;

    public SyncTask(
        LibrarySync librarySync,
        UserSync userSync,
        VideoSync videoSync,
        StateManager stateManager
    )
    {
        _librarySync = librarySync;
        _userSync = userSync;
        _videoSync = videoSync;
        _stateManager = stateManager;
    }

    // Create some dummy video file for each remote video.
    // Design a new IItemResolver with a custom ResolvePath for remoteVideos, which then sets the ID to be the video file name.
    // Might need to create a custom item type as well if possible.

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        await _librarySync.SyncLibraries(cancellationToken);
        await _userSync.SyncUsers(cancellationToken);
        await _videoSync.SyncVideos(cancellationToken);
        await _stateManager.Save(cancellationToken);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Enumerable.Empty<TaskTriggerInfo>();
}
