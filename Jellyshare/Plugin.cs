using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyshare.Configuration;
using Jellyshare.State;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyshare;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<Plugin> _logger;

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILibraryManager libraryManager,
        ILogger<Plugin> logger
    )
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public override string Name => "Jellyshare";

    public override Guid Id => Guid.Parse("36700b7b-d95d-4082-821d-cf412466cc6b");

    public static Plugin? Instance { get; private set; }

    public Dictionary<Uri, RemoteServer> RemoteServers { get; private set; } = new();

    public Dictionary<Guid, RemoteLibrary> RemoteLibraries { get; private set; } = new();

    public Dictionary<Guid, Guid> RemoteVideos { get; private set; } = new();

    // Each (LocalUser, RemoteAddress) has an associated RemoteUser.
    public Dictionary<(Guid UserId, Uri RemoteAddress), Guid> UserMap { get; private set; } = new();

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace
                )
            }
        };
    }

    public override void UpdateConfiguration(BasePluginConfiguration configuration)
    {
        base.UpdateConfiguration(configuration);
        LoadRemoteServers();
    }

    public async Task SaveState(CancellationToken cancellationToken)
    {
        await SaveRemoteLibraries(cancellationToken);
        await SaveUserMap(cancellationToken);
    }

    public async Task LoadState(CancellationToken cancellationToken)
    {
        LoadRemoteServers();
        LoadRemoteVideos();
        await LoadRemoteLibraries(cancellationToken);
        await LoadUserMap(cancellationToken);
    }

    private void LoadRemoteServers()
    {
        try
        {
            RemoteServers = JsonSerializer
                .Deserialize<List<RemoteServer>>(Configuration.RemoteServersRaw)
                .ToDictionary(server => server.Address, server => server);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to deserialize RemoteServers configuration.");
            _logger.LogError(ex.Message);
        }
    }

    private void LoadRemoteVideos()
    {
        foreach (var item in _libraryManager.GetItemList(new InternalItemsQuery()))
        {
            if (item.TryGetProviderId("Jellyshare", out var remoteIdStr))
            {
                if (Guid.TryParse(remoteIdStr, out var remoteId))
                {
                    RemoteVideos[item.Id] = remoteId;
                }
                else
                {
                    _logger.LogWarning(
                        "Could not load {Name}, {Id}. Faild to create Guid from {RemoteId}.",
                        item.Name,
                        item.Id,
                        remoteIdStr
                    );
                }
            }
        }
    }

    private async Task LoadRemoteLibraries(CancellationToken cancellationToken)
    {
        var path = Path.Combine(DataFolderPath, "state-remotelibraries.json");
        if (!File.Exists(path))
        {
            return;
        }
        await using var state = File.OpenRead(path);
        var remoteLibraries = await JsonSerializer.DeserializeAsync<
            Dictionary<Guid, RemoteLibrary>
        >(state, cancellationToken: cancellationToken);
        if (remoteLibraries is not null)
        {
            RemoteLibraries = remoteLibraries;
        }
    }

    private async Task SaveRemoteLibraries(CancellationToken cancellationToken)
    {
        var path = Path.Combine(DataFolderPath, "state-remotelibraries.json");
        await using var stream = File.OpenWrite(path);
        await JsonSerializer.SerializeAsync(
            stream,
            RemoteLibraries,
            cancellationToken: cancellationToken
        );
    }

    private async Task SaveUserMap(CancellationToken cancellationToken)
    {
        var path = Path.Combine(DataFolderPath, "state-usermap.json");
        await using var stream = File.OpenWrite(path);
        // Might not have to do this in .NET 8+.
        var intermediate = new List<List<string>>();
        foreach (var ((userId, remoteAddress), remoteUserId) in UserMap)
        {
            intermediate.Add(
                new[]
                {
                    userId.ToString(),
                    remoteAddress.ToString(),
                    remoteUserId.ToString()
                }.ToList()
            );
        }
        await JsonSerializer.SerializeAsync(
            stream,
            intermediate,
            cancellationToken: cancellationToken
        );
    }

    private async Task LoadUserMap(CancellationToken cancellationToken)
    {
        var path = Path.Combine(DataFolderPath, "state-usermap.json");
        if (!File.Exists(path))
        {
            return;
        }
        await using var state = File.OpenRead(path);
        // Might not have to do this in .NET 8+.
        var intermediate = (
            await JsonSerializer.DeserializeAsync<List<List<string>>>(
                state,
                cancellationToken: cancellationToken
            )
        )!;
        UserMap.Clear();
        foreach (var entry in intermediate)
        {
            var userId = Guid.Parse(entry[0]);
            var remoteAddress = new Uri(entry[1]);
            var remoteUserId = Guid.Parse(entry[2]);
            UserMap[(userId, remoteAddress)] = remoteUserId;
        }
    }
}
