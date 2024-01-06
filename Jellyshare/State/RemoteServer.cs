using System;
using System.Collections.Generic;

namespace Jellyshare.State;

public class RemoteServer
{
    public Uri Address { get; set; }

    public Guid ApiKey { get; set; }

    public Dictionary<Guid, string> Libraries { get; set; }

    public Guid User { get; set; }
}
