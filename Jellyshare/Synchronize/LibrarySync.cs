using System;
using System.Collections.Generic;
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
    private readonly ILogger<SyncTask> _logger;

    public LibrarySync(
        HttpClient httpClient,
        ILibraryManager libraryManager,
        ILogger<SyncTask> logger
    )
    {
        _httpClient = httpClient;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public async Task SyncLibraries(CancellationToken cancellationToken)
    {
        var instance = Plugin.Instance!;
        foreach (var (remoteAddress, apiKey) in instance.RemoteServers)
        {
            var remoteLibraries = await GetRemoteLibraries(
                remoteAddress,
                apiKey,
                cancellationToken
            );
            var localLibraries = GetLocalLibraries();

            foreach (var remoteLibrary in remoteLibraries)
            {
                var name = $"{remoteAddress.Host} {remoteLibrary.Name}";
                if (!localLibraries.Contains(name))
                {
                    CreateLocalLibrary(remoteLibrary, remoteAddress);
                    localLibraries.Add(name);
                }
            }
        }
        await Plugin.Instance!.SaveState(cancellationToken);
    }

    private HashSet<string> GetLocalLibraries()
    {
        return _libraryManager
            .GetUserRootFolder()
            .Children.Concat(_libraryManager.RootFolder.VirtualChildren)
            .Select(item => item.Name)
            .ToHashSet();
    }

    private void CreateLocalLibrary(BaseItemDto remoteLibrary, Uri remoteAddress)
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
        var name = $"{remoteAddress.Host} {remoteLibrary.Name}";
        var path = Path.Combine(
            Plugin.Instance!.DataFolderPath,
            "Libraries",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(path);

        var folder = new Folder()
        {
            Path = path,
            ProviderIds = { { "JellyshareRemoteAddress", "" } }
        };
        _libraryManager.CreateItem(folder, folder);

        _libraryManager.AddVirtualFolder(
            name,
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
                        ImageFetchers = new[] { "Jellyshare" }
                    }
                }
            },
            true
        );
        _libraryManager.AddMediaPath(name, new MediaPathInfo() { Path = path });

        var localId = _libraryManager.GetNewItemId(path, typeof(Folder));
        Plugin.Instance!.RemoteLibraries[localId] = new RemoteLibrary()
        {
            LocalId = localId,
            ExternalId = remoteLibrary.Id,
            RemoteAddress = remoteAddress
        };
    }

    private async Task<HashSet<BaseItemDto>> GetRemoteLibraries(
        Uri remoteAddress,
        Guid apiKey,
        CancellationToken cancellationToken
    )
    {
        var path = $"/Library/MediaFolders?api_key={apiKey:N}";

        var mediaFolders = await _httpClient.GetFromJsonAsync<QueryResult<BaseItemDto>>(
            new Uri(remoteAddress, path),
            JsonDefaults.Options,
            cancellationToken
        );
        return mediaFolders!
            .Items.Where(
                mediaFolder =>
                    mediaFolder.Type == BaseItemKind.CollectionFolder
                    && !mediaFolder.Name.Contains("Jellyshare")
            )
            .ToHashSet();
    }
}
