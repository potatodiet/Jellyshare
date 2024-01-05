using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyshare.Providers;

public class JellyShareProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JellyShareProvider> _logger;
    private readonly ILibraryManager _libraryManager;

    public JellyShareProvider(
        HttpClient httpClient,
        ILogger<JellyShareProvider> logger,
        ILibraryManager libraryManager
    )
    {
        _httpClient = httpClient;
        _logger = logger;
        _libraryManager = libraryManager;
    }

    public string Name => "Jellyshare";

    public int Order => 10;

    public async Task<HttpResponseMessage> GetImageResponse(
        string url,
        CancellationToken cancellationToken
    ) => await _httpClient.GetAsync(url, cancellationToken);

    public async Task<MetadataResult<Movie>> GetMetadata(
        MovieInfo info,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Jellyshare Provider has begun processing {Name} at {Path}.",
            info.Name,
            info.Path
        );

        if (!info.TryGetProviderId("JellyshareRemoteId", out var remoteIdStr))
        {
            throw new InvalidDataException("The Movie must have a JellyshareRemoteId Provider ID.");
        }
        if (!info.TryGetProviderId("JellyshareRemoteAddress", out var remoteAddressStr))
        {
            throw new InvalidDataException(
                "The Movie must have a JellyshareRemoteAddress Provider ID."
            );
        }
        var remoteId = Guid.Parse(remoteIdStr);
        var remoteAddress = new Uri(remoteAddressStr);

        var query = new InternalItemsQuery() { Path = info.Path };
        var item = _libraryManager.GetItemList(query).First();

        var userId = Plugin
            .Instance!.UserMap.First(pair => pair.Key.RemoteAddress == remoteAddress)
            .Value;
        var apiKey = Plugin.Instance!.RemoteServers[remoteAddress].ApiKey;
        var address = new Uri(
            remoteAddress,
            $"/Users/{userId}/Items/{remoteId}?api_key={apiKey:N}"
        );
        var remoteVideo =
            await _httpClient.GetFromJsonAsync<BaseItemDto>(
                address,
                JsonDefaults.Options,
                cancellationToken
            ) ?? throw new InvalidDataException();

        var people =
            from personDto in remoteVideo.People
            select new PersonInfo() { Name = personDto.Name, Role = personDto.Role };

        return new MetadataResult<Movie>()
        {
            Item = new Movie()
            {
                Name = remoteVideo.Name,
                DateCreated = DateTime.Now,
                Overview = remoteVideo.Overview,
                PremiereDate = remoteVideo.PremiereDate,
                Genres = remoteVideo.Genres,
                RunTimeTicks = remoteVideo.RunTimeTicks,
                Tags = remoteVideo.Tags,
                CommunityRating = remoteVideo.CommunityRating,
                CriticRating = remoteVideo.CriticRating,
                Tagline = remoteVideo.Taglines.FirstOrDefault(),
                ProductionYear = remoteVideo.ProductionYear,
                ProductionLocations = remoteVideo.ProductionLocations,
            },
            People = people.ToList(),
            HasMetadata = true
        };
    }

    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
        MovieInfo searchInfo,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }
}
