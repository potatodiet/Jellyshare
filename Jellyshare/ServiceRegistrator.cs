using Jellyshare.State;
using Jellyshare.Synchronize;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyshare;

public class ServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<LibrarySync, LibrarySync>();
        serviceCollection.AddSingleton<UserSync, UserSync>();
        serviceCollection.AddSingleton<VideoSync, VideoSync>();
        serviceCollection.AddSingleton<StateManager, StateManager>();
    }
}
