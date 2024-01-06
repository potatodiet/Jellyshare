using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using Jellyshare.State;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dto;

namespace Jellyshare.Synchronize;

public class UserSync
{
    private readonly HttpClient _httpClient;
    private readonly IServerApplicationHost _applicationHost;
    private readonly StateManager _stateManager;

    public UserSync(
        HttpClient httpClient,
        IServerApplicationHost applicationHost,
        StateManager stateManager
    )
    {
        _httpClient = httpClient;
        _applicationHost = applicationHost;
        _stateManager = stateManager;
    }

    public async Task SyncUsers(CancellationToken cancellationToken)
    {
        foreach (var server in _stateManager.RemoteServers.Values)
        {
            if (server.User == Guid.Empty)
            {
                var user = await CreateRemoteUser(
                    $"Jellyshare {_applicationHost.FriendlyName}",
                    Guid.NewGuid().ToString(),
                    server.Address,
                    server.ApiKey,
                    cancellationToken
                );
                server.User = user.Id;
            }
        }
        await _stateManager.Save(cancellationToken);
    }

    private async Task<UserDto> CreateRemoteUser(
        string username,
        string password,
        Uri remoteAddress,
        Guid apiKey,
        CancellationToken cancellationToken
    )
    {
        var path = $"/Users/New?api_key={apiKey:N}";
        var content = new { Name = username, Password = password };
        var res = await _httpClient.PostAsJsonAsync(
            new Uri(remoteAddress, path),
            content,
            cancellationToken: cancellationToken
        );
        using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        var user = await JsonSerializer.DeserializeAsync<UserDto>(
            stream,
            JsonDefaults.Options,
            cancellationToken
        );
        return user!;
    }
}
