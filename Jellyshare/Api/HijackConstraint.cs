using System;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyshare.State;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Jellyshare.Api;

public class HijackConstraint : IActionConstraint
{
    private static readonly Regex _guidRegex = new(@"[\w-]{32,36}", RegexOptions.Compiled);

    public int Order => 999;

    public bool Accept(ActionConstraintContext context)
    {
        var stateManager =
            context.RouteContext.HttpContext.RequestServices.GetService(typeof(StateManager))
            as StateManager;
        var items = stateManager.RemoteVideos;
        var path = context.RouteContext.HttpContext.Request.Path.ToString();
        foreach (var match in _guidRegex.Matches(path).Cast<Match?>())
        {
            if (Guid.TryParse(match?.Value, out var id))
            {
                if (items.Contains(id))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
