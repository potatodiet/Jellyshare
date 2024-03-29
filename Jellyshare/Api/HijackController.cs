using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jellyshare.State;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyshare.Api;

[Route("")]
[Authorize]
public class StreamHijackController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<StreamHijackController> _logger;
    private readonly StateManager _stateManager;

    public StreamHijackController(
        HttpClient httpClient,
        ILibraryManager libraryManager,
        ILogger<StreamHijackController> logger,
        StateManager stateManager
    )
    {
        _httpClient = httpClient;
        _libraryManager = libraryManager;
        _logger = logger;
        _stateManager = stateManager;
    }

    [Hijack]
    [HttpPost("Items/{itemId}/PlaybackInfo", Order = -2)]
    public async Task<ActionResult> GetPlaybackInfo(
        [FromRoute] Guid itemId,
        [FromQuery] Guid userId
    )
    {
        var item = _libraryManager.GetItemById(itemId)!;
        var remoteAddress = new Uri(item.GetProviderId("JellyshareRemoteAddress"));
        var remoteUser = _stateManager.RemoteServers[remoteAddress].User;
        var remoteId = Guid.Parse(item.GetProviderId("JellyshareRemoteId"));

        var path = $"Items/{remoteId}/PlaybackInfo";
        var query = HttpUtility.ParseQueryString(HttpContext.Request.QueryString.ToString());
        query["api_key"] = _stateManager.RemoteServers[remoteAddress].ApiKey.ToString("N");
        query["UserId"] = remoteUser.ToString("N");
        query["MediaSourceId"] = remoteId.ToString("N");

        using var streamReader = new StreamReader(HttpContext.Request.Body);
        var body = await streamReader.ReadToEndAsync();
        var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
        var address = new Uri(remoteAddress, $"{path}?{query}");
        using var res = await _httpClient.PostAsync(address, httpContent);

        var content = await res.Content.ReadAsStringAsync();
        content = content
            .Replace(remoteId.ToString("N"), itemId.ToString("N"))
            .Replace(remoteId.ToString("D"), itemId.ToString("D"));
        var contentType = res.Content.Headers.ContentType?.ToString();
        return new ContentResult()
        {
            Content = content,
            ContentType = contentType,
            StatusCode = (int)res.StatusCode
        };
    }

    [Hijack]
    [HttpGet("Videos/{itemId}/{*remainder}", Order = -1)]
    public async Task<ActionResult> HlsHijack([FromRoute] Guid itemId, [FromRoute] string remainder)
    {
        var item = _libraryManager.GetItemById(itemId)!;
        var remoteAddress = new Uri(item.GetProviderId("JellyshareRemoteAddress"));
        var remoteId = Guid.Parse(item.GetProviderId("JellyshareRemoteId"));

        var path = $"Videos/{remoteId}/{remainder}";

        var query = HttpUtility.ParseQueryString(HttpContext.Request.QueryString.ToString());
        query["api_key"] = _stateManager.RemoteServers[remoteAddress].ApiKey.ToString("N");
        query["MediaSourceId"] = remoteId.ToString("N");

        var address = new Uri(remoteAddress, $"{path}?{query}");
        var res = await _httpClient.GetAsync(address);
        var content = await res.Content.ReadAsStreamAsync();
        var contentType = res.Content.Headers.ContentType!.ToString();
        return new FileStreamResult(content, contentType);
    }
}
