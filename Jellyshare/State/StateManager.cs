using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyshare.State;

public class StateManager
{
    private readonly ILibraryManager _libraryManager;

    public StateManager(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    public Dictionary<Uri, RemoteServer> RemoteServers { get; private set; } = new();

    public HashSet<Guid> RemoteVideos { get; private set; } = new();

    public async Task Refresh(CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery() { IncludeItemTypes = new[] { BaseItemKind.Movie } };
        RemoteVideos = _libraryManager
            .GetItemList(query)
            .Where(item => item.HasProviderId("JellyshareRemoteAddress"))
            .Select(item => item.Id)
            .ToHashSet();

        RemoteServers = Plugin.Instance!.Configuration.RemoteServers.ToDictionary(
            dto => new Uri(dto.Address),
            dto => DeserializeRemoteServerDto(dto)
        );
    }

    public async Task Save(CancellationToken cancellationToken)
    {
        var instance = Plugin.Instance!;
        instance.Configuration.RemoteServers = RemoteServers
            .Values.Select(x => SerializeRemoteServer(x))
            .ToList();
        instance.SaveConfiguration();
    }

    private static RemoteServer DeserializeRemoteServerDto(RemoteServerDto dto)
    {
        return new RemoteServer()
        {
            Address = new Uri(dto.Address),
            ApiKey = Guid.Parse(dto.ApiKey),
            Libraries = dto.Libraries.ToDictionary(
                library => Guid.Parse(library.RemoteId),
                library => library.LocalName
            ),
            User = Guid.Parse(dto.User)
        };
    }

    private static RemoteServerDto SerializeRemoteServer(RemoteServer server)
    {
        return new RemoteServerDto()
        {
            Address = server.Address.ToString(),
            ApiKey = server.ApiKey.ToString(),
            Libraries = server
                .Libraries.Select(
                    pair =>
                        new RemoteLibraryDto()
                        {
                            RemoteId = pair.Key.ToString(),
                            LocalName = pair.Value
                        }
                )
                .ToList(),
            User = server.User.ToString()
        };
    }
}
