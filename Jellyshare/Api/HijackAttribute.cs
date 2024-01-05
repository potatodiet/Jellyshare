using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Jellyshare.Api;

[AttributeUsage(AttributeTargets.Method)]
public class HijackAttribute : Attribute, IActionConstraintFactory
{
    public bool IsReusable => true;

    public IActionConstraint CreateInstance(IServiceProvider services)
    {
        return new HijackConstraint();
    }
}
