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
        foreach (var library in Plugin.Instance!.RemoteLibraries.Values)
        {
            var libraryEntity = (Folder)_libraryManager.GetItemById(library.LocalId);
            var remoteVideos = await GetRemoteVideos(library, cancellationToken);
            foreach (var remoteVideo in remoteVideos)
            {
                if (!localVideos.Contains(remoteVideo.Id))
                {
                    CreateVideo(remoteVideo, libraryEntity);
                    localVideos.Add(remoteVideo.Id);
                }
            }
            _ = _libraryManager.GetItemById(library.LocalId).RefreshMetadata(cancellationToken);
        }
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
                ProviderIds = { { "Jellyshare", remoteVideo.Id.ToString() } },
                Path = path
            }
        );
        Plugin.Instance!.RemoteVideos[itemId] = remoteVideo.Id;
    }

    private HashSet<Guid> GetLocalVideos()
    {
        var query = new InternalItemsQuery() { IncludeItemTypes = new[] { BaseItemKind.Movie } };
        return _libraryManager
            .GetItemList(query)
            .Where(item => item.HasProviderId("Jellyshare"))
            .Select(item => Guid.Parse(item.GetProviderId("Jellyshare")!))
            .ToHashSet();
    }

    private async Task<IEnumerable<BaseItemDto>> GetRemoteVideos(
        RemoteLibrary library,
        CancellationToken cancellationToken
    )
    {
        var apiKey = Plugin.Instance!.RemoteServers[library.RemoteAddress].ApiKey;
        var path = $"Items/?api_key={apiKey:N}&ParentId={library.ExternalId}";
        var videos = await _httpClient.GetFromJsonAsync<QueryResult<BaseItemDto>>(
            new Uri(library.RemoteAddress, path),
            JsonDefaults.Options,
            cancellationToken
        );
        return videos!.Items;
    }
}
