using System.Collections.Generic;
using Jellyshare.State;
using MediaBrowser.Model.Plugins;

namespace Jellyshare.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public List<RemoteServerDto> RemoteServers { get; set; } = new();
}
