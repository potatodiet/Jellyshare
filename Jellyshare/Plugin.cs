using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Jellyshare.Configuration;
using Jellyshare.State;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyshare;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly StateManager _stateManager;

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        StateManager stateManager
    )
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _stateManager = stateManager;
    }

    public override string Name => "Jellyshare";

    public override Guid Id => Guid.Parse("36700b7b-d95d-4082-821d-cf412466cc6b");

    public static Plugin? Instance { get; private set; }

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

    public override async void UpdateConfiguration(BasePluginConfiguration configuration)
    {
        base.UpdateConfiguration(configuration);
        await _stateManager.Refresh(CancellationToken.None);
    }
}
