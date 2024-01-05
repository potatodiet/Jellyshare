using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyshare.Providers;

public class JellyshareImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly HttpClient _httpClient;

    public JellyshareImageProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "Jellyshare";

    public int Order => 10;

    public async Task<HttpResponseMessage> GetImageResponse(
        string url,
        CancellationToken cancellationToken
    ) => await _httpClient.GetAsync(url, cancellationToken);

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item,
        CancellationToken cancellationToken
    )
    {
        var externalId = item.GetProviderId("JellyshareRemoteId");
        var address = item.GetProviderId("JellyshareRemoteAddress");
        return new[]
        {
            new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Backdrop,
                Url = $"{address}Items/{externalId}/Images/Backdrop"
            },
            new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Logo,
                Url = $"{address}Items/{externalId}/Images/Logo"
            },
            new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Primary,
                Url = $"{address}Items/{externalId}/Images/Primary"
            }
        };
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) =>
        new[] { ImageType.Backdrop, ImageType.Logo, ImageType.Primary };

    public bool Supports(BaseItem item) => item is Movie;
}
