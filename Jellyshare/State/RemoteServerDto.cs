using System.Collections.Generic;

namespace Jellyshare.State;

public class RemoteServerDto
{
    public string Address { get; set; }

    public string ApiKey { get; set; }

    public List<RemoteLibraryDto> Libraries { get; set; }

    public string User { get; set; }
}
