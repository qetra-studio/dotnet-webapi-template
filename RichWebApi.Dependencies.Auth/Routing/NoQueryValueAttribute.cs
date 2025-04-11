using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace RichWebApi.Routing;

public class NoQueryValueAttribute(string queryParameter) : ActionMethodSelectorAttribute
{
	public override bool IsValidForRequest(RouteContext context, ActionDescriptor action)
	{
		return !context.HttpContext.Request.Query.TryGetValue(queryParameter, out _);
	}
}