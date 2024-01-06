using System;

namespace Jellyshare.State;

public class RemoteUserDto
{
    public string LocalId { get; set; }
    public string RemoteId { get; set; }
    public string RemoteAddress { get; set; }
}
