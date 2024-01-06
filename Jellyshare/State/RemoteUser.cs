using System;

namespace Jellyshare.State;

public class RemoteUser
{
    public Guid LocalId { get; set; }
    public Guid RemoteId { get; set; }
}
