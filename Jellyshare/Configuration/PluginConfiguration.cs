using MediaBrowser.Model.Plugins;

namespace Jellyshare.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string RemoteServersRaw { get; set; } = string.Empty;
}
