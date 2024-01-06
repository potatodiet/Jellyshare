using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using Jellyshare.State;
using Jellyshare.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyshare.Synchronize;

public class LibrarySync
{
    private readonly HttpClient _httpClient;
    private readonly ILibraryManager _libraryManager;
    private readonly StateManager _stateManager;
    private readonly ILogger<LibrarySync> _logger;

    public LibrarySync(
        HttpClient httpClient,
        ILibraryManager libraryManager,
        StateManager stateManager,
        ILogger<LibrarySync> logger
    )
    {
        _httpClient = httpClient;
        _libraryManager = libraryManager;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task SyncLibraries(CancellationToken cancellationToken)
    {
        var localLibraries = GetLocalLibraries();
        _logger.LogInformation("Hit LibrarySync");
        foreach (var server in _stateManager.RemoteServers.Values)
        {
            _logger.LogInformation($"Each: {server.Address}");
            var remoteLibraries = await GetRemoteLibraries(server, cancellationToken);
            foreach (var (libraryId, libraryName) in server.Libraries)
            {
                if (!localLibraries.Contains(libraryName))
                {
                    var remoteLibrary = remoteLibraries[libraryId];
                    CreateLocalLibrary(remoteLibrary, server.Address, libraryName);
                }
            }
        }
        await _stateManager.Refresh(cancellationToken);
    }

    private IEnumerable<string> GetLocalLibraries()
    {
        return _libraryManager
            .GetUserRootFolder()
            .Children.Concat(_libraryManager.RootFolder.VirtualChildren)
            .Select(item => item.Name)
            .ToHashSet();
    }

    private void CreateLocalLibrary(BaseItemDto remoteLibrary, Uri remoteAddress, string localName)
    {
        var collectionType = remoteLibrary.CollectionType switch
        {
            "movies" => CollectionTypeOptions.Movies,
            "tvshows" => CollectionTypeOptions.TvShows,
            _
                => throw new InvalidOperationException(
                    "The only libraries supported are movies, and tvshows."
                )
        };
        var path = Path.Combine(Plugin.Instance!.DataFolderPath, "Libraries", localName);
        Directory.CreateDirectory(path);

        _libraryManager.AddVirtualFolder(
            localName,
            collectionType,
            new LibraryOptions()
            {
                EnableRealtimeMonitor = false,
                MetadataSavers = Array.Empty<string>(),
                TypeOptions = new[]
                {
                    new TypeOptions()
                    {
                        Type = "Movie",
                        MetadataFetchers = new[] { "Jellyshare" },
                        ImageFetchers = new[] { "Jellyshare" },
                    }
                },
                PathInfos = new[] { new MediaPathInfo() { Path = path } }
            },
            true
        );

        var localId = _libraryManager.GetNewItemId(path, typeof(Folder));
        _libraryManager.GetItemById(localId).ProviderIds = new()
        {
            { "JellyshareRemoteAddress", remoteAddress.ToString() },
            { "JellyshareRemoteId", remoteLibrary.Id.ToString() },
        };
    }

    private async Task<Dictionary<Guid, BaseItemDto>> GetRemoteLibraries(
        RemoteServer server,
        CancellationToken cancellationToken
    )
    {
        var path = $"/Library/MediaFolders?api_key={server.ApiKey:N}";
        var mediaFolders = await _httpClient.GetFromJsonAsync<QueryResult<BaseItemDto>>(
            new Uri(server.Address, path),
            JsonDefaults.Options,
            cancellationToken
        );
        return mediaFolders!.Items.ToDictionary(item => item.Id, item => item);
    }
}
