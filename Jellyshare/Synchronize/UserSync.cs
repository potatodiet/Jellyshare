using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;

namespace Jellyshare.Synchronize;

public class UserSync
{
    private readonly HttpClient _httpClient;
    private readonly IServerApplicationHost _applicationHost;
    private readonly IUserManager _userManager;

    public UserSync(
        HttpClient httpClient,
        IServerApplicationHost applicationHost,
        IUserManager userManager
    )
    {
        _httpClient = httpClient;
        _applicationHost = applicationHost;
        _userManager = userManager;
    }

    public async Task SyncUsers(CancellationToken cancellationToken)
    {
        var instance = Plugin.Instance!;
        foreach (var server in instance.RemoteServers.Values)
        {
            var remoteUsers = await GetRemoteUsers(
                server.Address,
                server.ApiKey,
                cancellationToken
            );
            var servername = _applicationHost.FriendlyName;
            foreach (var user in GetLocalUsers())
            {
                if (user.Username.Contains("Jellyshare"))
                {
                    continue;
                }

                var username = $"Jellyshare {servername} {user.Username}";
                if (!remoteUsers.Contains(username))
                {
                    var remoteUser = await CreateRemoteUser(
                        username,
                        user.Id.ToString(),
                        server.Address,
                        server.ApiKey,
                        cancellationToken
                    );
                    Plugin.Instance!.UserMap[(user.Id, server.Address)] = remoteUser.Id;
                }
            }
        }
    }

    private async Task<HashSet<string>> GetRemoteUsers(
        Uri remoteAddress,
        Guid apiKey,
        CancellationToken cancellationToken
    )
    {
        var path = $"/Users?isHidden=true&api_key={apiKey:N}";
        var users = await _httpClient.GetFromJsonAsync<List<UserDto>>(
            new Uri(remoteAddress, path),
            JsonDefaults.Options,
            cancellationToken
        );
        return users!.Select(user => user.Name).ToHashSet();
    }

    private IEnumerable<User> GetLocalUsers()
    {
        return _userManager.Users;
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
