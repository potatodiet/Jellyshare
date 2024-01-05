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
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyshare.Synchronize;

public class VideoSync
{
    private readonly HttpClient _httpClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<SyncTask> _logger;

    public VideoSync(
        HttpClient httpClient,
        ILibraryManager libraryManager,
        ILogger<SyncTask> logger
    )
    {
        _httpClient = httpClient;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public async Task SyncVideos(CancellationToken cancellationToken)
    {
        var localVideos = GetLocalVideos();
        foreach (var library in GetLocalFolders())
        {
            var remoteVideos = await GetRemoteVideos(library, cancellationToken);
            foreach (var remoteVideo in remoteVideos)
            {
                if (!localVideos.Contains(remoteVideo.Id))
                {
                    CreateVideo(remoteVideo, library);
                    localVideos.Add(remoteVideo.Id);
                }
            }
            _ = library.RefreshMetadata(cancellationToken);
        }
        Plugin.Instance!.RefreshRemoteVideos();
    }

    private void CreateVideo(BaseItemDto remoteVideo, Folder libraryEntity)
    {
        var path = Path.Combine(libraryEntity.Path, remoteVideo.Id.ToString() + ".mp4");
        File.Create(path);

        var itemId = _libraryManager.GetNewItemId(path, typeof(Movie));
        libraryEntity.AddChild(
            new Movie()
            {
                Id = itemId,
                ProviderIds =
                {
                    { "JellyshareRemoteId", remoteVideo.Id.ToString() },
                    {
                        "JellyshareRemoteAddress",
                        libraryEntity.GetProviderId("JellyshareRemoteAddress")
                    }
                },
                Path = path,
                RunTimeTicks = remoteVideo.RunTimeTicks
            }
        );
    }

    private HashSet<Guid> GetLocalVideos()
    {
        var query = new InternalItemsQuery() { IncludeItemTypes = new[] { BaseItemKind.Movie } };
        return _libraryManager
            .GetItemList(query)
            .Where(item => item.HasProviderId("JellyshareRemoteId"))
            .Select(item => Guid.Parse(item.GetProviderId("JellyshareRemoteId")!))
            .ToHashSet();
    }

    private async Task<IEnumerable<BaseItemDto>> GetRemoteVideos(
        Folder library,
        CancellationToken cancellationToken
    )
    {
        var address = new Uri(library.GetProviderId("JellyshareRemoteAddress"));
        var externalId = library.GetProviderId("JellyshareRemoteId");
        var apiKey = Plugin.Instance!.RemoteServers[address].ApiKey;
        var path = $"Items/?api_key={apiKey:N}&ParentId={externalId}";
        var videos = await _httpClient.GetFromJsonAsync<QueryResult<BaseItemDto>>(
            new Uri(address, path),
            JsonDefaults.Options,
            cancellationToken
        );
        return videos!.Items;
    }

    private IEnumerable<Folder> GetLocalFolders()
    {
        var query = new InternalItemsQuery() { IncludeItemTypes = new[] { BaseItemKind.Folder } };
        return _libraryManager
            .GetItemList(query)
            .Where(folder => folder.HasProviderId("JellyshareRemoteAddress"))
            .Cast<Folder>();
    }
}
